# Rich House product importer

This tool creates products through the same authenticated `POST /api/Product` multipart API used by the Admin Dashboard. It never writes directly to SQL. Product uploads therefore use the backend's normal image pipeline and create the original plus card, thumbnail, detail, and optimized WebP variants.

## Safety model

- Dry-run is the default. Omitting both mode flags performs no mutation.
- The API defaults to `http://localhost:4000`; it never defaults to production.
- Authentication is read only from `RICH_HOUSE_ADMIN_TOKEN`. The token is not accepted as a command argument or printed.
- `--execute` is mandatory for POST requests.
- Execution against any non-local host additionally requires `RICH_HOUSE_CONFIRM_PRODUCTION_IMPORT=YES`.
- Every product is checked by case-insensitive name and slug during preflight and again immediately before its POST.
- Import POSTs ask the backend to fail on duplicate name/slug, so the API cannot silently create a `-2` slug during an import race.
- A matching existing product is skipped. A conflicting name/slug aborts the import. Slug suffixes are never used as duplicate recovery.
- The importer stops at the first failed POST, reports products already created, and can be rerun safely.

## Manifest

The root can be an array, as in [`products.example.json`](products.example.json), or `{ "products": [...] }`.

Required fields:

- `name`
- `category` as the exact category name
- `price`; there is deliberately no price default
- `image` or non-empty `images`

Supported fields are `slug`, `shortDescription`, `description`, `category`, `price`, `compareAtPrice`, `offerPrice`, `offerStart`, `offerEnd`, `stockQuantity` (or `stock`), `status`, `isActive`, `isFeatured`, `isBestSeller`, `isNewArrival`, `sizes`, `colors`, and images.

`image` is a shortcut for one main image. `images` accepts path strings or objects with `path`, `isMain`, and `order`. A top-level `mainImage` may name one image path. Relative paths resolve from the manifest directory. JPG, PNG, and WebP content is checked against its extension and the 5 MB API limit.

Defaults are intentionally conservative: derived slug, description equal to the product name, stock `0`, status `Draft`, active `true`, all merchandising flags `false`, and empty sizes/colors. Review these values in dry-run before execution. Price is always required. Offer price must be below base price; compare-at price must be above it; offer dates must be valid and ordered.

## Usage

Obtain an Admin or SuperAdmin bearer token without placing it in shell history, then export it:

```bash
read -rs RICH_HOUSE_ADMIN_TOKEN
export RICH_HOUSE_ADMIN_TOKEN
```

Local dry-run (default API and default safe mode):

```bash
python3 scripts/product-import/import-products.py path/to/products.json --dry-run
```

Local execution:

```bash
python3 scripts/product-import/import-products.py path/to/products.json --execute
```

Production dry-run (still makes only authenticated GET requests):

```bash
RICH_HOUSE_API_BASE_URL=https://richhouse-api.runasp.net \
python3 scripts/product-import/import-products.py path/to/products.json --dry-run
```

Production execution requires both explicit controls and should only follow an approved dry-run:

```bash
RICH_HOUSE_API_BASE_URL=https://richhouse-api.runasp.net \
RICH_HOUSE_CONFIRM_PRODUCTION_IMPORT=YES \
python3 scripts/product-import/import-products.py path/to/products.json --execute
```

## Codex workflow

1. Put real product images beside a new manifest.
2. Ask Codex to inspect the images and prepare descriptive fields and category/size names.
3. Supply missing commercial facts, especially price. Codex should ask rather than infer them.
4. Run a dry-run and review the exact create/skip plan.
5. Explicitly approve production execution only after the dry-run is clean.
6. Run with both production safety controls. Keep the result so a partial failure can be rerun idempotently.

## Troubleshooting

- `401`/`403`: use a current Admin or SuperAdmin token.
- Unknown category/size: use the exact current lookup name; the importer never assumes IDs.
- Conflict: inspect the existing product. The importer will not create a suffixed duplicate.
- Partial failure: correct the failed entry and rerun. Prior matching creations are skipped.
- Image error: check content, extension, file size, and path relative to the manifest.

## Tests

```bash
dotnet run --project tests/Marketify.ImagePipeline.Tests/Marketify.ImagePipeline.Tests.csproj
python3 -m unittest discover -s scripts/product-import/tests -v
```
