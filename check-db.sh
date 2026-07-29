#!/usr/bin/env bash

set -u

CONTAINER="marketify-sql"

echo "========================================"
echo " SQL Server Database Check"
echo "========================================"

# التأكد إن الكونتينر موجود
if ! docker inspect "$CONTAINER" >/dev/null 2>&1; then
    echo "❌ الكونتينر $CONTAINER مش موجود."
    exit 1
fi

# التأكد إنه شغال
STATUS=$(docker inspect -f '{{.State.Status}}' "$CONTAINER")
echo "Container status: $STATUS"

if [ "$STATUS" != "running" ]; then
    echo "⚠️ الكونتينر مش شغال. شغله بالأمر:"
    echo "docker start $CONTAINER"
    exit 1
fi

# استخراج باسورد SQL Server من متغيرات الكونتينر
SA_PASSWORD=$(
    docker inspect -f '{{range .Config.Env}}{{println .}}{{end}}' "$CONTAINER" |
    sed -n 's/^MSSQL_SA_PASSWORD=//p' |
    head -n 1
)

if [ -z "${SA_PASSWORD:-}" ]; then
    SA_PASSWORD=$(
        docker inspect -f '{{range .Config.Env}}{{println .}}{{end}}' "$CONTAINER" |
        sed -n 's/^SA_PASSWORD=//p' |
        head -n 1
    )
fi

if [ -z "${SA_PASSWORD:-}" ]; then
    echo "❌ مقدرتش أجيب باسورد sa من إعدادات الكونتينر."
    exit 1
fi

# تحديد مكان sqlcmd
if docker exec "$CONTAINER" test -x /opt/mssql-tools18/bin/sqlcmd; then
    SQLCMD="/opt/mssql-tools18/bin/sqlcmd"
    TRUST_FLAG="-C"
elif docker exec "$CONTAINER" test -x /opt/mssql-tools/bin/sqlcmd; then
    SQLCMD="/opt/mssql-tools/bin/sqlcmd"
    TRUST_FLAG=""
else
    echo "❌ أداة sqlcmd مش موجودة داخل الكونتينر."
    exit 1
fi

run_sql() {
    local database="$1"
    local query="$2"

    docker exec \
        -e SQLCMDPASSWORD="$SA_PASSWORD" \
        "$CONTAINER" \
        "$SQLCMD" \
        -S localhost \
        -U sa \
        $TRUST_FLAG \
        -d "$database" \
        -W \
        -s "|" \
        -Q "$query"
}

echo
echo "========================================"
echo " Docker storage"
echo "========================================"

docker inspect "$CONTAINER" --format 'Mounts: {{json .Mounts}}'

echo
echo "========================================"
echo " SQL Server databases"
echo "========================================"

run_sql master "
SET NOCOUNT ON;
SELECT
    name AS DatabaseName,
    state_desc AS Status,
    recovery_model_desc AS RecoveryModel,
    create_date AS CreatedAt
FROM sys.databases
ORDER BY database_id;
"

DATABASES=$(
    docker exec \
        -e SQLCMDPASSWORD="$SA_PASSWORD" \
        "$CONTAINER" \
        "$SQLCMD" \
        -S localhost \
        -U sa \
        $TRUST_FLAG \
        -d master \
        -h -1 \
        -W \
        -Q "
SET NOCOUNT ON;
SELECT name
FROM sys.databases
WHERE database_id > 4
  AND state_desc = 'ONLINE';
" | sed '/^[[:space:]]*$/d'
)

if [ -z "$DATABASES" ]; then
    echo
    echo "❌ مفيش أي User Database موجودة."
    exit 0
fi

while IFS= read -r DB; do
    DB=$(echo "$DB" | xargs)

    [ -z "$DB" ] && continue

    echo
    echo "################################################"
    echo " DATABASE: $DB"
    echo "################################################"

    echo
    echo "--- Tables and row counts ---"

    run_sql "$DB" "
SET NOCOUNT ON;

SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    SUM(p.rows) AS RowCount
FROM sys.tables t
INNER JOIN sys.schemas s
    ON t.schema_id = s.schema_id
INNER JOIN sys.partitions p
    ON t.object_id = p.object_id
WHERE p.index_id IN (0,1)
GROUP BY s.name, t.name
ORDER BY RowCount DESC, t.name;
"

    echo
    echo "--- Important Marketify tables ---"

    run_sql "$DB" "
SET NOCOUNT ON;

SELECT 'Categories' AS TableName,
       CASE WHEN OBJECT_ID('dbo.Categories') IS NULL
            THEN -1
            ELSE (SELECT COUNT(*) FROM dbo.Categories)
       END AS RowCount

UNION ALL

SELECT 'Products',
       CASE WHEN OBJECT_ID('dbo.Products') IS NULL
            THEN -1
            ELSE (SELECT COUNT(*) FROM dbo.Products)
       END

UNION ALL

SELECT 'ProductImages',
       CASE WHEN OBJECT_ID('dbo.ProductImages') IS NULL
            THEN -1
            ELSE (SELECT COUNT(*) FROM dbo.ProductImages)
       END

UNION ALL

SELECT 'ProductSizes',
       CASE WHEN OBJECT_ID('dbo.ProductSizes') IS NULL
            THEN -1
            ELSE (SELECT COUNT(*) FROM dbo.ProductSizes)
       END

UNION ALL

SELECT 'ProductVariants',
       CASE WHEN OBJECT_ID('dbo.ProductVariants') IS NULL
            THEN -1
            ELSE (SELECT COUNT(*) FROM dbo.ProductVariants)
       END

UNION ALL

SELECT 'Sizes',
       CASE WHEN OBJECT_ID('dbo.Sizes') IS NULL
            THEN -1
            ELSE (SELECT COUNT(*) FROM dbo.Sizes)
       END

UNION ALL

SELECT 'AspNetUsers',
       CASE WHEN OBJECT_ID('dbo.AspNetUsers') IS NULL
            THEN -1
            ELSE (SELECT COUNT(*) FROM dbo.AspNetUsers)
       END;
"

    echo
    echo "--- Applied EF Core migrations ---"

    run_sql "$DB" "
SET NOCOUNT ON;

IF OBJECT_ID('dbo.__EFMigrationsHistory') IS NOT NULL
BEGIN
    SELECT MigrationId, ProductVersion
    FROM dbo.__EFMigrationsHistory
    ORDER BY MigrationId;
END
ELSE
BEGIN
    SELECT 'No __EFMigrationsHistory table' AS Result;
END
"

    echo
    echo "--- Categories preview ---"

    run_sql "$DB" "
SET NOCOUNT ON;

IF OBJECT_ID('dbo.Categories') IS NOT NULL
BEGIN
    SELECT TOP (50)
        Id,
        Name,
        Slug,
        IsActive,
        IsDeleted
    FROM dbo.Categories
    ORDER BY Id;
END
ELSE
BEGIN
    SELECT 'Categories table not found' AS Result;
END
"

    echo
    echo "--- Products preview ---"

    run_sql "$DB" "
SET NOCOUNT ON;

IF OBJECT_ID('dbo.Products') IS NOT NULL
BEGIN
    SELECT TOP (50)
        Id,
        Name,
        Price,
        CategoryId,
        IsActive,
        Status,
        CreatedAt
    FROM dbo.Products
    ORDER BY Id;
END
ELSE
BEGIN
    SELECT 'Products table not found' AS Result;
END
"

done <<< "$DATABASES"

echo
echo "========================================"
echo " Check completed"
echo "========================================"
