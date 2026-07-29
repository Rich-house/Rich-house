export interface RichHousePublicSiteSettings {
  FacebookUrl: string;
  InstagramUrl: string;
  WhatsAppNumber: string;
}

export interface RichHouseStoreLocation {
  address: string;
  directionsUrl: string;
  name: string;
}

export const defaultWhatsAppMessage = 'Hello Rich House, I would like to ask about your products.';

export const richHouseBrand = {
  name: 'Rich House',
  adminTitle: 'Rich House Admin',
  browserTitle: 'Rich House',
  eyebrow: 'RICH HOUSE MENSWEAR',
  fallbackProductName: 'Rich House product',
  homeTitle: 'Rich House',
  logoAlt: 'Rich House',
  logoPath: 'assets/brand/rich-house-logo-transparent.png',
  logoWebpPath: 'assets/brand/rich-house-logo.webp',
  logoSmallWebpPath: 'assets/brand/rich-house-logo-small.webp',
  iconPath: 'assets/brand/rich-house-icon-512.png',
  shortDescription:
    'Rich House is a premium menswear destination shaped around refined tailoring, elevated essentials, and confident modern dressing.',
  socialImagePath: 'assets/brand/rich-house-logo.webp',
  heroHeading: 'Crafted for the Modern Gentleman',
  heroCopy: 'Refined suits and timeless menswear for every defining moment.',
} as const;

export const richHouseUi = {
  modalConfirmColor: '#1e1e1e',
} as const;

export const publicSiteSettings: RichHousePublicSiteSettings = {
  FacebookUrl: 'https://www.facebook.com/share/1YQkSUeoBj/?mibextid=wwXIfr',
  InstagramUrl:
    'https://www.instagram.com/rich.house01?igsh=MWhwZzF6ZGw2dnp2cA%3D%3D&utm_source=qr',
  WhatsAppNumber: '201024682081',
};

export const buildGoogleMapsSearchUrl = (address: string): string =>
  `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(address)}`;

export const richHouseStoreLocations: RichHouseStoreLocation[] = [
  {
    name: 'Nasr City Branch',
    address: '23 Abbas El Akkad Street, Nasr City, Cairo, Egypt',
    directionsUrl: buildGoogleMapsSearchUrl('23 Abbas El Akkad Street, Nasr City, Cairo, Egypt'),
  },
  {
    name: 'Mohandessin Branch',
    address: '41 Shehab Street, Mohandessin, Giza, Egypt',
    directionsUrl: buildGoogleMapsSearchUrl('41 Shehab Street, Mohandessin, Giza, Egypt'),
  },
];

export const footerInformationLinks = [
  { label: 'About', route: '/about' },
  { label: 'Contact', route: '/contact' },
  { label: 'Size Guide', route: '/size-guide' },
  { label: 'Returns and Exchanges', route: '/returns' },
  { label: 'Privacy Policy', route: '/privacy' },
  { label: 'Terms', route: '/terms' },
] as const;

export const primaryNavigationLinks = [
  { label: 'Home', route: '/' },
  { label: 'Shop', route: '/shop' },
  { label: 'Offers', route: '/offers' },
  { label: 'About', route: '/about' },
  { label: 'Contact', route: '/contact' },
] as const;

export const buildWhatsAppUrl = (
  phoneNumber: string,
  message = defaultWhatsAppMessage,
): string | null => {
  const normalized = phoneNumber.replace(/[^\d]/g, '');
  return normalized ? `https://wa.me/${normalized}?text=${encodeURIComponent(message)}` : null;
};

export const buildProductInquiryMessage = (productName?: string | null): string =>
  productName?.trim()
    ? `Hello Rich House, I would like to ask about ${productName.trim()}.`
    : defaultWhatsAppMessage;

export const buildProductTitle = (productName?: string | null): string =>
  productName?.trim()
    ? productName.includes(richHouseBrand.name)
      ? productName.trim()
      : `${productName.trim()} | ${richHouseBrand.name}`
    : richHouseBrand.browserTitle;

export const buildCategoryTitle = (categoryName?: string | null): string =>
  categoryName?.trim()
    ? categoryName.includes(richHouseBrand.name)
      ? categoryName.trim()
      : `${categoryName.trim()} | ${richHouseBrand.name}`
    : richHouseBrand.browserTitle;
