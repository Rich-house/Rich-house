import { CommonModule } from '@angular/common';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  ElementRef,
  ViewChild,
  inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { Observable, forkJoin, of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import { ProductsService } from '../../../core/services/product';
import { apiConfig } from '../../../core/config/api.config';
import { SeoService } from '../../../core/services/seo.service';
import {
  buildWhatsAppUrl,
  publicSiteSettings,
  richHouseBrand,
} from '../../../core/config/site-settings.config';
import {
  productPlaceholderImage,
  resolveCatalogThumbnailUrl,
  resolveCatalogResponsiveThumbnailUrl,
  resolveImageUrl,
} from '../../../core/utils/image-url';
import { describeApiError, type ApiErrorKind } from '../../../core/utils/http-error';
import { CategoryItem } from '../../../shared/models/category.models';
import { ProductCard } from '../../../shared/models/product.models';
import { ImageFallbackDirective } from '../../../shared/directives/image-fallback.directive';
import { ProductCardComponent } from '../../../shared/components/product-card/product-card';

type HeroMediaState = 'loading' | 'playing' | 'error';
type HomepageSectionState = 'loading' | 'ready' | 'empty' | 'error';

interface HomepageSectionResult<TData> {
  data: TData;
  error: ApiErrorKind | null;
  requestUrl: string | null;
  state: HomepageSectionState;
  status: number | null;
}

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

  @ViewChild('heroVideo') private heroVideoRef?: ElementRef<HTMLVideoElement>;

  private readonly heroAssetRevision = '20260729';
  readonly brand = richHouseBrand;
  readonly fallbackImage = productPlaceholderImage;
  readonly heroPosterUrl =
    `/assets/hero/rich-house-suit-hero-poster.webp?v=${this.heroAssetRevision}`;
  readonly heroMobilePosterUrl =
    `/assets/hero/rich-house-suit-hero-mobile.webp?v=${this.heroAssetRevision}`;
  readonly heroMobilePosterSrcset =
    '/assets/hero/rich-house-suit-hero-mobile-820.webp?v=20261004 820w, ' +
    `${this.heroMobilePosterUrl} 1080w`;
  readonly heroVideoMp4Url =
    `/assets/hero/rich-house-suit-hero-optimized.mp4?v=${this.heroAssetRevision}`;
  readonly publicSiteSettings = publicSiteSettings;
  readonly whatsAppUrl = buildWhatsAppUrl(publicSiteSettings.WhatsAppNumber);
  readonly benefits = [
    {
      icon: 'bi-gem',
      title: 'Premium Tailoring',
      copy: 'Considered suiting and occasion pieces selected for a polished, composed finish.',
    },
    {
      icon: 'bi-person-check',
      title: 'Perfect Fit',
      copy: 'Clear size choices and product details help you find the fit that feels right.',
    },
    {
      icon: 'bi-chat-dots',
      title: 'Personal Assistance',
      copy: 'Message the Rich House team for guidance with sizing, availability, or choosing a look.',
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
  heroMediaState: HeroMediaState = 'loading';
  heroVideoEnabled = false;
  categoriesState: HomepageSectionState = 'loading';
  newArrivalsState: HomepageSectionState = 'loading';
  bestSellersState: HomepageSectionState = 'loading';
  offersState: HomepageSectionState = 'loading';
  featuredState: HomepageSectionState = 'loading';

  private heroVideoElement?: HTMLVideoElement;
  private heroPlaybackInFlight = false;
  private heroVideoTimer: number | null = null;

  constructor() {
    this.loadHomepage();
    this.destroyRef.onDestroy(() => {
      if (this.heroVideoTimer !== null) {
        window.clearTimeout(this.heroVideoTimer);
      }
    });
  }

  ngAfterViewInit(): void {
    if (!this.shouldLoadHeroVideo()) {
      this.heroMediaState = 'playing';
      return;
    }

    this.heroVideoTimer = window.setTimeout(() => {
      this.heroVideoEnabled = true;
      this.cdr.detectChanges();
      window.setTimeout(() => void this.ensureHeroVideoPlayback());
    }, 1400);
  }

  get featuredCategories(): CategoryItem[] {
    return this.categories;
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

  get hasRenderableHomepageContent(): boolean {
    return this.hasHomepageContent({
      bestSellers: this.bestSellers,
      categories: this.categories,
      fallbackProducts: [],
      featuredProducts: this.featuredProducts,
      newArrivals: this.newArrivals,
      offers: this.offers,
    });
  }

  resolveCategoryImage(path: string | null): string {
    return resolveCatalogThumbnailUrl(path);
  }

  categoryImageSrcset(path: string | null): string {
    return `${resolveCatalogResponsiveThumbnailUrl(path)} 384w, ${this.resolveCategoryImage(path)} 512w`;
  }

  categoryImageFallbacks(path: string | null): readonly string[] {
    return [resolveImageUrl(path), this.fallbackImage];
  }

  retry(): void {
    if (this.errorKind === 'unauthorized') {
      void this.router.navigate(['/admin/login']);
      return;
    }

    this.loadHomepage();
  }

  onHeroVideoLoaded(): void {
    void this.ensureHeroVideoPlayback();
  }

  onHeroVideoCanPlay(): void {
    void this.ensureHeroVideoPlayback();
  }

  onHeroVideoPlaying(): void {
    this.heroPlaybackInFlight = false;
    this.heroMediaState = 'playing';
    this.cdr.markForCheck();
  }

  onHeroVideoError(event?: Event): void {
    this.heroPlaybackInFlight = false;
    this.heroMediaState = 'error';
    this.logHeroMediaDiagnostic('Real hero media error.', event);
    this.cdr.markForCheck();
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
    this.categoriesState = 'loading';
    this.newArrivalsState = 'loading';
    this.bestSellersState = 'loading';
    this.offersState = 'loading';
    this.featuredState = 'loading';

    const emptyFallbackResult: HomepageSectionResult<ProductCard[]> = {
      data: [],
      error: null,
      requestUrl: null,
      state: 'empty',
      status: null,
    };
    const featuredBundle$ = this.wrapHomepageSection(
      'featuredProducts',
      this.productsService
        .getCatalog({ page: 1, pageSize: 8, sort: 'featured', featured: true })
        .pipe(map((response) => response.items)),
      [],
      (items) => items.length > 0,
    ).pipe(
      switchMap((featuredProducts) => {
        if (featuredProducts.data.length > 0) {
          return of({ featuredProducts, fallbackProducts: emptyFallbackResult });
        }

        return this.wrapHomepageSection(
          'fallbackProducts',
          this.productsService
            .getCatalog({ page: 1, pageSize: 6, sort: 'featured' })
            .pipe(map((response) => response.items)),
          [],
          (items) => items.length > 0,
        ).pipe(map((fallbackProducts) => ({ featuredProducts, fallbackProducts })));
      }),
    );

    forkJoin({
      categories: this.wrapHomepageSection(
        'categories',
        this.productsService.getCategories(),
        [],
        (items) => items.length > 0,
      ),
      featuredBundle: featuredBundle$,
      newArrivals: this.wrapHomepageSection(
        'newArrivals',
        this.productsService
          .getCatalog({ page: 1, pageSize: 4, sort: 'newest', newArrival: true })
          .pipe(map((response) => response.items)),
        [],
        (items) => items.length > 0,
      ),
      bestSellers: this.wrapHomepageSection(
        'bestSellers',
        this.productsService
          .getCatalog({ page: 1, pageSize: 4, sort: 'bestSelling', bestSeller: true })
          .pipe(map((response) => response.items)),
        [],
        (items) => items.length > 0,
      ),
      offers: this.wrapHomepageSection(
        'offers',
        this.productsService
          .getOfferProducts({ page: 1, pageSize: 4, sort: 'priceLowToHigh' })
          .pipe(map((response) => response.items)),
        [],
        (items) => items.length > 0,
      ),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (sections) => {
          const { featuredProducts, fallbackProducts } = sections.featuredBundle;
          this.categories = sections.categories.data;
          this.categoriesState = sections.categories.state;
          this.newArrivals = sections.newArrivals.data;
          this.newArrivalsState = sections.newArrivals.state;
          this.bestSellers = sections.bestSellers.data;
          this.bestSellersState = sections.bestSellers.state;
          this.offers = sections.offers.data;
          this.offersState = sections.offers.state;

          this.featuredProducts =
            featuredProducts.data.length > 0
              ? featuredProducts.data
              : fallbackProducts.data;
          this.featuredState = this.resolveFeaturedState(
            featuredProducts,
            fallbackProducts,
          );

          const hasRenderableContent = this.hasHomepageContent({
            bestSellers: this.bestSellers,
            categories: this.categories,
            fallbackProducts: fallbackProducts.data,
            featuredProducts: this.featuredProducts,
            newArrivals: this.newArrivals,
            offers: this.offers,
          });

          if (!hasRenderableContent) {
            const primaryError = this.resolveHomepageError({
              ...sections,
              featuredProducts,
              fallbackProducts,
            });
            this.loading = false;
            if (primaryError) {
              this.errorKind = primaryError.kind;
              this.errorTitle = primaryError.title;
              this.errorMessage = primaryError.message;
              this.errorActionLabel =
                primaryError.kind === 'unauthorized' ? 'Go to login' : 'Try again';
            } else {
              this.errorKind = 'empty';
              this.errorTitle = 'Catalog coming soon';
              this.errorMessage =
                'The homepage is connected, but there are no categories or products available to display yet.';
              this.errorActionLabel = 'Refresh';
            }
            this.cdr.markForCheck();
            return;
          }

          const leadProduct =
            this.featuredProducts[0] ??
            this.bestSellers[0] ??
            this.newArrivals[0] ??
            this.offers[0] ??
            fallbackProducts.data[0] ??
            null;
          this.editorialProduct =
            [...this.featuredProducts, ...this.newArrivals, ...fallbackProducts.data].find(
              (product) => product.slug === 'ivory-tuxedo-with-double-stripe-lapels',
            ) ?? this.featuredProducts[1] ?? fallbackProducts.data[1] ?? leadProduct;
          this.loading = false;
          this.seoService.setPageSeo({
            title: richHouseBrand.homeTitle,
            description: 'Premium menswear, suits, shirts and formalwear from Rich House.',
            keywords: richHouseBrand.defaultKeywords,
            breadcrumbs: [{ name: 'Home', path: '/' }],
            image: leadProduct?.imageUrls[0] ?? null,
            type: 'website',
            url: this.router.url,
          });
          this.cdr.markForCheck();
        },
      });
  }

  private async ensureHeroVideoPlayback(): Promise<void> {
    const videoElement = this.heroVideoRef?.nativeElement;
    if (!videoElement) {
      return;
    }

    this.heroVideoElement = videoElement;
    videoElement.muted = true;
    videoElement.defaultMuted = true;
    videoElement.playsInline = true;
    videoElement.loop = true;
    videoElement.autoplay = true;
    videoElement.preload = 'metadata';
    videoElement.playbackRate = 1;
    if (videoElement.getAttribute('src') !== this.heroVideoMp4Url) {
      videoElement.setAttribute('src', this.heroVideoMp4Url);
    }
    videoElement.setAttribute('muted', '');
    videoElement.setAttribute('playsinline', '');
    if (!videoElement.currentSrc) {
      videoElement.load();
    }

    if (this.heroPlaybackInFlight) {
      return;
    }

    this.heroPlaybackInFlight = true;
    try {
      if (this.heroMediaState !== 'playing') {
        this.heroMediaState = 'loading';
        this.cdr.markForCheck();
      }
      await videoElement.play();
    } catch (error) {
      this.heroMediaState = 'error';
      console.error('Real hero playback failure:', error);
      this.logHeroMediaDiagnostic('Real hero playback failure.', error);
      this.cdr.markForCheck();
    } finally {
      this.heroPlaybackInFlight = false;
    }
  }

  private shouldLoadHeroVideo(): boolean {
    if (typeof window === 'undefined' || typeof navigator === 'undefined') {
      return false;
    }

    const connection = (navigator as Navigator & { connection?: { saveData?: boolean } }).connection;
    return (
      window.matchMedia('(min-width: 48.01rem)').matches
      && !window.matchMedia('(prefers-reduced-motion: reduce)').matches
      && !connection?.saveData
    );
  }

  private wrapHomepageSection<TData>(
    section: string,
    request$: Observable<TData>,
    fallback: TData,
    hasContent: (data: TData) => boolean,
  ): Observable<HomepageSectionResult<TData>> {
    return request$.pipe(
      map(
        (data): HomepageSectionResult<TData> => ({
          data,
          error: null,
          requestUrl: null,
          state: hasContent(data) ? 'ready' : 'empty',
          status: 200,
        }),
      ),
      catchError((error) => {
        const apiError = describeApiError(error, {
          unavailable: 'The Rich House service is temporarily unreachable. Please wait a moment and retry the homepage.',
          unauthorized:
            'Your session is no longer authorized for this request. Please sign in again.',
          server:
            'The Rich House backend returned an unexpected error while loading the homepage.',
          unknown:
            'We could not load the Rich House storefront just now. Please try again in a moment.',
        });

        this.logHomepageDiagnostic(section, error);

        return of({
          data: fallback,
          error: apiError.kind,
          requestUrl: this.extractRequestUrl(error),
          state: 'error' as const,
          status: apiError.status,
        });
      }),
    );
  }

  private resolveFeaturedState(
    featuredProducts: HomepageSectionResult<ProductCard[]>,
    fallbackProducts: HomepageSectionResult<ProductCard[]>,
  ): HomepageSectionState {
    if (featuredProducts.data.length > 0 || fallbackProducts.data.length > 0) {
      return 'ready';
    }

    if (featuredProducts.state === 'error' && fallbackProducts.state === 'error') {
      return 'error';
    }

    return 'empty';
  }

  private resolveHomepageError(sections: {
    bestSellers: HomepageSectionResult<ProductCard[]>;
    categories: HomepageSectionResult<CategoryItem[]>;
    fallbackProducts: HomepageSectionResult<ProductCard[]>;
    featuredProducts: HomepageSectionResult<ProductCard[]>;
    newArrivals: HomepageSectionResult<ProductCard[]>;
    offers: HomepageSectionResult<ProductCard[]>;
  }): { kind: ApiErrorKind; message: string; title: string } | null {
    const firstFailedSection = [
      sections.categories,
      sections.featuredProducts,
      sections.fallbackProducts,
      sections.newArrivals,
      sections.bestSellers,
      sections.offers,
    ].find((section) => section.error);

    if (!firstFailedSection?.error) {
      return null;
    }

    if (firstFailedSection.error === 'unauthorized') {
      return {
        kind: 'unauthorized',
        title: 'Sign in required',
        message: 'Your session is no longer authorized for this request. Please sign in again.',
      };
    }

    if (firstFailedSection.error === 'unavailable') {
      return {
        kind: 'unavailable',
        title: 'Service temporarily unavailable',
        message: 'The Rich House service is temporarily unreachable. Please wait a moment and retry the homepage.',
      };
    }

    if (firstFailedSection.error === 'server') {
      return {
        kind: 'server',
        title: 'Server error',
        message: 'The Rich House backend returned an unexpected error while loading the homepage.',
      };
    }

    return {
      kind: 'unknown',
      title: 'Request failed',
      message: 'We could not load the Rich House storefront just now. Please try again in a moment.',
    };
  }

  private extractRequestUrl(error: unknown): string | null {
    if (typeof error === 'object' && error && 'url' in error && typeof error.url === 'string') {
      return error.url;
    }

    return null;
  }

  private logHomepageDiagnostic(section: string, error: unknown): void {
    if (!this.isLocalDebugEnvironment()) {
      return;
    }

    const diagnostic = describeApiError(error);
    console.warn(`[Rich House] Homepage section failed: ${section}`, {
      kind: diagnostic.kind,
      status: diagnostic.status,
      url: this.extractRequestUrl(error),
    });
  }

  private logHeroMediaDiagnostic(message: string, error?: unknown): void {
    if (!this.isLocalDebugEnvironment()) {
      return;
    }

    const mediaError = this.heroVideoElement?.error;
    console.warn(`[Rich House] Hero video: ${message}`, {
      browserError: error,
      mediaErrorCode: mediaError?.code ?? null,
      mediaErrorMessage: mediaError?.message ?? null,
      state: this.heroMediaState,
    });
  }

  private isLocalDebugEnvironment(): boolean {
    if (typeof window === 'undefined') {
      return false;
    }

    return ['127.0.0.1', 'localhost'].includes(window.location.hostname);
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
