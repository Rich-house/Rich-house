export interface RichHousePublicSiteSettings {
  FacebookUrl: string;
  InstagramUrl: string;
  WhatsAppNumber: string;
}

export const defaultWhatsAppMessage =
  'Hello Rich House, I would like to ask about your products.';

export const richHouseBrand = {
  name: 'Rich House',
  eyebrow: 'Premium Menswear',
  tagline: 'Tailored confidence for modern occasions.',
  shortDescription:
    'Rich House is a premium menswear destination created for men who value elegance, confidence, and attention to detail.',
  heroHeading: 'Modern menswear, tailored for every important entrance.',
  heroCopy:
    'Discover refined tailoring, elevated essentials, and occasion-ready pieces shaped for work, celebration, and everyday confidence.',
} as const;

export const publicSiteSettings: RichHousePublicSiteSettings = {
  FacebookUrl: 'https://www.facebook.com/share/1YQkSUeoBj/?mibextid=wwXIfr',
  InstagramUrl:
    'https://www.instagram.com/rich.house01?igsh=MWhwZzF6ZGw2dnp2cA%3D%3D&utm_source=qr',
  WhatsAppNumber: '201024682081',
};

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

export const buildProductWhatsAppUrl = (productName?: string | null): string | null =>
  buildWhatsAppUrl(publicSiteSettings.WhatsAppNumber, buildProductInquiryMessage(productName));
