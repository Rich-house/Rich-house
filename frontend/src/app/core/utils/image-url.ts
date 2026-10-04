import { environment } from '../../../environments/environment';

const trimTrailingSlash = (value: string) => value.replace(/\/+$/, '');
const trimLeadingSlash = (value: string) => value.replace(/^\/+/, '');

const assetBaseUrl = trimTrailingSlash(environment.api.assetBaseUrl || environment.api.baseUrl);
const absoluteUrlPattern = /^(?:https?:)?\/\//i;
const passthroughProtocolPattern = /^(?:data:|blob:)/i;
const frontendAssetPattern = /^\/?assets\//i;
const backendRelativePattern = /^\/?(?:uploads|images|catalog)\//i;
const filesystemPathPattern = /^(?:[a-z]:[\\/]|\/(?:home|users|var|tmp|mnt|srv)\b)/i;

export const productPlaceholderImage = 'assets/placeholders/product-placeholder.svg';

const buildBackendAssetUrl = (path: string): string => `${assetBaseUrl}/${trimLeadingSlash(path)}`;

export const resolveImageUrl = (path: string | null | undefined): string => {
  if (!path) {
    return productPlaceholderImage;
  }

  const normalizedPath = path.replace(/\\/g, '/').trim();
  if (!normalizedPath) {
    return productPlaceholderImage;
  }

  if (absoluteUrlPattern.test(normalizedPath) || passthroughProtocolPattern.test(normalizedPath)) {
    return normalizedPath;
  }

  if (filesystemPathPattern.test(normalizedPath)) {
    return productPlaceholderImage;
  }

  if (frontendAssetPattern.test(normalizedPath)) {
    return normalizedPath.startsWith('/') ? normalizedPath : normalizedPath;
  }

  if (backendRelativePattern.test(normalizedPath) || normalizedPath.startsWith('/')) {
    return buildBackendAssetUrl(normalizedPath);
  }

  return buildBackendAssetUrl(normalizedPath);
};

export const resolveCatalogThumbnailUrl = (path: string | null | undefined): string => {
  if (!path) {
    return productPlaceholderImage;
  }

  const thumbnailPath = path
    .replace('/catalog/products/', '/catalog/thumbnails/')
    .replace(/\/images\/(products|categories)\/([^/?#]+)\.(?:jpe?g|png|webp)([?#].*)?$/i, '/images/$1/thumbnails/$2.webp$3');
  return resolveImageUrl(thumbnailPath);
};

export const resolveCatalogResponsiveThumbnailUrl = (
  path: string | null | undefined,
): string => {
  if (!path) {
    return productPlaceholderImage;
  }

  const responsivePath = path
    .replace(/\/catalog\/(?:products|thumbnails)\//i, '/catalog/responsive/384/')
    .replace(
      /\/images\/(products|categories)\/(?:thumbnails\/)?([^/?#]+)\.(?:jpe?g|png|webp)([?#].*)?$/i,
      '/images/$1/responsive/384/$2.webp$3',
    );
  return resolveImageUrl(responsivePath);
};

export const resolveOptimizedProductImageUrl = (path: string | null | undefined): string => {
  if (!path) {
    return productPlaceholderImage;
  }

  const optimizedPath = path.replace(
    /\/images\/products\/([^/?#]+)\.(?:jpe?g|png|webp)([?#].*)?$/i,
    '/images/products/optimized/$1.webp$2',
  );
  return resolveImageUrl(optimizedPath);
};

export const resolveResponsiveProductImageUrl = (
  path: string | null | undefined,
): string => {
  if (!path) {
    return productPlaceholderImage;
  }

  const responsivePath = path.replace(
    /\/images\/products\/(?:optimized\/)?([^/?#]+)\.(?:jpe?g|png|webp)([?#].*)?$/i,
    '/images/products/responsive/720/$1.webp$2',
  );
  return resolveImageUrl(responsivePath);
};
