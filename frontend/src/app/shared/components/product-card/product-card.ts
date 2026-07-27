import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { resolveCatalogThumbnailUrl, productPlaceholderImage } from '../../../core/utils/image-url';
import { ProductCard as CatalogProductCard } from '../../../models/product.models';
import { ImageFallbackDirective } from '../../directives/image-fallback.directive';
import { EgpPricePipe } from '../../pipes/egp-price.pipe';

@Component({
  selector: 'app-product-card',
  imports: [CommonModule, RouterLink, EgpPricePipe, ImageFallbackDirective],
  templateUrl: './product-card.html',
  styleUrl: './product-card.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductCardComponent {
  @Input({ required: true }) product!: CatalogProductCard;
  @Input() priority = false;

  readonly fallbackImage = productPlaceholderImage;

  get productRoute(): (string | number)[] {
    if (this.product.slug) {
      return ['/product', this.product.slug];
    }

    return ['/details', this.product.id];
  }

  get primaryImage(): string {
    return resolveCatalogThumbnailUrl(this.product.imageUrls[0]);
  }

  get secondaryImage(): string | null {
    return this.product.imageUrls.length > 1 ? resolveCatalogThumbnailUrl(this.product.imageUrls[1]) : null;
  }

  get hasCompareAtPrice(): boolean {
    return !!this.product.compareAtPrice && this.product.compareAtPrice > this.product.price;
  }

  get discountPercent(): number | null {
    if (!this.hasCompareAtPrice || !this.product.compareAtPrice) {
      return null;
    }

    return Math.round(((this.product.compareAtPrice - this.product.price) / this.product.compareAtPrice) * 100);
  }

  get categoryLabel(): string {
    return this.product.categoryName || 'Rich House';
  }

  get altText(): string {
    return `${this.product.name} by Rich House`;
  }
}
