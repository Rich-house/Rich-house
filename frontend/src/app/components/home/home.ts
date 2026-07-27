import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { map } from 'rxjs/operators';
import { ProductsService } from '../../Services/product';
import { apiConfig } from '../../core/config/api.config';
import { SeoService } from '../../core/services/seo.service';
import {
  buildWhatsAppUrl,
  publicSiteSettings,
  richHouseBrand,
} from '../../core/config/site-settings.config';
import {
  productPlaceholderImage,
  resolveCatalogThumbnailUrl,
  resolveImageUrl,
} from '../../core/utils/image-url';
import { describeApiError, type ApiErrorKind } from '../../core/utils/http-error';
import { CategoryItem } from '../../models/category.models';
import { ProductCard } from '../../models/product.models';
import { ImageFallbackDirective } from '../../shared/directives/image-fallback.directive';
import { ProductCardComponent } from '../../shared/components/product-card/product-card';

@Component({
  selector: 'app-home',
  imports: [CommonModule, RouterLink, ProductCardComponent, ImageFallbackDirective],
  templateUrl: './home.html',
  styleUrl: './home.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Home {
  private readonly destroyRef = inject(DestroyRef);
  private readonly productsService = inject(ProductsService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly router = inject(Router);
  private readonly seoService = inject(SeoService);

  readonly brand = richHouseBrand;
  readonly fallbackImage = productPlaceholderImage;
  readonly heroImageUrl = 'assets/hero/rich-house-hero.webp';
  readonly publicSiteSettings = publicSiteSettings;
  readonly whatsAppUrl = buildWhatsAppUrl(publicSiteSettings.WhatsAppNumber);
  readonly benefits = [
    {
      icon: 'bi-patch-check',
      title: 'Refined quality',
      copy: 'Thoughtful pieces selected for fit, finish, and lasting wardrobe value.',
    },
    {
      icon: 'bi-shield-lock',
      title: 'Secure ordering',
      copy: 'A cleaner storefront experience designed to keep browsing and checkout straightforward.',
    },
    {
      icon: 'bi-box-seam',
      title: 'Delivery ready',
      copy: 'Catalog, availability, and offer visibility are structured for a smooth fulfillment workflow.',
    },
    {
      icon: 'bi-arrow-repeat',
      title: 'Exchange friendly',
      copy: 'Policy messaging is ready to be refined later from configurable site content.',
    },
  ] as const;

  readonly socialCards = [
    {
      label: 'Facebook',
      icon: 'bi-facebook',
      url: publicSiteSettings.FacebookUrl,
      copy: 'Follow the latest arrivals, styling highlights, and store updates.',
    },
    {
      label: 'Instagram',
      icon: 'bi-instagram',
      url: publicSiteSettings.InstagramUrl,
      copy: 'See how Rich House pieces come together through a sharper visual edit.',
    },
  ] as const;

  loading = true;
  errorActionLabel = 'Try again';
  errorKind: ApiErrorKind | 'empty' | null = null;
  errorTitle = '';
  errorMessage = '';
  categories: CategoryItem[] = [];
  editorialProduct: ProductCard | null = null;
  newArrivals: ProductCard[] = [];
  bestSellers: ProductCard[] = [];
  offers: ProductCard[] = [];
  featuredProducts: ProductCard[] = [];

  constructor() {
    this.loadHomepage();
  }

  get featuredCategories(): CategoryItem[] {
    return this.categories.slice(0, 4);
  }

  get editorialImageUrl(): string {
    return resolveImageUrl(this.editorialProduct?.imageUrls[0]);
  }

  get hasBestSellers(): boolean {
    return this.bestSellers.length > 0;
  }

  get hasOffers(): boolean {
    return this.offers.length > 0;
  }

  resolveCategoryImage(path: string | null): string {
    return resolveCatalogThumbnailUrl(path);
  }

  retry(): void {
    if (this.errorKind === 'unauthorized') {
      void this.router.navigate(['/login']);
      return;
    }

    this.loadHomepage();
  }

  trackById(_index: number, item: { id: number }): number {
    return item.id;
  }

  private loadHomepage(): void {
    this.loading = true;
    this.errorActionLabel = 'Try again';
    this.errorKind = null;
    this.errorTitle = '';
    this.errorMessage = '';

    forkJoin({
      categories: this.productsService.getCategories(),
      featuredProducts: this.productsService
        .getCatalog({ page: 1, pageSize: 8, sort: 'featured', featured: true })
        .pipe(map((response) => response.items)),
      newArrivals: this.productsService
        .getCatalog({ page: 1, pageSize: 4, sort: 'newest', newArrival: true })
        .pipe(map((response) => response.items)),
      bestSellers: this.productsService
        .getCatalog({ page: 1, pageSize: 4, sort: 'bestSelling', bestSeller: true })
        .pipe(map((response) => response.items)),
      offers: this.productsService
        .getOfferProducts({ page: 1, pageSize: 4, sort: 'priceLowToHigh' })
        .pipe(map((response) => response.items)),
      fallbackProducts: this.productsService
        .getCatalog({ page: 1, pageSize: 6, sort: 'featured' })
        .pipe(map((response) => response.items)),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          if (!this.hasHomepageContent(data)) {
            this.loading = false;
            this.errorKind = 'empty';
            this.errorTitle = 'Catalog coming soon';
            this.errorMessage =
              'The homepage is connected, but there are no categories or products available to display yet.';
            this.errorActionLabel = 'Refresh';
            this.cdr.markForCheck();
            return;
          }

          this.categories = data.categories;
          this.featuredProducts =
            data.featuredProducts.length > 0 ? data.featuredProducts : data.fallbackProducts;
          this.newArrivals = data.newArrivals;
          this.bestSellers = data.bestSellers;
          this.offers = data.offers;
          const leadProduct =
            data.featuredProducts[0] ??
            data.bestSellers[0] ??
            data.newArrivals[0] ??
            data.offers[0] ??
            data.fallbackProducts[0] ??
            null;
          this.editorialProduct =
            data.featuredProducts[1] ?? data.fallbackProducts[1] ?? leadProduct;
          this.loading = false;
          this.seoService.applyRouteSeo({
            title: richHouseBrand.browserTitle,
            description: 'Premium menswear, suits, shirts and formalwear from Rich House.',
            type: 'website',
          });
          this.cdr.markForCheck();
        },
        error: (error) => {
          const apiError = describeApiError(error, {
            unavailable: `The Rich House backend is not running on ${apiConfig.baseUrl} right now. Start it, then retry the homepage.`,
            unauthorized:
              'Your session is no longer authorized for this request. Please sign in again.',
            server:
              'The Rich House backend returned an unexpected error while loading the homepage.',
            unknown:
              'We could not load the Rich House storefront just now. Please try again in a moment.',
          });

          this.loading = false;
          this.errorKind = apiError.kind;
          this.errorTitle = apiError.title;
          this.errorMessage = apiError.message;
          this.errorActionLabel = apiError.kind === 'unauthorized' ? 'Go to login' : 'Try again';
          this.cdr.markForCheck();
        },
      });
  }

  private hasHomepageContent(data: {
    bestSellers: ProductCard[];
    categories: CategoryItem[];
    fallbackProducts: ProductCard[];
    featuredProducts: ProductCard[];
    newArrivals: ProductCard[];
    offers: ProductCard[];
  }): boolean {
    return (
      data.categories.length > 0 ||
      data.featuredProducts.length > 0 ||
      data.newArrivals.length > 0 ||
      data.bestSellers.length > 0 ||
      data.offers.length > 0 ||
      data.fallbackProducts.length > 0
    );
  }
}
