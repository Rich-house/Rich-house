export const environment = {
  production: true,
  api: {
    baseUrl: 'https://api.example.com',
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
