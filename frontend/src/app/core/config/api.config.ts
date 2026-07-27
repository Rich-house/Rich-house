import { environment } from '../../../environments/environment';

const trimTrailingSlash = (value: string) => value.replace(/\/+$/, '');
const trimLeadingSlash = (value: string) => value.replace(/^\/+/, '');

export const apiBaseUrl = trimTrailingSlash(environment.api.baseUrl);
export const apiAssetBaseUrl = trimTrailingSlash(
  environment.api.assetBaseUrl || environment.api.baseUrl,
);

const buildUrl = (path: string): string => `${apiBaseUrl}/${trimLeadingSlash(path)}`;

export const apiConfig = {
  baseUrl: apiBaseUrl,
  assetBaseUrl: apiAssetBaseUrl,
  auth: buildUrl(environment.api.endpoints.auth),
  admin: buildUrl(environment.api.endpoints.admin),
  products: buildUrl(environment.api.endpoints.products),
  categories: buildUrl(environment.api.endpoints.categories),
  reviews: buildUrl(environment.api.endpoints.reviews),
  cart: buildUrl(environment.api.endpoints.cart),
  orders: buildUrl(environment.api.endpoints.orders),
  paymentCheckout: buildUrl(environment.api.endpoints.paymentCheckout),
  currentUser: buildUrl(environment.api.endpoints.currentUser),
};

export const isApiRequestUrl = (url: string): boolean =>
  url === apiBaseUrl || url.startsWith(`${apiBaseUrl}/`);
