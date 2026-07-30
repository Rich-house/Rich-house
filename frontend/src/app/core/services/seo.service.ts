import { DOCUMENT } from '@angular/common';
import { Inject, Injectable } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { ProductDetails } from '../../shared/models/product.models';
import {
  buildCanonicalUrl,
  buildProductTitle,
  publicSiteSettings,
  richHouseBrand,
  richHouseStoreLocations,
} from '../config/site-settings.config';
import { resolveImageUrl } from '../utils/image-url';

export interface SeoBreadcrumbItem {
  name: string;
  path: string;
}

type StructuredDataValue = Record<string, unknown> | Record<string, unknown>[];

export interface RouteSeoConfig {
  breadcrumbs?: SeoBreadcrumbItem[];
  description?: string;
  image?: string | null;
  keywords?: readonly string[] | string;
  robots?: string;
  structuredData?: StructuredDataValue | null;
  title?: string;
  type?: 'website' | 'article' | 'product';
}

interface SeoConfig {
  breadcrumbs?: SeoBreadcrumbItem[];
  description?: string;
  image?: string | null;
  keywords?: readonly string[] | string;
  robots?: string;
  structuredData?: StructuredDataValue | null;
  title?: string;
  type?: 'website' | 'article' | 'product';
  url?: string;
}

@Injectable({
  providedIn: 'root',
})
export class SeoService {
  readonly defaultTitle = richHouseBrand.browserTitle;
  readonly defaultDescription = 'Premium menswear, suits, shirts and formalwear from Rich House.';

  constructor(
    @Inject(DOCUMENT) private readonly document: Document,
    private readonly meta: Meta,
    private readonly title: Title,
  ) {}

  applyRouteSeo(config: RouteSeoConfig = {}, url?: string): void {
    this.setSeo({
      breadcrumbs: config.breadcrumbs,
      title: config.title || this.defaultTitle,
      description: config.description || this.defaultDescription,
      image: config.image ?? null,
      keywords: config.keywords,
      robots: config.robots,
      structuredData: config.structuredData ?? null,
      type: config.type || 'website',
      url,
    });
  }

  setPageSeo(config: SeoConfig): void {
    this.setSeo({
      breadcrumbs: config.breadcrumbs,
      title: config.title || this.defaultTitle,
      description: config.description || this.defaultDescription,
      image: config.image ?? null,
      keywords: config.keywords,
      robots: config.robots,
      structuredData: config.structuredData ?? null,
      type: config.type || 'website',
      url: config.url,
    });
  }

  setProductSeo(product: ProductDetails, url?: string): void {
    const title = buildProductTitle(product.seoTitle?.trim() || product.name);
    const description =
      product.seoDescription?.trim() ||
      product.shortDescription?.trim() ||
      product.description?.trim() ||
      this.defaultDescription;
    const primaryImage = resolveImageUrl(product.imageUrls[0]);
    const productUrl = this.resolvePageUrl(url);
    const breadcrumbs: SeoBreadcrumbItem[] = [
      { name: 'Home', path: '/' },
      { name: 'Shop', path: '/shop' },
    ];

    if (product.categoryName?.trim()) {
      breadcrumbs.push({
        name: product.categoryName.trim(),
        path: product.categorySlug?.trim() ? `/shop/${product.categorySlug.trim()}` : '/shop',
      });
    }

    breadcrumbs.push({
      name: product.name,
      path: product.slug?.trim() ? `/product/${product.slug.trim()}` : `/details/${product.id}`,
    });

    this.setSeo({
      breadcrumbs,
      title,
      description,
      image: primaryImage,
      keywords: [
        product.name,
        product.categoryName || 'menswear',
        richHouseBrand.name,
        'formalwear',
        'men suits',
        'shop Egypt',
      ],
      type: 'product',
      url: productUrl,
      structuredData: {
        '@context': 'https://schema.org',
        '@type': 'Product',
        name: product.name,
        description,
        image: product.imageUrls.map((imageUrl) => resolveImageUrl(imageUrl)),
        sku: product.sku || undefined,
        category: product.categoryName || undefined,
        brand: {
          '@type': 'Brand',
          name: richHouseBrand.name,
        },
        seller: {
          '@type': 'Organization',
          name: richHouseBrand.name,
          logo: this.resolvePageUrl(richHouseBrand.logoPath),
          url: this.resolvePageUrl('/'),
        },
        offers: {
          '@type': 'Offer',
          priceCurrency: 'EGP',
          price: product.offerPrice ?? product.price,
          availability:
            product.stockQuantity > 0
              ? 'https://schema.org/InStock'
              : 'https://schema.org/OutOfStock',
          url: productUrl,
        },
      },
    });
  }

  private setSeo(
    config: Required<Pick<SeoConfig, 'title' | 'description' | 'type'>> & SeoConfig,
  ): void {
    const pageUrl = this.resolvePageUrl(config.url);
    const imageUrl = this.resolveAbsoluteImageUrl(
      config.image ? resolveImageUrl(config.image) : richHouseBrand.socialImagePath,
    );
    const organizationLogoUrl = this.resolveAbsoluteImageUrl(richHouseBrand.logoPath);
    const keywords = this.normalizeKeywords(config.keywords);

    this.title.setTitle(config.title);
    this.updateNamedMeta('description', config.description);
    this.updateNamedMeta('keywords', keywords);
    this.updateNamedMeta('robots', config.robots || 'index,follow');
    this.updateNamedMeta('application-name', richHouseBrand.name);
    this.updateNamedMeta('apple-mobile-web-app-title', richHouseBrand.name);
    this.updateNamedMeta('twitter:card', imageUrl ? 'summary_large_image' : 'summary');
    this.updateNamedMeta('twitter:title', config.title);
    this.updateNamedMeta('twitter:description', config.description);
    this.updateNamedMeta('twitter:image', imageUrl);
    this.updateNamedMeta('twitter:image:alt', config.title);
    this.updatePropertyMeta('og:site_name', richHouseBrand.name);
    this.updatePropertyMeta('og:locale', 'en_US');
    this.updatePropertyMeta('og:type', config.type);
    this.updatePropertyMeta('og:title', config.title);
    this.updatePropertyMeta('og:description', config.description);
    this.updatePropertyMeta('og:url', pageUrl);
    this.updatePropertyMeta('og:image', imageUrl);
    this.updatePropertyMeta('og:image:alt', config.title);
    this.updateCanonicalLink(pageUrl);

    this.updateStructuredData(
      this.buildStructuredDataPayload(
        config,
        pageUrl,
        imageUrl,
        organizationLogoUrl,
      ),
    );
  }

  private updateNamedMeta(name: string, content: string): void {
    this.meta.updateTag({ name, content });
  }

  private updatePropertyMeta(property: string, content: string): void {
    this.meta.updateTag({ property, content });
  }

  private updateCanonicalLink(href: string): void {
    const existingLink = this.document.head.querySelector('link[rel="canonical"]');
    const link =
      existingLink instanceof HTMLLinkElement ? existingLink : this.document.createElement('link');

    link.setAttribute('rel', 'canonical');
    link.setAttribute('href', href);

    if (!existingLink) {
      this.document.head.appendChild(link);
    }
  }

  private updateStructuredData(data: StructuredDataValue): void {
    const existingScript = this.document.getElementById('rich-house-structured-data');
    const script =
      existingScript instanceof HTMLScriptElement
        ? existingScript
        : this.document.createElement('script');

    script.id = 'rich-house-structured-data';
    script.type = 'application/ld+json';
    script.text = JSON.stringify(data);

    if (!existingScript) {
      this.document.head.appendChild(script);
    }
  }

  private resolvePageUrl(path?: string): string {
    const normalizedPath = path || this.document.location.pathname + this.document.location.search;
    return buildCanonicalUrl(normalizedPath);
  }

  private resolveAbsoluteImageUrl(path: string): string {
    return /^(?:https?:)?\/\//i.test(path) ? path : this.resolvePageUrl(path);
  }

  private normalizeKeywords(keywords?: readonly string[] | string): string {
    const values = Array.isArray(keywords)
      ? keywords
      : typeof keywords === 'string'
        ? keywords.split(',')
        : richHouseBrand.defaultKeywords;

    return values
      .map((keyword) => keyword.trim())
      .filter(Boolean)
      .filter((value, index, array) => array.indexOf(value) === index)
      .join(', ');
  }

  private buildStructuredDataPayload(
    config: Required<Pick<SeoConfig, 'title' | 'description' | 'type'>> & SeoConfig,
    pageUrl: string,
    imageUrl: string,
    organizationLogoUrl: string,
  ): StructuredDataValue {
    const baseItems: Record<string, unknown>[] = [
      {
        '@context': 'https://schema.org',
        '@type': 'Organization',
        name: richHouseBrand.name,
        url: buildCanonicalUrl('/'),
        logo: organizationLogoUrl,
        image: imageUrl,
        sameAs: [
          publicSiteSettings.FacebookUrl,
          publicSiteSettings.InstagramUrl,
          `https://wa.me/${publicSiteSettings.WhatsAppNumber}`,
        ].filter(Boolean),
      },
      {
        '@context': 'https://schema.org',
        '@type': 'ClothingStore',
        name: richHouseBrand.name,
        url: buildCanonicalUrl('/'),
        logo: organizationLogoUrl,
        image: imageUrl,
        sameAs: [
          publicSiteSettings.FacebookUrl,
          publicSiteSettings.InstagramUrl,
          `https://wa.me/${publicSiteSettings.WhatsAppNumber}`,
        ].filter(Boolean),
        areaServed: 'EG',
        department: richHouseStoreLocations.map((location) => ({
          '@type': 'Place',
          name: location.name,
          url: location.directionsUrl,
          address: {
            '@type': 'PostalAddress',
            streetAddress: location.address,
            addressCountry: 'EG',
          },
        })),
      },
      {
        '@context': 'https://schema.org',
        '@type': 'WebSite',
        name: richHouseBrand.name,
        url: buildCanonicalUrl('/'),
        description: richHouseBrand.shortDescription,
        potentialAction: {
          '@type': 'SearchAction',
          target: `${buildCanonicalUrl('/shop')}?search={search_term_string}`,
          'query-input': 'required name=search_term_string',
        },
      },
      {
        '@context': 'https://schema.org',
        '@type': 'WebPage',
        name: config.title,
        description: config.description,
        url: pageUrl,
        image: imageUrl,
        isPartOf: {
          '@type': 'WebSite',
          name: richHouseBrand.name,
          url: buildCanonicalUrl('/'),
        },
      },
    ];

    const breadcrumbItem = this.buildBreadcrumbStructuredData(config.breadcrumbs);
    if (breadcrumbItem) {
      baseItems.push(breadcrumbItem);
    }

    const additionalItems =
      config.structuredData === null || config.structuredData === undefined
        ? []
        : Array.isArray(config.structuredData)
          ? config.structuredData
          : [config.structuredData];

    return [...baseItems, ...additionalItems];
  }

  private buildBreadcrumbStructuredData(
    breadcrumbs?: SeoBreadcrumbItem[],
  ): Record<string, unknown> | null {
    if (!breadcrumbs?.length) {
      return null;
    }

    return {
      '@context': 'https://schema.org',
      '@type': 'BreadcrumbList',
      itemListElement: breadcrumbs.map((breadcrumb, index) => ({
        '@type': 'ListItem',
        position: index + 1,
        name: breadcrumb.name,
        item: this.resolvePageUrl(breadcrumb.path),
      })),
    };
  }
}
