from __future__ import annotations

import hashlib
import importlib.util
import json
import tempfile
import unittest
from decimal import Decimal
from pathlib import Path

MODULE_PATH = Path(__file__).parents[1] / "import-products.py"
SPEC = importlib.util.spec_from_file_location("rich_house_importer", MODULE_PATH)
importer = importlib.util.module_from_spec(SPEC)
assert SPEC and SPEC.loader
import sys
sys.modules[SPEC.name] = importer
SPEC.loader.exec_module(importer)

JPEG = b"\xff\xd8\xff" + b"test-image-content"


class FakeApi:
    def __init__(self, *, base_url="http://localhost:4000", existing=None, details=None, downloads=None):
        self.base_url = base_url
        self.existing = existing or []
        self.details = details or {}
        self.downloads = downloads or {}
        self.posts = []

    def get_json(self, path, query=None):
        if path == "/api/Auth/me":
            return {"email": "admin@example.invalid"}
        if path == "/api/Product/admin":
            return {"items": self.existing, "pagination": {"totalPages": 1}}
        if path.startswith("/api/Product/admin/"):
            return self.details[int(path.rsplit("/", 1)[1])]
        if path == "/api/Category/admin":
            return [{"id": 31, "name": "Suits"}]
        if path == "/api/admin/catalog/sizes":
            return [{"id": 4, "name": "44"}, {"id": 6, "name": "46"}]
        raise AssertionError(path)

    def download(self, path):
        return self.downloads[path]

    def post_product(self, product):
        self.posts.append(product)
        return {"id": 100 + len(self.posts), "name": product.name, "slug": product.slug}


class ImporterTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        (self.root / "image.jpg").write_bytes(JPEG)

    def tearDown(self):
        self.temp.cleanup()

    def product(self, **overrides):
        value = {
            "name": "Test Suit",
            "description": "A test suit.",
            "category": "Suits",
            "price": 100,
            "stockQuantity": 3,
            "sizes": ["44"],
            "image": "image.jpg",
        }
        value.update(overrides)
        return value

    def write_manifest(self, products):
        path = self.root / "products.json"
        path.write_text(json.dumps(products), encoding="utf-8")
        return path

    def prepare(self, raw, api=None):
        api = api or FakeApi()
        _, categories, sizes = importer.refresh_lookups(api)
        return importer.prepare_product(raw, 0, self.root, categories, sizes)

    def test_valid_manifest(self):
        product = self.prepare(self.product())
        self.assertEqual(product.slug, "test-suit")
        self.assertEqual(product.price, Decimal("100"))
        self.assertEqual(product.category_id, 31)
        self.assertEqual(product.size_ids, [4])
        self.assertTrue(product.images[0].is_main)

    def test_missing_price(self):
        with self.assertRaisesRegex(importer.ImporterError, "price is required"):
            self.prepare(self.product(price=None))

    def test_unknown_category(self):
        with self.assertRaisesRegex(importer.ImporterError, "category is unknown"):
            self.prepare(self.product(category="Unknown"))

    def test_unknown_size(self):
        with self.assertRaisesRegex(importer.ImporterError, "unknown size"):
            self.prepare(self.product(sizes=["999"]))

    def test_duplicate_slug(self):
        products = [self.prepare(self.product(name="One", slug="same")), self.prepare(self.product(name="Two", slug="same"))]
        with self.assertRaisesRegex(importer.ImporterError, "duplicate product slug"):
            importer.validate_manifest_duplicates(products)

    def test_duplicate_name(self):
        products = [self.prepare(self.product(name="Same", slug="one")), self.prepare(self.product(name="same", slug="two"))]
        with self.assertRaisesRegex(importer.ImporterError, "duplicate product name"):
            importer.validate_manifest_duplicates(products)

    def test_dry_run_performs_no_post(self):
        api = FakeApi()
        result = importer.run_import(self.write_manifest([self.product()]), api, execute=False, production_confirmed=False)
        self.assertEqual(result, 0)
        self.assertEqual(api.posts, [])

    def test_execute_requires_explicit_flag(self):
        args = importer.parse_args([str(self.write_manifest([self.product()]))])
        self.assertFalse(args.execute)

    def test_multipart_requests_strict_duplicate_handling(self):
        body, content_type = importer.build_multipart(self.prepare(self.product()))
        self.assertIn("multipart/form-data", content_type)
        self.assertIn(b'name="FailOnDuplicateNameOrSlug"\r\n\r\ntrue', body)

    def test_production_execute_requires_confirmation_before_requests(self):
        api = FakeApi(base_url="https://richhouse-api.runasp.net")
        with self.assertRaisesRegex(importer.ImporterError, "requires RICH_HOUSE_CONFIRM_PRODUCTION_IMPORT=YES"):
            importer.run_import(self.write_manifest([self.product()]), api, execute=True, production_confirmed=False)
        self.assertEqual(api.posts, [])

    def test_partial_previous_import_is_skipped(self):
        first = self.product(name="First Suit", slug="first-suit")
        second = self.product(name="Second Suit", slug="second-suit")
        image_hash = hashlib.sha256(JPEG).hexdigest()
        self.assertTrue(image_hash)
        details = {
            10: {
                "id": 10, "name": "First Suit", "slug": "first-suit", "shortDescription": None,
                "description": "A test suit.", "categoryId": 31, "price": 100,
                "compareAtPrice": None, "offerPrice": None, "stockQuantity": 3, "status": "Draft",
                "isActive": True, "isFeatured": False, "isBestSeller": False, "isNewArrival": False,
                "selectedSizeIds": [4], "colors": [],
                "images": [{"url": "/images/products/first.jpg", "isMain": True, "sortOrder": 0}],
            }
        }
        api = FakeApi(
            existing=[{"id": 10, "name": "First Suit", "slug": "first-suit"}],
            details=details,
            downloads={"/images/products/first.jpg": JPEG},
        )
        result = importer.run_import(self.write_manifest([first, second]), api, execute=True, production_confirmed=False)
        self.assertEqual(result, 0)
        self.assertEqual([product.name for product in api.posts], ["Second Suit"])


if __name__ == "__main__":
    unittest.main()
