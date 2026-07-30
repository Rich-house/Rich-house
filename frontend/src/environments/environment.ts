export const environment = {
  production: true,
  api: {
    baseUrl: 'https://richhouse-api.runasp.net',
    assetBaseUrl: 'https://richhouse-api.runasp.net',
    endpoints: {
      auth: 'api/Auth',
      admin: 'api/Admin',
      adminCatalog: 'api/admin/catalog',
      products: 'api/Product',
      categories: 'api/Category',
      offers: 'api/Offer',
      reviews: 'api/Review',
      cart: 'api/Cart',
      currentUser: 'api/Auth/me',
    },
  },
} as const;
