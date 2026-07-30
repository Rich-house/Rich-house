import { Routes } from '@angular/router';
import { ContentPageData } from './shared/components/content-page/content-page';
import { richHouseBrand } from './core/config/site-settings.config';
import { authGuard } from './core/guards/auth-guard';
import { SeoBreadcrumbItem } from './core/services/seo.service';

const defaultStoreDescription = 'Premium menswear, suits, shirts and formalwear from Rich House.';
const browserTitle = richHouseBrand.browserTitle;
const adminRobots = 'noindex,nofollow';
const homeBreadcrumbs: SeoBreadcrumbItem[] = [{ name: 'Home', path: '/' }];
const shopBreadcrumbs: SeoBreadcrumbItem[] = [...homeBreadcrumbs, { name: 'Shop', path: '/shop' }];
const offersBreadcrumbs: SeoBreadcrumbItem[] = [
  ...homeBreadcrumbs,
  { name: 'Offers', path: '/offers' },
];
const aboutBreadcrumbs: SeoBreadcrumbItem[] = [
  ...homeBreadcrumbs,
  { name: 'About', path: '/about' },
];
const contactBreadcrumbs: SeoBreadcrumbItem[] = [
  ...homeBreadcrumbs,
  { name: 'Contact', path: '/contact' },
];

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
    title: richHouseBrand.homeTitle,
    data: {
      seo: {
        description: defaultStoreDescription,
        keywords: richHouseBrand.defaultKeywords,
        breadcrumbs: homeBreadcrumbs,
        type: 'website',
      },
    },
    loadComponent: () => import('./features/home/home/home').then((m) => m.Home),
  },
  { path: 'home', redirectTo: '', pathMatch: 'full' },
  {
    path: 'shop',
    title: `Shop | ${richHouseBrand.name}`,
    data: {
      seo: {
        description:
          'Browse the Rich House menswear collection, including suits, shirts, elevated essentials, and formalwear.',
        keywords: [
          richHouseBrand.name,
          'shop',
          'menswear',
          'formalwear',
          'shirts',
          'suits',
          'belts',
          'shoes',
        ],
        breadcrumbs: shopBreadcrumbs,
      },
    },
    loadComponent: () => import('./features/products/product-list/product').then((m) => m.Product),
  },
  {
    path: 'shop/:categorySlug',
    title: `Shop | ${richHouseBrand.name}`,
    data: {
      seo: {
        description:
          'Explore Rich House categories, from tailoring and shirts to occasion-ready menswear essentials.',
        keywords: [
          richHouseBrand.name,
          'category',
          'menswear',
          'formalwear',
        ],
        breadcrumbs: shopBreadcrumbs,
      },
    },
    loadComponent: () => import('./features/products/product-list/product').then((m) => m.Product),
  },
  {
    path: 'offers',
    title: `Offers | ${richHouseBrand.name}`,
    data: {
      offersOnly: true,
      seo: {
        description:
          'Discover current Rich House offers across premium menswear, tailoring, and formalwear.',
        keywords: [
          richHouseBrand.name,
          'offers',
          'discounts',
          'menswear sale',
          'formalwear offers',
        ],
        breadcrumbs: offersBreadcrumbs,
      },
    },
    loadComponent: () => import('./features/products/product-list/product').then((m) => m.Product),
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
    loadComponent: () => import('./features/products/product-details/details').then((m) => m.Details),
  },
  { path: 'product', redirectTo: 'shop', pathMatch: 'full' },
  {
    path: 'admin',
    title: richHouseBrand.adminTitle,
    data: {
      seo: {
        description: 'Secure administrator access for the Rich House dashboard.',
        robots: adminRobots,
      },
    },
    loadComponent: () => import('./features/auth/login/login').then((m) => m.LoginComponent),
  },
  {
    path: 'admin/login',
    title: richHouseBrand.adminTitle,
    data: {
      seo: {
        description: 'Secure administrator access for the Rich House dashboard.',
        robots: adminRobots,
      },
    },
    loadComponent: () => import('./features/auth/login/login').then((m) => m.LoginComponent),
  },
  {
    path: 'dashboard',
    title: richHouseBrand.adminTitle,
    data: {
      managementOnly: true,
      seo: {
        description:
          'Manage the Rich House catalog, categories, and users from the admin dashboard.',
        robots: adminRobots,
      },
    },
    loadComponent: () => import('./features/admin/dashboard/dashboard').then((m) => m.Dashboard),
    canActivate: [authGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'overview',
      },
      {
        path: 'overview',
        loadComponent: () =>
          import('./features/admin/overview/overview').then((m) => m.AdminOverviewComponent),
      },
      {
        path: 'products',
        loadComponent: () =>
          import('./features/admin/products/products').then((m) => m.AdminProductsComponent),
      },
      {
        path: 'add-product',
        loadComponent: () =>
          import('./features/admin/product-form/product-form').then((m) => m.AdminProductFormComponent),
      },
      {
        path: 'edit-product/:id',
        loadComponent: () =>
          import('./features/admin/product-form/product-form').then((m) => m.AdminProductFormComponent),
      },
      {
        path: 'categories',
        loadComponent: () =>
          import('./features/admin/categories/categories').then((m) => m.AdminCategoriesComponent),
      },
      {
        path: 'add-category',
        loadComponent: () =>
          import('./features/admin/category-form/category-form').then((m) => m.AdminCategoryFormComponent),
      },
      {
        path: 'edit-category/:id',
        loadComponent: () =>
          import('./features/admin/category-form/category-form').then((m) => m.AdminCategoryFormComponent),
      },
      {
        path: 'offers',
        loadComponent: () =>
          import('./features/admin/offers/offers').then((m) => m.AdminOffersComponent),
      },
      {
        path: 'add-offer',
        loadComponent: () =>
          import('./features/admin/offer-form/offer-form').then((m) => m.AdminOfferFormComponent),
      },
      {
        path: 'edit-offer/:id',
        loadComponent: () =>
          import('./features/admin/offer-form/offer-form').then((m) => m.AdminOfferFormComponent),
      },
      {
        path: 'users',
        data: {
          superAdminOnly: true,
        },
        canActivate: [authGuard],
        loadComponent: () =>
          import('./features/admin/users/users').then((m) => m.AdminUsersComponent),
      },
    ],
  },
  {
    path: 'profile',
    title: browserTitle,
    loadComponent: () => import('./features/auth/profile/profile').then((m) => m.Profile),
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
    loadComponent: () => import('./features/products/product-details/details').then((m) => m.Details),
  },
  {
    path: 'cart',
    title: browserTitle,
    loadComponent: () => import('./features/cart/cart/cart').then((m) => m.Cart),
  },
  {
    path: 'contact',
    title: `Contact | ${richHouseBrand.name}`,
    data: {
      seo: {
        description:
          'Contact Rich House for sizing, availability, styling guidance, and customer support.',
        keywords: [
          richHouseBrand.name,
          'contact',
          'WhatsApp',
          'store location',
          'customer support',
        ],
        breadcrumbs: contactBreadcrumbs,
      },
    },
    loadComponent: () => import('./features/help/help-center/help-center').then((m) => m.HelpCenter),
  },
  { path: 'helpcenter', redirectTo: 'contact', pathMatch: 'full' },
  {
    path: 'create-order',
    title: browserTitle,
    loadComponent: () => import('./features/order/order/order').then((m) => m.OrderComponent),
  },
  {
    path: 'about',
    title: `About | ${richHouseBrand.name}`,
    data: {
      seo: {
        description:
          'Learn about the Rich House menswear brand, its visual direction, and its approach to modern formalwear.',
        keywords: [
          richHouseBrand.name,
          'about',
          'menswear brand',
          'formalwear',
          'tailoring',
        ],
        breadcrumbs: aboutBreadcrumbs,
      },
    },
    loadComponent: () => import('./features/about/aboutus/aboutus').then((m) => m.Aboutus),
  },
  { path: 'about/vision', redirectTo: 'about', pathMatch: 'full' },
  { path: 'about/values', redirectTo: 'about', pathMatch: 'full' },
  {
    path: 'size-guide',
    title: `Size Guide | ${richHouseBrand.name}`,
    data: {
      content: sizeGuideContent,
      seo: {
        description:
          'Use the Rich House size guide for tailoring, shirts, and knitwear across current catalog sizing.',
        keywords: [richHouseBrand.name, 'size guide', 'menswear sizes', 'suits', 'shirts'],
        breadcrumbs: [...homeBreadcrumbs, { name: 'Size Guide', path: '/size-guide' }],
      },
    },
    loadComponent: () =>
      import('./shared/components/content-page/content-page').then((m) => m.ContentPage),
  },
  {
    path: 'returns',
    title: `Returns and Exchanges | ${richHouseBrand.name}`,
    data: {
      content: returnsContent,
      seo: {
        description:
          'Review the current Rich House returns and exchanges guidance for menswear orders.',
        keywords: [richHouseBrand.name, 'returns', 'exchanges', 'order support'],
        breadcrumbs: [...homeBreadcrumbs, { name: 'Returns and Exchanges', path: '/returns' }],
      },
    },
    loadComponent: () =>
      import('./shared/components/content-page/content-page').then((m) => m.ContentPage),
  },
  {
    path: 'privacy',
    title: `Privacy Policy | ${richHouseBrand.name}`,
    data: {
      content: privacyContent,
      seo: {
        description:
          'Read the current Rich House privacy overview for account, order, and storefront information handling.',
        keywords: [richHouseBrand.name, 'privacy policy', 'customer information', 'orders'],
        breadcrumbs: [...homeBreadcrumbs, { name: 'Privacy Policy', path: '/privacy' }],
      },
    },
    loadComponent: () =>
      import('./shared/components/content-page/content-page').then((m) => m.ContentPage),
  },
  {
    path: 'terms',
    title: `Terms | ${richHouseBrand.name}`,
    data: {
      content: termsContent,
      seo: {
        description: 'Review the current Rich House storefront terms and ordering guidance.',
        keywords: [richHouseBrand.name, 'terms', 'store policy', 'ordering'],
        breadcrumbs: [...homeBreadcrumbs, { name: 'Terms', path: '/terms' }],
      },
    },
    loadComponent: () =>
      import('./shared/components/content-page/content-page').then((m) => m.ContentPage),
  },
  {
    path: 'login',
    redirectTo: 'admin/login',
    pathMatch: 'full',
  },
  {
    path: 'register',
    title: browserTitle,
    loadComponent: () => import('./features/auth/register/register').then((m) => m.RegisterComponent),
  },
  {
    path: 'confirmemail',
    title: browserTitle,
    loadComponent: () =>
      import('./features/auth/confirmemail/confirmemail').then((m) => m.ConfirmEmailComponent),
  },
  {
    path: '**',
    title: browserTitle,
    loadComponent: () => import('./components/not-found/not-found').then((m) => m.NotFound),
  },
];
