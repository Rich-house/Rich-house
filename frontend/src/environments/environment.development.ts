export const environment = {
  production: false,
  api: {
    baseUrl: 'http://localhost:4000',
    assetBaseUrl: '',
    endpoints: {
      auth: 'api/Auth',
      admin: 'api/Admin',
      products: 'api/Product',
      categories: 'api/Category',
      reviews: 'api/Review',
      cart: 'api/Cart',
      currentUser: 'api/Auth/me',
    },
  },
} as const;
