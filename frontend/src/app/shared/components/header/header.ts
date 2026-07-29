import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  HostListener,
  inject,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter } from 'rxjs';
import { ProductsService } from '../../../core/services/product';
import {
  primaryNavigationLinks,
  richHouseBrand,
} from '../../../core/config/site-settings.config';
import { CategoryItem } from '../../models/category.models';

@Component({
  selector: 'app-header',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './header.html',
  styleUrl: './header.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Header {
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly brand = richHouseBrand;
  readonly navigationLinks = primaryNavigationLinks;
  readonly productService = inject(ProductsService);
  readonly announcementMessages = [
    'PREMIUM TAILORING FOR EVERY OCCASION',
    'NEW COLLECTION AVAILABLE',
    'BOOK YOUR FITTING THROUGH WHATSAPP',
  ] as const;

  categories: CategoryItem[] = [];
  cartCount = 0;
  searchText = '';
  mobileMenuOpen = false;
  categoriesMenuOpen = false;

  constructor() {
    this.productService.cartCount$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((count) => {
        this.cartCount = count;
        this.cdr.markForCheck();
      });

    this.productService
      .getCategories()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (categories) => {
          this.categories = categories;
          this.cdr.markForCheck();
        },
        error: () => {
          this.categories = [];
          this.cdr.markForCheck();
        },
      });

    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.closeAllMenus());
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closeAllMenus();
  }

  get hasCategories(): boolean {
    return this.categories.length > 0;
  }

  get desktopCategories(): CategoryItem[] {
    return this.categories.slice(0, 6);
  }

  get mobileCategories(): CategoryItem[] {
    return this.categories.slice(0, 8);
  }

  isRouteActive(route: string): boolean {
    const currentUrl = this.router.url;
    if (route === '/') {
      return currentUrl === '/' || currentUrl.startsWith('/home');
    }

    return currentUrl === route || currentUrl.startsWith(`${route}/`);
  }

  toggleMobileMenu(): void {
    this.mobileMenuOpen = !this.mobileMenuOpen;
    if (!this.mobileMenuOpen) {
      this.categoriesMenuOpen = false;
    }
  }

  closeMobileMenu(): void {
    this.mobileMenuOpen = false;
  }

  toggleCategoriesMenu(): void {
    this.categoriesMenuOpen = !this.categoriesMenuOpen;
  }

  submitSearch(): void {
    const query = this.searchText.trim();
    this.router.navigate(['/shop'], {
      queryParams: query ? { search: query } : {},
    });
  }

  trackByCategoryId(_index: number, category: CategoryItem): number {
    return category.id;
  }

  private closeAllMenus(): void {
    this.mobileMenuOpen = false;
    this.categoriesMenuOpen = false;
    this.cdr.markForCheck();
  }
}
