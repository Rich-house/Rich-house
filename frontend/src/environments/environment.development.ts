export const environment = {
  production: false,
  api: {
    baseUrl: 'http://localhost:4000',
    assetBaseUrl: '',
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
