import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  HostListener,
  inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, NavigationEnd, Router, RouterModule } from '@angular/router';
import { combineLatest, filter } from 'rxjs';
import { ProductsService } from '../../../core/services/product';
import {
  buildCategoryTitle,
  richHouseBrand,
} from '../../../core/config/site-settings.config';
import { SeoService } from '../../../core/services/seo.service';
import { CategoryItem } from '../../../shared/models/category.models';
import { CatalogQuery, PaginationMeta, ProductCard } from '../../../shared/models/product.models';
import { ProductCardComponent } from '../../../shared/components/product-card/product-card';

type CatalogMode = 'shop' | 'offers';

@Component({
  selector: 'app-product',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, ProductCardComponent],
  templateUrl: './product.html',
  styleUrl: './product.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Product {
  private readonly destroyRef = inject(DestroyRef);
  private readonly productService = inject(ProductsService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly seoService = inject(SeoService);

  readonly sortOptions = [
    { value: 'featured', label: 'Featured first' },
    { value: 'newest', label: 'Newest' },
    { value: 'priceLowToHigh', label: 'Price: low to high' },
    { value: 'priceHighToLow', label: 'Price: high to low' },
    { value: 'bestSelling', label: 'Best selling' },
  ] as const;

  readonly sizeOptions = [
    '46',
    '48',
    '50',
    '52',
    '54',
    '56',
    '58',
    '60',
    'S',
    'M',
    'L',
    'XL',
    'XXL',
  ];

  mode: CatalogMode = 'shop';
  categories: CategoryItem[] = [];
  products: ProductCard[] = [];
  loading = true;
  errorMessage = '';
  mobileFiltersOpen = false;

  filters = {
    search: '',
    sort: 'featured',
    size: '',
    minPrice: '',
    maxPrice: '',
    inStock: false,
    onOffer: false,
    page: 1,
  };

  meta: PaginationMeta = {
    page: 1,
    pageSize: 12,
    totalItems: 0,
    totalPages: 0,
    hasPreviousPage: false,
    hasNextPage: false,
  };

  categorySlug: string | null = null;

  constructor() {
    this.productService
      .getCategories()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((categories) => {
        this.categories = categories;
        this.updateCatalogSeo();
        this.cdr.markForCheck();
      });

    combineLatest([this.route.data, this.route.paramMap, this.route.queryParamMap])
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(([data, params, queryParams]) => {
        this.mode = data['offersOnly'] ? 'offers' : 'shop';
        this.categorySlug = this.mode === 'shop' ? params.get('categorySlug') : null;
        this.filters = {
          search: queryParams.get('search') ?? '',
          sort: queryParams.get('sort') ?? 'featured',
          size: queryParams.get('size') ?? '',
          minPrice: queryParams.get('minPrice') ?? '',
          maxPrice: queryParams.get('maxPrice') ?? '',
          inStock: queryParams.get('inStock') === 'true',
          onOffer: this.mode === 'offers' || queryParams.get('onOffer') === 'true',
          page: Number(queryParams.get('page') ?? '1') || 1,
        };
        this.loadCatalog();
        this.cdr.markForCheck();
      });

    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => {
        this.mobileFiltersOpen = false;
      });
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.mobileFiltersOpen = false;
  }

  get heading(): string {
    if (this.mode === 'offers') {
      return 'Current Offers';
    }

    return this.selectedCategory?.name ?? 'Shop';
  }

  get subheading(): string {
    if (this.mode === 'offers') {
      return 'Discover only the currently active Rich House offers, without expired promotions or placeholder discounts.';
    }

    if (this.selectedCategory) {
      return `Browse the live ${this.selectedCategory.name} selection from the Rich House catalog.`;
    }

    return 'Explore tailoring, layering, and elevated essentials through the live Rich House catalog.';
  }

  get selectedCategory(): CategoryItem | null {
    if (!this.categorySlug) {
      return null;
    }

    return this.categories.find((category) => category.slug === this.categorySlug) ?? null;
  }

  get activeChips(): string[] {
    const chips: string[] = [];

    if (this.categorySlug && this.selectedCategory) {
      chips.push(this.selectedCategory.name);
    }

    if (this.filters.search) {
      chips.push(`Search: ${this.filters.search}`);
    }

    if (this.filters.size) {
      chips.push(`Size: ${this.filters.size}`);
    }

    if (this.filters.minPrice || this.filters.maxPrice) {
      const min = this.filters.minPrice || '0';
      const max = this.filters.maxPrice || 'any';
      chips.push(`Price: EGP ${min} - ${max === 'any' ? 'Any' : max}`);
    }

    if (this.filters.inStock) {
      chips.push('In stock');
    }

    if (this.filters.onOffer && this.mode !== 'offers') {
      chips.push('Offers only');
    }

    return chips;
  }

  get categoryLinks(): CategoryItem[] {
    return this.categories.slice(0, 8);
  }

  get showPagination(): boolean {
    return this.meta.totalPages > 1;
  }

  get pageNumbers(): number[] {
    const total = this.meta.totalPages;
    const current = this.meta.page;
    const start = Math.max(1, current - 1);
    const end = Math.min(total, current + 1);
    const pages: number[] = [];

    for (let page = start; page <= end; page += 1) {
      pages.push(page);
    }

    return pages;
  }

  toggleMobileFilters(): void {
    this.mobileFiltersOpen = !this.mobileFiltersOpen;
    this.cdr.markForCheck();
  }

  applyFilters(resetPage = true): void {
    this.navigateWithFilters(resetPage ? 1 : this.filters.page);
  }

  changeCategory(categorySlug: string | null): void {
    const target = categorySlug ? ['/shop', categorySlug] : ['/shop'];
    this.router.navigate(target, {
      queryParams: this.buildQueryParams(1),
    });
  }

  clearFilters(): void {
    this.filters = {
      search: '',
      sort: 'featured',
      size: '',
      minPrice: '',
      maxPrice: '',
      inStock: false,
      onOffer: this.mode === 'offers',
      page: 1,
    };

    if (this.mode === 'offers') {
      this.router.navigate(['/offers'], { queryParams: this.buildQueryParams(1) });
      return;
    }

    this.router.navigate(this.categorySlug ? ['/shop', this.categorySlug] : ['/shop'], {
      queryParams: this.buildQueryParams(1),
    });
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.meta.totalPages || page === this.meta.page) {
      return;
    }

    this.navigateWithFilters(page);
  }

  retry(): void {
    this.loadCatalog();
  }

  trackById(_index: number, item: { id: number }): number {
    return item.id;
  }

  private loadCatalog(): void {
    this.loading = true;
    this.errorMessage = '';

    const query: CatalogQuery = {
      page: this.filters.page,
      pageSize: 12,
      search: this.filters.search || undefined,
      categorySlug: this.mode === 'shop' ? (this.categorySlug ?? undefined) : undefined,
      size: this.filters.size || undefined,
      minPrice: this.filters.minPrice ? Number(this.filters.minPrice) : undefined,
      maxPrice: this.filters.maxPrice ? Number(this.filters.maxPrice) : undefined,
      inStock: this.filters.inStock || undefined,
      onOffer: this.filters.onOffer || undefined,
      sort: this.filters.sort,
    };

    const request =
      this.mode === 'offers'
        ? this.productService.getOfferProducts(query)
        : this.productService.getCatalog(query);

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response) => {
        this.products = response.items;
        this.meta = response.meta;
        this.loading = false;
        this.updateCatalogSeo();
        this.cdr.markForCheck();
      },
      error: () => {
        this.products = [];
        this.loading = false;
        this.errorMessage =
          'We could not load this part of the Rich House catalog. Please try again.';
        this.cdr.markForCheck();
      },
    });
  }

  private updateCatalogSeo(): void {
    if (this.mode === 'offers') {
      this.seoService.setPageSeo({
        title: `Current Offers | ${richHouseBrand.name}`,
        description:
          'Discover current Rich House offers across premium menswear, suits, shirts, and formalwear.',
        keywords: [
          richHouseBrand.name,
          'offers',
          'discounts',
          'menswear sale',
          'formalwear offers',
        ],
        breadcrumbs: [
          { name: 'Home', path: '/' },
          { name: 'Offers', path: '/offers' },
        ],
        image: this.products[0]?.imageUrls[0] ?? null,
        url: this.router.url,
      });
      return;
    }

    if (this.selectedCategory) {
      this.seoService.setPageSeo({
        title: buildCategoryTitle(this.selectedCategory.name),
        description: `Browse the Rich House ${this.selectedCategory.name} selection, curated for premium menswear and formalwear shopping.`,
        keywords: [
          this.selectedCategory.name,
          `${this.selectedCategory.name} ${richHouseBrand.name}`,
          richHouseBrand.name,
          'menswear',
          'formalwear',
          'shop Egypt',
        ],
        breadcrumbs: [
          { name: 'Home', path: '/' },
          { name: 'Shop', path: '/shop' },
          {
            name: this.selectedCategory.name,
            path: `/shop/${this.selectedCategory.slug ?? ''}`,
          },
        ],
        image: this.selectedCategory.imageUrl ?? this.products[0]?.imageUrls[0] ?? null,
        url: this.router.url,
      });
      return;
    }

    this.seoService.setPageSeo({
      title: this.filters.search
        ? `Search: ${this.filters.search.trim()} | ${richHouseBrand.name}`
        : `Shop | ${richHouseBrand.name}`,
      description:
        'Browse the Rich House menswear collection, including suits, shirts, elevated essentials, and formalwear.',
      keywords: this.filters.search
        ? [
            this.filters.search.trim(),
            richHouseBrand.name,
            'menswear search',
            'formalwear',
          ]
        : [
            richHouseBrand.name,
            'shop',
            'menswear',
            'formalwear',
            'suits',
            'shirts',
            'belts',
            'shoes',
          ],
      breadcrumbs: [
        { name: 'Home', path: '/' },
        { name: 'Shop', path: '/shop' },
      ],
      image: this.products[0]?.imageUrls[0] ?? null,
      url: this.router.url,
    });
  }

  private navigateWithFilters(page: number): void {
    const target =
      this.mode === 'offers'
        ? ['/offers']
        : this.categorySlug
          ? ['/shop', this.categorySlug]
          : ['/shop'];
    this.router.navigate(target, {
      queryParams: this.buildQueryParams(page),
    });
  }

  private buildQueryParams(page: number): Record<string, string | number | boolean> {
    const queryParams: Record<string, string | number | boolean> = {
      sort: this.filters.sort,
      page,
    };

    if (this.filters.search) {
      queryParams['search'] = this.filters.search;
    }

    if (this.filters.size) {
      queryParams['size'] = this.filters.size;
    }

    if (this.filters.minPrice) {
      queryParams['minPrice'] = this.filters.minPrice;
    }

    if (this.filters.maxPrice) {
      queryParams['maxPrice'] = this.filters.maxPrice;
    }

    if (this.filters.inStock) {
      queryParams['inStock'] = true;
    }

    if (this.filters.onOffer && this.mode !== 'offers') {
      queryParams['onOffer'] = true;
    }

    return queryParams;
  }
}
