import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { ProductsService } from '../../../core/services/product';
import {
  buildWhatsAppUrl,
  footerInformationLinks,
  primaryNavigationLinks,
  publicSiteSettings,
  richHouseBrand,
  richHouseStoreLocations,
} from '../../../core/config/site-settings.config';
import { CategoryItem } from '../../models/category.models';

@Component({
  selector: 'app-footer',
  imports: [CommonModule, RouterLink],
  templateUrl: './footer.html',
  styleUrl: './footer.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Footer {
  private readonly destroyRef = inject(DestroyRef);
  private readonly productService = inject(ProductsService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly brand = richHouseBrand;
  readonly navigationLinks = primaryNavigationLinks;
  readonly informationLinks = footerInformationLinks;
  readonly currentYear = new Date().getFullYear();
  readonly publicSiteSettings = publicSiteSettings;
  readonly whatsAppUrl = buildWhatsAppUrl(publicSiteSettings.WhatsAppNumber);
  readonly storeLocations = richHouseStoreLocations;

  categories: CategoryItem[] = [];

  constructor() {
    this.productService
      .getCategories()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (categories) => {
          this.categories = categories.slice(0, 4);
          this.cdr.markForCheck();
        },
        error: () => {
          this.categories = [];
          this.cdr.markForCheck();
        },
      });
  }

  trackByCategoryId(_index: number, category: CategoryItem): number {
    return category.id;
  }
}
