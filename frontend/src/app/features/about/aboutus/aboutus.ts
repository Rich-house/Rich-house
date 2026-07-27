import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { map } from 'rxjs/operators';
import { ProductsService } from '../../../core/services/product';
import { SeoService } from '../../../core/services/seo.service';
import { richHouseBrand, richHouseStoreLocations } from '../../../core/config/site-settings.config';
import {
  productPlaceholderImage,
  resolveCatalogThumbnailUrl,
  resolveImageUrl,
} from '../../../core/utils/image-url';
import { CategoryItem } from '../../../shared/models/category.models';
import { ProductCard } from '../../../shared/models/product.models';
import { ImageFallbackDirective } from '../../../shared/directives/image-fallback.directive';

@Component({
  selector: 'app-aboutus',
  imports: [CommonModule, RouterLink, ImageFallbackDirective],
  templateUrl: './aboutus.html',
  styleUrl: './aboutus.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Aboutus {
  private readonly destroyRef = inject(DestroyRef);
  private readonly productsService = inject(ProductsService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly seoService = inject(SeoService);

  readonly brand = richHouseBrand;
  readonly fallbackImage = productPlaceholderImage;
  readonly storeLocations = richHouseStoreLocations;
  readonly principles = [
    'Timeless menswear with a modern point of view.',
    'Clearer shopping journeys built around fit, occasion, and confidence.',
    'Product presentation shaped for polish rather than marketplace noise.',
  ] as const;

  readonly storySections = [
    {
      title: 'Timeless, not theatrical',
      copy: 'Rich House focuses on menswear that feels composed across work, ceremony, and elevated everyday dressing. The emphasis is on balance, material presence, and confidence rather than excess.',
    },
    {
      title: 'Built for real wardrobes',
      copy: 'The catalog is organized to help customers discover tailoring, layering, and refined essentials without the clutter of a broad generic marketplace.',
    },
    {
      title: 'Ready to evolve',
      copy: 'This brand story is intentionally structured so the Rich House team can refine it later from site settings or the admin experience without reworking the page layout.',
    },
  ] as const;

  heroProduct: ProductCard | null = null;
  featuredCategories: CategoryItem[] = [];

  constructor() {
    forkJoin({
      heroProduct: this.productsService
        .getCatalog({ page: 1, pageSize: 1, sort: 'featured' })
        .pipe(map((response) => response.items[0] ?? null)),
      categories: this.productsService.getCategories(),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.heroProduct = data.heroProduct;
          this.featuredCategories = data.categories.slice(0, 3);
          this.seoService.applyRouteSeo({
            title: richHouseBrand.browserTitle,
            description:
              'Learn about the Rich House menswear brand, its visual direction, and its approach to modern formalwear.',
          });
          this.cdr.markForCheck();
        },
      });
  }

  get heroImageUrl(): string {
    return resolveImageUrl(this.heroProduct?.imageUrls[0]);
  }

  resolveCategoryImage(path: string | null): string {
    return resolveCatalogThumbnailUrl(path);
  }

  trackById(_index: number, item: { id: number }): number {
    return item.id;
  }
}
