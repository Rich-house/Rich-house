#!/usr/bin/env python3
"""Safely import Rich House products through the authenticated Product API."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import sys
import urllib.error
import urllib.parse
import urllib.request
import uuid
from dataclasses import dataclass
from datetime import datetime
from decimal import Decimal, InvalidOperation
from pathlib import Path
from typing import Any

DEFAULT_API_BASE_URL = "http://localhost:4000"
MAX_IMAGE_BYTES = 5 * 1024 * 1024
PRODUCTION_CONFIRMATION = "RICH_HOUSE_CONFIRM_PRODUCTION_IMPORT"
TOKEN_VARIABLE = "RICH_HOUSE_ADMIN_TOKEN"
API_VARIABLE = "RICH_HOUSE_API_BASE_URL"
LOCAL_HOSTS = {"localhost", "127.0.0.1", "::1"}


class ImporterError(RuntimeError):
    pass


class ApiError(ImporterError):
    def __init__(self, method: str, path: str, status: int, detail: str):
        super().__init__(f"{method} {path} failed with HTTP {status}: {detail}")
        self.status = status


@dataclass(frozen=True)
class ImageInput:
    path: Path
    is_main: bool
    order: int
    media_type: str
    digest: str


@dataclass
class PreparedProduct:
    source_index: int
    name: str
    slug: str
    short_description: str
    description: str
    category_name: str
    category_id: int
    price: Decimal
    compare_at_price: Decimal | None
    offer_price: Decimal | None
    offer_start: str | None
    offer_end: str | None
    stock_quantity: int
    status: str
    is_active: bool
    is_featured: bool
    is_best_seller: bool
    is_new_arrival: bool
    size_names: list[str]
    size_ids: list[int]
    colors: list[str]
    images: list[ImageInput]


class ApiClient:
    def __init__(self, base_url: str, token: str, timeout: float = 30.0):
        self.base_url = base_url.rstrip("/")
        self.token = token
        self.timeout = timeout

    def get_json(self, path: str, query: dict[str, Any] | None = None) -> Any:
        if query:
            path = f"{path}?{urllib.parse.urlencode(query)}"
        return self._json_request("GET", path)

    def post_product(self, product: PreparedProduct) -> Any:
        body, content_type = build_multipart(product)
        return self._json_request("POST", "/api/Product", body, content_type)

    def download(self, path_or_url: str) -> bytes:
        url = path_or_url if urllib.parse.urlparse(path_or_url).scheme else f"{self.base_url}/{path_or_url.lstrip('/')}"
        headers = {"Accept": "image/*", "User-Agent": "RichHouseProductImporter/1.0"}
        if urllib.parse.urlparse(url).netloc == urllib.parse.urlparse(self.base_url).netloc:
            headers["Authorization"] = f"Bearer {self.token}"
        request = urllib.request.Request(url, method="GET", headers=headers)
        try:
            with urllib.request.urlopen(request, timeout=self.timeout) as response:
                return response.read(MAX_IMAGE_BYTES + 1)
        except urllib.error.HTTPError as error:
            raise ApiError("GET", path_or_url, error.code, read_error_detail(error)) from error
        except urllib.error.URLError as error:
            raise ImporterError(f"GET {path_or_url} failed: {error.reason}") from error

    def _json_request(
        self,
        method: str,
        path: str,
        body: bytes | None = None,
        content_type: str | None = None,
    ) -> Any:
        headers = self._headers()
        if content_type:
            headers["Content-Type"] = content_type
        request = urllib.request.Request(
            f"{self.base_url}/{path.lstrip('/')}", data=body, method=method, headers=headers
        )
        try:
            with urllib.request.urlopen(request, timeout=self.timeout) as response:
                payload = response.read()
                return json.loads(payload) if payload else None
        except urllib.error.HTTPError as error:
            raise ApiError(method, path, error.code, read_error_detail(error)) from error
        except urllib.error.URLError as error:
            raise ImporterError(f"{method} {path} failed: {error.reason}") from error

    def _headers(self) -> dict[str, str]:
        return {
            "Accept": "application/json",
            "Authorization": f"Bearer {self.token}",
            "User-Agent": "RichHouseProductImporter/1.0",
        }


def read_error_detail(error: urllib.error.HTTPError) -> str:
    raw = error.read(4096).decode("utf-8", errors="replace").strip()
    return raw or error.reason


def slugify(value: str) -> str:
    slug = re.sub(r"[^a-z0-9]+", "-", value.strip().lower()).strip("-")
    if not slug:
        raise ImporterError(f"Cannot derive a valid ASCII slug from product name {value!r}.")
    return slug


def decimal_value(value: Any, field: str, required: bool = False) -> Decimal | None:
    if value is None or value == "":
        if required:
            raise ImporterError(f"{field} is required and cannot be inferred.")
        return None
    if isinstance(value, bool):
        raise ImporterError(f"{field} must be a number.")
    try:
        result = Decimal(str(value))
    except InvalidOperation as error:
        raise ImporterError(f"{field} must be a valid number.") from error
    return result


def boolean_value(value: Any, field: str, default: bool) -> bool:
    if value is None:
        return default
    if not isinstance(value, bool):
        raise ImporterError(f"{field} must be true or false.")
    return value


def integer_value(value: Any, field: str, default: int) -> int:
    if value is None:
        return default
    if isinstance(value, bool) or not isinstance(value, int):
        raise ImporterError(f"{field} must be an integer.")
    return value


def iso_datetime(value: Any, field: str) -> str | None:
    if value is None or value == "":
        return None
    if not isinstance(value, str):
        raise ImporterError(f"{field} must be an ISO-8601 string.")
    candidate = value.replace("Z", "+00:00")
    try:
        datetime.fromisoformat(candidate)
    except ValueError as error:
        raise ImporterError(f"{field} must be a valid ISO-8601 date/time.") from error
    return value


def detect_image_type(path: Path) -> str:
    data = path.read_bytes()[:16]
    extension = path.suffix.lower()
    if data.startswith(b"\xff\xd8\xff"):
        detected, media_type = ".jpg", "image/jpeg"
        valid_extensions = {".jpg", ".jpeg"}
    elif data.startswith(b"\x89PNG\r\n\x1a\n"):
        detected, media_type = ".png", "image/png"
        valid_extensions = {".png"}
    elif len(data) >= 12 and data[:4] == b"RIFF" and data[8:12] == b"WEBP":
        detected, media_type = ".webp", "image/webp"
        valid_extensions = {".webp"}
    else:
        raise ImporterError(f"Image is not a valid JPG, PNG, or WebP file: {path}")
    if extension not in valid_extensions:
        raise ImporterError(f"Image content ({detected}) does not match its extension: {path}")
    return media_type


def prepare_images(raw: dict[str, Any], manifest_directory: Path) -> list[ImageInput]:
    entries: list[Any]
    if "images" in raw:
        entries = raw["images"]
        if not isinstance(entries, list) or not entries:
            raise ImporterError("images must be a non-empty array.")
    elif raw.get("image"):
        entries = [{"path": raw["image"], "isMain": True, "order": 0}]
    else:
        raise ImporterError("At least one image is required (image or images).")

    main_path = raw.get("mainImage")
    if main_path is not None and not isinstance(main_path, str):
        raise ImporterError("mainImage must be an image path string.")
    normalized: list[tuple[Path, bool, int, str, str]] = []
    seen_paths: set[Path] = set()
    for index, entry in enumerate(entries):
        if isinstance(entry, str):
            entry = {"path": entry}
        if not isinstance(entry, dict) or not isinstance(entry.get("path"), str):
            raise ImporterError(f"images[{index}] must be a path string or an object with path.")
        path = (manifest_directory / entry["path"]).resolve()
        if path in seen_paths:
            raise ImporterError(f"Image path is duplicated: {entry['path']}")
        seen_paths.add(path)
        if not path.is_file():
            raise ImporterError(f"Image file does not exist: {path}")
        size = path.stat().st_size
        if size <= 0 or size > MAX_IMAGE_BYTES:
            raise ImporterError(f"Image must be non-empty and no larger than 5 MB: {path}")
        media_type = detect_image_type(path)
        is_main_value = entry.get("isMain", False)
        if not isinstance(is_main_value, bool):
            raise ImporterError(f"images[{index}].isMain must be true or false.")
        is_main = is_main_value or (main_path is not None and entry["path"] == main_path)
        order = integer_value(entry.get("order"), f"images[{index}].order", index)
        if order < 0:
            raise ImporterError(f"images[{index}].order cannot be negative.")
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        normalized.append((path, is_main, order, media_type, digest))

    normalized.sort(key=lambda item: item[2])
    if len({item[2] for item in normalized}) != len(normalized):
        raise ImporterError("Image order values must be unique.")
    if main_path is not None and not any(item[0] == (manifest_directory / main_path).resolve() for item in normalized):
        raise ImporterError("mainImage must reference one of the supplied image paths.")
    main_count = sum(1 for item in normalized if item[1])
    if main_count > 1:
        raise ImporterError("Only one image may be marked as the main image.")
    if main_count == 0:
        first = normalized[0]
        normalized[0] = (first[0], True, first[2], first[3], first[4])
    return [ImageInput(*item) for item in normalized]


def load_manifest(path: Path) -> list[dict[str, Any]]:
    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        raise ImporterError(f"Could not read manifest {path}: {error}") from error
    products = payload.get("products") if isinstance(payload, dict) else payload
    if not isinstance(products, list) or not products:
        raise ImporterError("Manifest must be a non-empty product array or an object with a products array.")
    if not all(isinstance(product, dict) for product in products):
        raise ImporterError("Every manifest product must be a JSON object.")
    return products


def fetch_all_products(api: ApiClient) -> list[dict[str, Any]]:
    products: list[dict[str, Any]] = []
    page = 1
    while True:
        response = api.get_json("/api/Product/admin", {"page": page, "pageSize": 50})
        if not isinstance(response, dict) or not isinstance(response.get("items"), list):
            raise ImporterError("Admin product endpoint returned an unexpected response.")
        products.extend(response["items"])
        pagination = response.get("pagination") or response.get("meta") or {}
        total_pages = pagination.get("totalPages")
        if isinstance(total_pages, int):
            if page >= total_pages:
                break
        elif len(response["items"]) < 50:
            break
        page += 1
        if page > 10_000:
            raise ImporterError("Product pagination did not terminate safely.")
    return products


def lookup_by_name(items: Any, label: str) -> dict[str, dict[str, Any]]:
    if not isinstance(items, list):
        raise ImporterError(f"{label} endpoint returned an unexpected response.")
    result: dict[str, dict[str, Any]] = {}
    for item in items:
        if not isinstance(item, dict) or not isinstance(item.get("name"), str) or not isinstance(item.get("id"), int):
            raise ImporterError(f"{label} endpoint returned an invalid lookup item.")
        key = item["name"].strip().casefold()
        if key in result:
            raise ImporterError(f"{label} lookup contains duplicate name {item['name']!r}.")
        result[key] = item
    return result


def prepare_product(
    raw: dict[str, Any],
    index: int,
    manifest_directory: Path,
    categories: dict[str, dict[str, Any]],
    sizes: dict[str, dict[str, Any]],
) -> PreparedProduct:
    prefix = f"products[{index}]"
    name = raw.get("name")
    if not isinstance(name, str) or len(name.strip()) < 2:
        raise ImporterError(f"{prefix}.name is required and must contain at least 2 characters.")
    name = name.strip()
    supplied_slug = raw.get("slug")
    if supplied_slug is not None and not isinstance(supplied_slug, str):
        raise ImporterError(f"{prefix}.slug must be text.")
    slug = slugify(supplied_slug or name)
    category_name = raw.get("category")
    if not isinstance(category_name, str) or not category_name.strip():
        raise ImporterError(f"{prefix}.category is required.")
    category = categories.get(category_name.strip().casefold())
    if category is None:
        raise ImporterError(f"{prefix}.category is unknown: {category_name!r}.")

    price = decimal_value(raw.get("price"), f"{prefix}.price", required=True)
    assert price is not None
    if price <= 0:
        raise ImporterError(f"{prefix}.price must be greater than zero.")
    compare_at_price = decimal_value(raw.get("compareAtPrice"), f"{prefix}.compareAtPrice")
    offer_price = decimal_value(raw.get("offerPrice"), f"{prefix}.offerPrice")
    if compare_at_price is not None and compare_at_price <= price:
        raise ImporterError(f"{prefix}.compareAtPrice must be greater than price.")
    if offer_price is not None and (offer_price <= 0 or offer_price >= price):
        raise ImporterError(f"{prefix}.offerPrice must be greater than zero and lower than price.")
    offer_start = iso_datetime(raw.get("offerStart"), f"{prefix}.offerStart")
    offer_end = iso_datetime(raw.get("offerEnd"), f"{prefix}.offerEnd")
    if offer_start and offer_end:
        if datetime.fromisoformat(offer_end.replace("Z", "+00:00")) <= datetime.fromisoformat(offer_start.replace("Z", "+00:00")):
            raise ImporterError(f"{prefix}.offerEnd must be after offerStart.")

    stock_quantity = integer_value(raw.get("stockQuantity", raw.get("stock")), f"{prefix}.stockQuantity", 0)
    if stock_quantity < 0:
        raise ImporterError(f"{prefix}.stockQuantity cannot be negative.")
    description = raw.get("description", name)
    if not isinstance(description, str) or not description.strip():
        raise ImporterError(f"{prefix}.description must be text when supplied.")
    short_description = raw.get("shortDescription", "")
    if not isinstance(short_description, str):
        raise ImporterError(f"{prefix}.shortDescription must be text.")

    raw_sizes = raw.get("sizes", [])
    if not isinstance(raw_sizes, list) or not all(isinstance(size, str) and size.strip() for size in raw_sizes):
        raise ImporterError(f"{prefix}.sizes must be an array of size names.")
    size_names: list[str] = []
    size_ids: list[int] = []
    seen_sizes: set[str] = set()
    for size_name in raw_sizes:
        key = size_name.strip().casefold()
        size = sizes.get(key)
        if size is None:
            raise ImporterError(f"{prefix}.sizes contains unknown size {size_name!r}.")
        if key not in seen_sizes:
            seen_sizes.add(key)
            size_names.append(size["name"])
            size_ids.append(size["id"])

    colors = raw.get("colors", [])
    if not isinstance(colors, list) or not all(isinstance(color, str) and color.strip() for color in colors):
        raise ImporterError(f"{prefix}.colors must be an array of non-empty names.")
    unique_colors: dict[str, str] = {}
    for color in colors:
        unique_colors.setdefault(color.strip().casefold(), color.strip())
    colors = list(unique_colors.values())
    images = prepare_images(raw, manifest_directory)
    status = raw.get("status", "Draft")
    if not isinstance(status, str) or not status.strip():
        raise ImporterError(f"{prefix}.status must be non-empty text.")
    canonical_statuses = {value.casefold(): value for value in ("Draft", "Active", "Published", "Archived")}
    canonical_status = canonical_statuses.get(status.strip().casefold())
    if canonical_status is None:
        raise ImporterError(f"{prefix}.status must be Draft, Active, Published, or Archived.")

    return PreparedProduct(
        source_index=index,
        name=name,
        slug=slug,
        short_description=short_description.strip(),
        description=description.strip(),
        category_name=category["name"],
        category_id=category["id"],
        price=price,
        compare_at_price=compare_at_price,
        offer_price=offer_price,
        offer_start=offer_start,
        offer_end=offer_end,
        stock_quantity=stock_quantity,
        status=canonical_status,
        is_active=boolean_value(raw.get("isActive"), f"{prefix}.isActive", True),
        is_featured=boolean_value(raw.get("isFeatured"), f"{prefix}.isFeatured", False),
        is_best_seller=boolean_value(raw.get("isBestSeller"), f"{prefix}.isBestSeller", False),
        is_new_arrival=boolean_value(raw.get("isNewArrival"), f"{prefix}.isNewArrival", False),
        size_names=size_names,
        size_ids=size_ids,
        colors=colors,
        images=images,
    )


def validate_manifest_duplicates(products: list[PreparedProduct]) -> None:
    seen_names: set[str] = set()
    seen_slugs: set[str] = set()
    for product in products:
        name_key = product.name.casefold()
        if name_key in seen_names:
            raise ImporterError(f"Manifest contains duplicate product name {product.name!r}.")
        if product.slug in seen_slugs:
            raise ImporterError(f"Manifest contains duplicate product slug {product.slug!r}.")
        seen_names.add(name_key)
        seen_slugs.add(product.slug)


def find_existing(product: PreparedProduct, existing: list[dict[str, Any]]) -> dict[str, Any] | None:
    by_name = [item for item in existing if str(item.get("name", "")).strip().casefold() == product.name.casefold()]
    by_slug = [item for item in existing if str(item.get("slug", "")).strip().casefold() == product.slug.casefold()]
    matches = {item.get("id"): item for item in by_name + by_slug}
    if len(matches) > 1:
        raise ImporterError(f"Name/slug for {product.name!r} collide with different existing products.")
    return next(iter(matches.values()), None)


def normalized_decimal(value: Any) -> Decimal | None:
    return None if value is None else Decimal(str(value))


def normalized_datetime(value: Any) -> str | None:
    if value is None or value == "":
        return None
    parsed = datetime.fromisoformat(str(value).replace("Z", "+00:00"))
    return parsed.isoformat()


def existing_matches(api: ApiClient, product: PreparedProduct, summary: dict[str, Any]) -> bool:
    details = api.get_json(f"/api/Product/admin/{summary['id']}")
    expected = {
        "name": product.name,
        "slug": product.slug,
        "shortDescription": product.short_description or None,
        "description": product.description,
        "categoryId": product.category_id,
        "price": product.price,
        "compareAtPrice": product.compare_at_price,
        "offerPrice": product.offer_price,
        "offerStart": normalized_datetime(product.offer_start),
        "offerEnd": normalized_datetime(product.offer_end),
        "stockQuantity": product.stock_quantity,
        "status": product.status,
        "isActive": product.is_active,
        "isFeatured": product.is_featured,
        "isBestSeller": product.is_best_seller,
        "isNewArrival": product.is_new_arrival,
    }
    for key, value in expected.items():
        actual = details.get(key)
        if key in {"price", "compareAtPrice", "offerPrice"}:
            actual = normalized_decimal(actual)
        elif key in {"offerStart", "offerEnd"}:
            actual = normalized_datetime(actual)
        if actual != value:
            return False
    if sorted(details.get("selectedSizeIds", [])) != sorted(product.size_ids):
        return False
    if sorted(details.get("colors", []), key=str.casefold) != sorted(product.colors, key=str.casefold):
        return False
    images = sorted(details.get("images", []), key=lambda item: item.get("sortOrder", 0))
    if len(images) != len(product.images):
        return False
    for remote, local in zip(images, product.images):
        if bool(remote.get("isMain")) != local.is_main:
            return False
        content = api.download(remote.get("url", ""))
        if len(content) > MAX_IMAGE_BYTES or hashlib.sha256(content).hexdigest() != local.digest:
            return False
    return True


def append_part(parts: list[bytes], boundary: str, name: str, value: str) -> None:
    parts.extend([
        f"--{boundary}\r\n".encode(),
        f'Content-Disposition: form-data; name="{name}"\r\n\r\n'.encode(),
        value.encode("utf-8"),
        b"\r\n",
    ])


def build_multipart(product: PreparedProduct) -> tuple[bytes, str]:
    boundary = f"----RichHouseImport{uuid.uuid4().hex}"
    parts: list[bytes] = []
    fields: list[tuple[str, str]] = [
        ("Name", product.name), ("Slug", product.slug),
        ("ShortDescription", product.short_description), ("Description", product.description),
        ("CategoryId", str(product.category_id)), ("Price", str(product.price)),
        ("StockQuantity", str(product.stock_quantity)), ("Status", product.status),
        ("IsActive", str(product.is_active).lower()), ("IsFeatured", str(product.is_featured).lower()),
        ("IsBestSeller", str(product.is_best_seller).lower()), ("IsNewArrival", str(product.is_new_arrival).lower()),
        ("FailOnDuplicateNameOrSlug", "true"),
    ]
    for field, value in (("CompareAtPrice", product.compare_at_price), ("OfferPrice", product.offer_price),
                         ("OfferStart", product.offer_start), ("OfferEnd", product.offer_end)):
        if value is not None:
            fields.append((field, str(value)))
    fields.extend(("SelectedSizeIds", str(size_id)) for size_id in product.size_ids)
    fields.extend(("Colors", color) for color in product.colors)
    for field, value in fields:
        append_part(parts, boundary, field, value)

    main_reference = "new:0"
    for index, image in enumerate(product.images):
        token = f"new:{index}"
        append_part(parts, boundary, "ImageOrder", token)
        if image.is_main:
            main_reference = token
        parts.extend([
            f"--{boundary}\r\n".encode(),
            f'Content-Disposition: form-data; name="Images"; filename="{image.path.name}"\r\n'.encode(),
            f"Content-Type: {image.media_type}\r\n\r\n".encode(),
            image.path.read_bytes(), b"\r\n",
        ])
    append_part(parts, boundary, "MainImageReference", main_reference)
    parts.append(f"--{boundary}--\r\n".encode())
    return b"".join(parts), f"multipart/form-data; boundary={boundary}"


def is_non_local_url(base_url: str) -> bool:
    host = (urllib.parse.urlparse(base_url).hostname or "").casefold()
    return host not in LOCAL_HOSTS


def refresh_lookups(api: ApiClient) -> tuple[list[dict[str, Any]], dict[str, dict[str, Any]], dict[str, dict[str, Any]]]:
    products = fetch_all_products(api)
    categories = lookup_by_name(api.get_json("/api/Category/admin"), "Category")
    sizes = lookup_by_name(api.get_json("/api/admin/catalog/sizes"), "Size")
    return products, categories, sizes


def run_import(
    manifest_path: Path,
    api: ApiClient,
    execute: bool,
    production_confirmed: bool,
) -> int:
    if execute and is_non_local_url(api.base_url) and not production_confirmed:
        raise ImporterError(
            f"Execution against non-local host requires {PRODUCTION_CONFIRMATION}=YES."
        )

    api.get_json("/api/Auth/me")
    existing, categories, sizes = refresh_lookups(api)
    raw_products = load_manifest(manifest_path)
    prepared: list[PreparedProduct] = []
    validation_errors: list[str] = []
    for index, raw in enumerate(raw_products):
        try:
            prepared.append(prepare_product(raw, index, manifest_path.parent.resolve(), categories, sizes))
        except ImporterError as error:
            validation_errors.append(str(error))
    if validation_errors:
        print("EXECUTE PREFLIGHT" if execute else "DRY RUN")
        print(f"Products in manifest: {len(raw_products)}")
        print(f"Valid: {len(prepared)}")
        print(f"Errors: {len(validation_errors)}")
        for error in validation_errors:
            print(f"  - {error}")
        print("No mutations performed.")
        raise ImporterError("Manifest validation failed.")
    validate_manifest_duplicates(prepared)

    create: list[PreparedProduct] = []
    skipped: list[PreparedProduct] = []
    for product in prepared:
        match = find_existing(product, existing)
        if match is None:
            create.append(product)
        elif existing_matches(api, product, match):
            skipped.append(product)
        else:
            raise ImporterError(
                f"Existing product conflicts with manifest name/slug: {product.name!r} ({product.slug})."
            )

    mode = "EXECUTE" if execute else "DRY RUN"
    print(mode)
    print(f"Products in manifest: {len(prepared)}")
    print(f"Valid: {len(prepared)}")
    print(f"Already matching (skip): {len(skipped)}")
    print(f"Would create: {len(create)}")
    print("Errors: 0")
    if create:
        print("\nWOULD CREATE:" if not execute else "\nTO CREATE:")
        for number, product in enumerate(create, 1):
            print(
                f"{number}. {product.name} | slug={product.slug} | category={product.category_name} "
                f"| price={product.price} | sizes={','.join(product.size_names) or '-'} | images={len(product.images)}"
            )
    if not execute:
        print("\nNo mutations performed.")
        return 0

    succeeded: list[tuple[str, Any]] = []
    for product in create:
        current, current_categories, current_sizes = refresh_lookups(api)
        match = find_existing(product, current)
        if match is not None:
            if existing_matches(api, product, match):
                print(f"SKIP: {product.name} already exists and matches.")
                continue
            raise ImporterError(f"Import stopped: {product.name!r} now conflicts with an existing product.")
        current_category = current_categories.get(product.category_name.casefold())
        if current_category is None or current_category["id"] != product.category_id:
            raise ImporterError(f"Import stopped: category mapping changed for {product.category_name!r}.")
        for size_name, size_id in zip(product.size_names, product.size_ids):
            current_size = current_sizes.get(size_name.casefold())
            if current_size is None or current_size["id"] != size_id:
                raise ImporterError(f"Import stopped: size mapping changed for {size_name!r}.")
        try:
            response = api.post_product(product)
        except Exception as error:
            print(f"FAILED: {product.name}", file=sys.stderr)
            print(f"Successfully created before failure: {len(succeeded)}", file=sys.stderr)
            for name, product_id in succeeded:
                print(f"  - {name} (ID {product_id})", file=sys.stderr)
            raise ImporterError(f"Import stopped after API failure: {error}") from error
        if not isinstance(response, dict) or response.get("slug") != product.slug:
            raise ImporterError(
                f"Import stopped: API returned an unexpected slug after creating {product.name!r}. "
                "Inspect the created record before rerunning."
            )
        succeeded.append((product.name, response.get("id") if isinstance(response, dict) else "unknown"))
        print(f"CREATED: {product.name} (ID {succeeded[-1][1]})")
    print(f"\nCreated: {len(succeeded)}; skipped: {len(prepared) - len(create) + len(create) - len(succeeded)}")
    return 0


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("manifest", type=Path, help="JSON product manifest")
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--dry-run", action="store_true", help="validate only (the default)")
    mode.add_argument("--execute", action="store_true", help="create products through POST /api/Product")
    parser.add_argument("--api-base-url", help=f"API base URL (default: ${API_VARIABLE} or {DEFAULT_API_BASE_URL})")
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv)
    token = os.environ.get(TOKEN_VARIABLE, "").strip()
    if not token:
        raise ImporterError(f"Set {TOKEN_VARIABLE}; tokens are never accepted on the command line.")
    base_url = args.api_base_url or os.environ.get(API_VARIABLE) or DEFAULT_API_BASE_URL
    return run_import(
        args.manifest.resolve(),
        ApiClient(base_url, token),
        execute=bool(args.execute),
        production_confirmed=os.environ.get(PRODUCTION_CONFIRMATION) == "YES",
    )


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except ImporterError as error:
        print(f"ERROR: {error}", file=sys.stderr)
        raise SystemExit(2)
