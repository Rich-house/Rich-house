import { CommonModule } from '@angular/common';
import { Component, DestroyRef, computed, inject } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRouteSnapshot, NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter, map, startWith } from 'rxjs';
import { Footer } from './shared/components/footer/footer';
import { Header } from './shared/components/header/header';
import { RouteSeoConfig, SeoService } from './core/services/seo.service';
import { WhatsAppButtonComponent } from './shared/components/whatsapp-button/whatsapp-button';
import { ProductsService } from './core/services/product';
import {
  resolveCatalogResponsiveThumbnailUrl,
  resolveCatalogThumbnailUrl,
  resolveOptimizedProductImageUrl,
  resolveResponsiveProductImageUrl,
} from './core/utils/image-url';

@Component({
  selector: 'app-root',
  imports: [CommonModule, RouterOutlet, Header, Footer, WhatsAppButtonComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  private readonly seoService = inject(SeoService);
  private readonly productsService = inject(ProductsService);

  private readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map(() => this.router.url),
      startWith(this.router.url),
    ),
    { initialValue: this.router.url },
  );

  readonly showCustomerChrome = computed(
    () => !/^\/(?:admin|dashboard|login|register|confirmemail)(?:\/|$)/.test(this.currentUrl()),
  );
  readonly showFloatingWhatsApp = computed(
    () =>
      this.showCustomerChrome()
      && !/^\/(?:product|details|cart|create-order)(?:\/|$)/.test(this.currentUrl()),
  );

  constructor() {
    this.warmInitialStorefrontData();

    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        startWith(null),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => {
        this.applyRouteSeo();
      });
  }

  private warmInitialStorefrontData(): void {
    if (typeof window === 'undefined') {
      return;
    }

    const path = window.location.pathname.replace(/\/+$/, '') || '/';
    if (path === '/shop' && !window.location.search) {
      this.productsService
        .getCatalog({ page: 1, pageSize: 12, sort: 'featured' })
        .subscribe({
          next: (response) => this.preloadCatalogImage(response.items[0]?.imageUrls[0]),
          error: () => undefined,
        });
      return;
    }

    if (path === '/offers' && !window.location.search) {
      this.productsService
        .getOfferProducts({ page: 1, pageSize: 12, onOffer: true, sort: 'featured' })
        .subscribe({
          next: (response) => this.preloadCatalogImage(response.items[0]?.imageUrls[0]),
          error: () => undefined,
        });
      return;
    }

    const productSlug = path.match(/^\/product\/([^/]+)$/)?.[1];
    if (productSlug) {
      this.productsService
        .getProductBySlug(decodeURIComponent(productSlug))
        .subscribe({
          next: (product) => this.preloadProductImage(product.imageUrls[0]),
          error: () => undefined,
        });
    }
  }

  private preloadCatalogImage(imageUrl?: string): void {
    if (!imageUrl) {
      return;
    }

    this.appendImagePreload(
      resolveCatalogResponsiveThumbnailUrl(imageUrl),
      `${resolveCatalogResponsiveThumbnailUrl(imageUrl)} 384w, ${resolveCatalogThumbnailUrl(imageUrl)} 512w`,
      '(max-width: 48rem) calc(50vw - 1.5rem), (max-width: 80rem) calc(33.333vw - 2rem), 20rem',
    );
  }

  private preloadProductImage(imageUrl?: string): void {
    if (!imageUrl) {
      return;
    }

    this.appendImagePreload(
      resolveResponsiveProductImageUrl(imageUrl),
      `${resolveResponsiveProductImageUrl(imageUrl)} 720w, ${resolveOptimizedProductImageUrl(imageUrl)} 1024w`,
      '(max-width: 48rem) calc(100vw - 2rem), min(52vw, 42rem)',
    );
  }

  private appendImagePreload(href: string, srcset: string, sizes: string): void {
    if (document.head.querySelector(`link[rel="preload"][href="${href}"]`)) {
      return;
    }

    const preload = document.createElement('link');
    preload.rel = 'preload';
    preload.as = 'image';
    preload.href = href;
    preload.imageSrcset = srcset;
    preload.imageSizes = sizes;
    preload.fetchPriority = 'high';
    document.head.appendChild(preload);
  }

  private applyRouteSeo(): void {
    const activeRoute = this.getDeepestRoute(this.router.routerState.snapshot.root);
    const seoConfig = (activeRoute.data['seo'] as RouteSeoConfig | undefined) ?? {};

    this.seoService.applyRouteSeo(
      {
        ...seoConfig,
        title: activeRoute.title || seoConfig.title,
      },
      this.router.url,
    );
  }

  private getDeepestRoute(route: ActivatedRouteSnapshot): ActivatedRouteSnapshot {
    let currentRoute = route;

    while (currentRoute.firstChild) {
      currentRoute = currentRoute.firstChild;
    }

    return currentRoute;
  }
}
