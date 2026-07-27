import { Routes } from '@angular/router';
import { ContentPageData } from './components/content-page/content-page';
import { richHouseBrand } from './core/config/site-settings.config';
import { authGuard } from './guards/auth-guard';

const defaultStoreDescription = 'Premium menswear, suits, shirts and formalwear from Rich House.';
const browserTitle = richHouseBrand.browserTitle;

const sizeGuideContent: ContentPageData = {
  eyebrow: 'Size Guide',
  title: 'Choose the fit that feels considered, not complicated.',
  intro:
    'Use these provisional size ranges as a quick reference while the full Rich House measurement guide is prepared for admin-managed content.',
  sections: [
    {
      title: 'Tailoring',
      paragraphs: [
        'Suiting, coats, and structured outerwear typically follow European numeric sizing.',
      ],
      bullets: ['46, 48, 50, 52, 54, 56, 58, 60'],
    },
    {
      title: 'Shirts and Knitwear',
      paragraphs: [
        'Smart shirts, polos, and lightweight layers are currently arranged by standard alpha sizing.',
      ],
      bullets: ['S, M, L, XL, XXL'],
    },
    {
      title: 'Need help choosing?',
      paragraphs: [
        'If you are deciding between two sizes, order the closer fit for your preferred silhouette and review the product description for any notes about structure or ease.',
      ],
    },
  ],
};

const returnsContent: ContentPageData = {
  eyebrow: 'Returns and Exchanges',
  title: 'A smoother exchange experience starts with clear expectations.',
  intro:
    'Rich House return and exchange copy is provisional for now and is structured to be replaced later from site settings or the admin dashboard.',
  sections: [
    {
      title: 'Condition',
      paragraphs: [
        'Items should be returned in their original condition, unworn, and with any relevant packaging or tags kept intact where applicable.',
      ],
    },
    {
      title: 'Review before sending',
      paragraphs: [
        'Check sizing, finish, and overall fit soon after delivery so any exchange request can be handled efficiently.',
      ],
    },
    {
      title: 'Support',
      paragraphs: [
        'When Rich House contact operations are finalized, this page can be updated with the exact process, timings, and any category-specific exclusions.',
      ],
    },
  ],
};

const privacyContent: ContentPageData = {
  eyebrow: 'Privacy',
  title: 'Your information should support the order experience, not distract from it.',
  intro:
    'This provisional privacy overview avoids invented legal claims and is ready to be replaced with the client’s final policy text.',
  sections: [
    {
      title: 'Information used',
      paragraphs: [
        'Order details, contact information, and account data may be used to process purchases, support customer service, and improve the storefront experience.',
      ],
    },
    {
      title: 'Security',
      paragraphs: [
        'Sensitive authentication details are handled through the backend and should not be exposed in the customer interface.',
      ],
    },
    {
      title: 'Final policy',
      paragraphs: [
        'Before launch, Rich House should publish a final approved privacy policy reflecting real business operations, legal requirements, and hosting arrangements.',
      ],
    },
  ],
};

const termsContent: ContentPageData = {
  eyebrow: 'Terms',
  title: 'Clear storefront terms keep expectations aligned on both sides.',
  intro:
    'These provisional terms are intentionally high-level and should be replaced with approved production copy before public launch.',
  sections: [
    {
      title: 'Ordering',
      paragraphs: [
        'Availability, pricing, and promotional visibility may change as the catalog is refined and merchandise information is updated.',
      ],
    },
    {
      title: 'Product presentation',
      paragraphs: [
        'Product photography, fit notes, and styling descriptions are designed to guide the shopping journey, but final product details remain editable by the Rich House team.',
      ],
    },
    {
      title: 'Store updates',
      paragraphs: [
        'Rich House reserves the ability to refine storefront content, policies, and configuration as the business moves toward production readiness.',
      ],
    },
  ],
};

export const routes: Routes = [
  {
    path: '',
    title: browserTitle,
    data: {
      seo: {
        description: defaultStoreDescription,
        type: 'website',
      },
    },
    loadComponent: () => import('./components/home/home').then((m) => m.Home),
  },
  { path: 'home', redirectTo: '', pathMatch: 'full' },
  {
    path: 'shop',
    title: browserTitle,
    data: {
      seo: {
        description:
          'Browse the Rich House menswear collection, including suits, shirts, elevated essentials, and formalwear.',
      },
    },
    loadComponent: () => import('./components/product/product').then((m) => m.Product),
  },
  {
    path: 'shop/:categorySlug',
    title: browserTitle,
    data: {
      seo: {
        description:
          'Explore Rich House categories, from tailoring and shirts to occasion-ready menswear essentials.',
      },
    },
    loadComponent: () => import('./components/product/product').then((m) => m.Product),
  },
  {
    path: 'offers',
    title: browserTitle,
    data: {
      offersOnly: true,
      seo: {
        description:
          'Discover current Rich House offers across premium menswear, tailoring, and formalwear.',
      },
    },
    loadComponent: () => import('./components/product/product').then((m) => m.Product),
  },
  {
    path: 'product/:slug',
    title: browserTitle,
    data: {
      seo: {
        description:
          'Explore Rich House product details, gallery images, sizing, and pricing for premium menswear pieces.',
        type: 'product',
      },
    },
    loadComponent: () => import('./components/details/details').then((m) => m.Details),
  },
  { path: 'product', redirectTo: 'shop', pathMatch: 'full' },
  {
    path: 'dashboard',
    title: browserTitle,
    data: {
      seo: {
        description:
          'Manage the Rich House catalog, categories, and users from the admin dashboard.',
      },
    },
    loadComponent: () => import('./components/dashboard/dashboard').then((m) => m.Dashboard),
    canActivate: [authGuard],
  },
  {
    path: 'dashboard/add-product',
    title: browserTitle,
    loadComponent: () =>
      import('./components/add-product/add-product').then((m) => m.AddProductComponent),
    canActivate: [authGuard],
  },
  {
    path: 'dashboard/edit-product/:id',
    title: browserTitle,
    loadComponent: () =>
      import('./components/edit-product/edit-product').then((m) => m.EditProductComponent),
    canActivate: [authGuard],
  },
  {
    path: 'dashboard/add-category',
    title: browserTitle,
    loadComponent: () =>
      import('./components/add-category/add-category').then((m) => m.AddCategoryComponent),
    canActivate: [authGuard],
  },
  {
    path: 'dashboard/edit-category/:id',
    title: browserTitle,
    loadComponent: () =>
      import('./components/edit-category/edit-category').then((m) => m.EditCategory),
    canActivate: [authGuard],
  },
  {
    path: 'profile',
    title: browserTitle,
    loadComponent: () => import('./components/profile/profile').then((m) => m.Profile),
    canActivate: [authGuard],
  },
  {
    path: 'details/:id',
    title: browserTitle,
    data: {
      seo: {
        description:
          'Explore Rich House product details, gallery images, sizing, and pricing for premium menswear pieces.',
        type: 'product',
      },
    },
    loadComponent: () => import('./components/details/details').then((m) => m.Details),
  },
  {
    path: 'cart',
    title: browserTitle,
    loadComponent: () => import('./components/cart/cart').then((m) => m.Cart),
  },
  {
    path: 'contact',
    title: browserTitle,
    data: {
      seo: {
        description:
          'Contact Rich House for sizing, availability, styling guidance, and customer support.',
      },
    },
    loadComponent: () => import('./components/help-center/help-center').then((m) => m.HelpCenter),
  },
  { path: 'helpcenter', redirectTo: 'contact', pathMatch: 'full' },
  {
    path: 'create-order',
    title: browserTitle,
    loadComponent: () => import('./components/order/order').then((m) => m.OrderComponent),
  },
  {
    path: 'about',
    title: browserTitle,
    data: {
      seo: {
        description:
          'Learn about the Rich House menswear brand, its visual direction, and its approach to modern formalwear.',
      },
    },
    loadComponent: () => import('./components/aboutus/aboutus').then((m) => m.Aboutus),
  },
  { path: 'about/vision', redirectTo: 'about', pathMatch: 'full' },
  { path: 'about/values', redirectTo: 'about', pathMatch: 'full' },
  {
    path: 'size-guide',
    title: browserTitle,
    data: {
      content: sizeGuideContent,
      seo: {
        description:
          'Use the Rich House size guide for tailoring, shirts, and knitwear across current catalog sizing.',
      },
    },
    loadComponent: () =>
      import('./components/content-page/content-page').then((m) => m.ContentPage),
  },
  {
    path: 'returns',
    title: browserTitle,
    data: {
      content: returnsContent,
      seo: {
        description:
          'Review the current Rich House returns and exchanges guidance for menswear orders.',
      },
    },
    loadComponent: () =>
      import('./components/content-page/content-page').then((m) => m.ContentPage),
  },
  {
    path: 'privacy',
    title: browserTitle,
    data: {
      content: privacyContent,
      seo: {
        description:
          'Read the current Rich House privacy overview for account, order, and storefront information handling.',
      },
    },
    loadComponent: () =>
      import('./components/content-page/content-page').then((m) => m.ContentPage),
  },
  {
    path: 'terms',
    title: browserTitle,
    data: {
      content: termsContent,
      seo: {
        description: 'Review the current Rich House storefront terms and ordering guidance.',
      },
    },
    loadComponent: () =>
      import('./components/content-page/content-page').then((m) => m.ContentPage),
  },
  {
    path: 'login',
    title: browserTitle,
    data: {
      seo: {
        description: 'Sign in to your Rich House account or access the Rich House admin dashboard.',
      },
    },
    loadComponent: () => import('./components/login/login').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    title: browserTitle,
    loadComponent: () => import('./components/register/register').then((m) => m.RegisterComponent),
  },
  {
    path: 'confirmemail',
    title: browserTitle,
    loadComponent: () =>
      import('./components/confirmemail/confirmemail').then((m) => m.ConfirmEmailComponent),
  },
  {
    path: '**',
    title: browserTitle,
    loadComponent: () => import('./components/not-found/not-found').then((m) => m.NotFound),
  },
];
