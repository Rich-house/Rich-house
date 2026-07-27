import { DOCUMENT } from '@angular/common';
import { Inject, Injectable } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { ProductDetails } from '../../models/product.models';
import { richHouseBrand } from '../config/site-settings.config';
import { resolveImageUrl } from '../utils/image-url';

export interface RouteSeoConfig {
  description?: string;
  title?: string;
  type?: 'website' | 'article' | 'product';
}

interface SeoConfig {
  description?: string;
  image?: string | null;
  structuredData?: Record<string, unknown> | null;
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
      title: config.title || this.defaultTitle,
      description: config.description || this.defaultDescription,
      type: config.type || 'website',
      url,
    });
  }

  setPageSeo(config: SeoConfig): void {
    this.setSeo({
      title: config.title || this.defaultTitle,
      description: config.description || this.defaultDescription,
      image: config.image ?? null,
      structuredData: config.structuredData ?? null,
      type: config.type || 'website',
      url: config.url,
    });
  }

  setProductSeo(product: ProductDetails, url?: string): void {
    const title = richHouseBrand.browserTitle;
    const description =
      product.seoDescription?.trim() ||
      product.shortDescription?.trim() ||
      product.description?.trim() ||
      this.defaultDescription;
    const primaryImage = resolveImageUrl(product.imageUrls[0]);
    const productUrl = this.resolvePageUrl(url);

    this.setSeo({
      title,
      description,
      image: primaryImage,
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
    const imageUrl = config.image
      ? resolveImageUrl(config.image)
      : this.resolvePageUrl('/android-chrome-512x512.png');

    this.title.setTitle(config.title);
    this.updateNamedMeta('description', config.description);
    this.updateNamedMeta('application-name', richHouseBrand.name);
    this.updateNamedMeta('apple-mobile-web-app-title', richHouseBrand.name);
    this.updateNamedMeta('twitter:card', imageUrl ? 'summary_large_image' : 'summary');
    this.updateNamedMeta('twitter:title', config.title);
    this.updateNamedMeta('twitter:description', config.description);
    this.updateNamedMeta('twitter:image', imageUrl);
    this.updatePropertyMeta('og:site_name', richHouseBrand.name);
    this.updatePropertyMeta('og:type', config.type);
    this.updatePropertyMeta('og:title', config.title);
    this.updatePropertyMeta('og:description', config.description);
    this.updatePropertyMeta('og:url', pageUrl);
    this.updatePropertyMeta('og:image', imageUrl);

    this.updateStructuredData(
      config.structuredData ?? {
        '@context': 'https://schema.org',
        '@type': 'WebPage',
        name: config.title,
        description: config.description,
        url: pageUrl,
        isPartOf: {
          '@type': 'WebSite',
          name: richHouseBrand.name,
          url: this.resolvePageUrl('/'),
        },
      },
    );
  }

  private updateNamedMeta(name: string, content: string): void {
    this.meta.updateTag({ name, content });
  }

  private updatePropertyMeta(property: string, content: string): void {
    this.meta.updateTag({ property, content });
  }

  private updateStructuredData(data: Record<string, unknown>): void {
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
    const baseUrl = this.document.location.origin;
    const normalizedPath = path || this.document.location.pathname + this.document.location.search;
    return new URL(normalizedPath, `${baseUrl}/`).href;
  }
}
