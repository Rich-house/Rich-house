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
      orders: 'api/Order',
      paymentCheckout: 'api/Payment/checkout',
      currentUser: 'api/Auth/me',
    },
  },
} as const;
