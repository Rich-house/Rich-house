import { mkdir, writeFile } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const siteUrl = normalizeBaseUrl(process.env.RICH_HOUSE_SITE_URL || 'https://www.richhouseeg.com');
const apiUrl = normalizeBaseUrl(
  process.env.RICH_HOUSE_API_URL || 'https://richhouse-api.runasp.net',
);
const currentFilePath = fileURLToPath(import.meta.url);
const projectRoot = resolve(dirname(currentFilePath), '..');
const publicDirectory = resolve(projectRoot, 'public');
const sitemapOutputPath = resolve(publicDirectory, 'sitemap.xml');
const robotsOutputPath = resolve(publicDirectory, 'robots.txt');
const buildDate = new Date().toISOString().split('T')[0];

const staticPages = [
  { path: '/', changefreq: 'daily', priority: '1.0' },
  { path: '/shop', changefreq: 'daily', priority: '0.9' },
  { path: '/offers', changefreq: 'daily', priority: '0.8' },
  { path: '/about', changefreq: 'monthly', priority: '0.7' },
  { path: '/contact', changefreq: 'monthly', priority: '0.7' },
];

await mkdir(publicDirectory, { recursive: true });

const [categories, products] = await Promise.all([fetchCategories(), fetchAllProducts()]);

const sitemapEntries = [
  ...staticPages.map((page) => ({
    loc: toAbsoluteUrl(page.path),
    lastmod: buildDate,
    changefreq: page.changefreq,
    priority: page.priority,
  })),
  ...buildCategoryEntries(categories, products).map((category) => ({
      loc: toAbsoluteUrl(`/shop/${category.slug.trim()}`),
      lastmod: buildDate,
      changefreq: 'weekly',
      priority: '0.8',
    })),
  ...products
    .map((product) => {
      const slug = typeof product.slug === 'string' ? product.slug.trim() : '';
      const path = slug ? `/product/${slug}` : `/details/${product.id}`;

      return {
        loc: toAbsoluteUrl(path),
        lastmod: buildDate,
        changefreq: 'weekly',
        priority: '0.7',
      };
    }),
];

const uniqueSitemapEntries = Array.from(
  new Map(sitemapEntries.map((entry) => [entry.loc, entry])).values(),
);

await Promise.all([
  writeFile(sitemapOutputPath, renderSitemap(uniqueSitemapEntries), 'utf8'),
  writeFile(
    robotsOutputPath,
    `User-agent: *\nAllow: /\n\nSitemap: ${toAbsoluteUrl('/sitemap.xml')}\n`,
    'utf8',
  ),
]);

console.info(
  `Generated Rich House SEO assets: ${uniqueSitemapEntries.length} sitemap URLs, robots.txt.`,
);

async function fetchCategories() {
  const categories = await fetchJson('/api/Category');
  if (!Array.isArray(categories)) {
    throw new Error('Expected /api/Category to return an array.');
  }

  return categories;
}

function buildCategoryEntries(categories, products) {
  const uniqueCategories = new Map();

  for (const category of categories) {
    if (!category || typeof category.slug !== 'string' || !category.slug.trim()) {
      continue;
    }

    uniqueCategories.set(category.slug.trim(), {
      name: typeof category.name === 'string' ? category.name.trim() : category.slug.trim(),
      slug: category.slug.trim(),
    });
  }

  for (const product of products) {
    if (!product || typeof product.categorySlug !== 'string' || !product.categorySlug.trim()) {
      continue;
    }

    const slug = product.categorySlug.trim();
    if (!uniqueCategories.has(slug)) {
      uniqueCategories.set(slug, {
        name: typeof product.categoryName === 'string' ? product.categoryName.trim() : slug,
        slug,
      });
    }
  }

  return Array.from(uniqueCategories.values());
}

async function fetchAllProducts() {
  const items = [];
  const seenIds = new Set();
  const pageSize = 100;
  let page = 1;
  let totalPages = 1;

  while (page <= totalPages) {
    const response = await fetchJson(
      `/api/Product/catalog?page=${page}&pageSize=${pageSize}&sort=featured`,
    );

    if (!response || !Array.isArray(response.items) || !response.meta) {
      throw new Error(`Unexpected catalog response shape on page ${page}.`);
    }

    totalPages = Number(response.meta.totalPages || 1);

    for (const product of response.items) {
      if (!product || typeof product.id !== 'number' || seenIds.has(product.id)) {
        continue;
      }

      seenIds.add(product.id);
      items.push(product);
    }

    page += 1;
  }

  return items;
}

async function fetchJson(path) {
  const response = await fetch(`${apiUrl}${path}`, {
    headers: {
      Accept: 'application/json',
    },
  });

  if (!response.ok) {
    throw new Error(`Failed to fetch ${path}: HTTP ${response.status}`);
  }

  return response.json();
}

function renderSitemap(entries) {
  const renderedEntries = entries
    .map(
      (entry) => `  <url>
    <loc>${escapeXml(entry.loc)}</loc>
    <lastmod>${escapeXml(entry.lastmod)}</lastmod>
    <changefreq>${escapeXml(entry.changefreq)}</changefreq>
    <priority>${escapeXml(entry.priority)}</priority>
  </url>`,
    )
    .join('\n');

  return `<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
${renderedEntries}
</urlset>
`;
}

function escapeXml(value) {
  return String(value)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&apos;');
}

function normalizeBaseUrl(value) {
  return value.replace(/\/+$/, '');
}

function toAbsoluteUrl(path) {
  return new URL(path.startsWith('/') ? path : `/${path}`, `${siteUrl}/`).href;
}
