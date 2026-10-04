import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ProductsService } from '../../../core/services/product';
import Swal from 'sweetalert2';
import { CartItem, CartResponse, getCartItemKey } from '../../../shared/models/cart.models';
import { richHouseUi } from '../../../core/config/site-settings.config';
import { productPlaceholderImage, resolveCatalogThumbnailUrl, resolveImageUrl } from '../../../core/utils/image-url';
import { ImageFallbackDirective } from '../../../shared/directives/image-fallback.directive';
import { EgpPricePipe } from '../../../shared/pipes/egp-price.pipe';
@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, RouterModule, ImageFallbackDirective, EgpPricePipe],
  templateUrl: './cart.html',
  styleUrls: ['./cart.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Cart implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  cartData: CartResponse | null = null;
  loading = true;
  readonly fallbackImage = productPlaceholderImage;
  readonly resolveImage = resolveCatalogThumbnailUrl;
  readonly trackByCartItem = (_index: number, item: CartItem): string => getCartItemKey(item);

  constructor(
    private productService: ProductsService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loadCart();
  }

  imageFallbacks(path: string): readonly string[] {
    return [resolveImageUrl(path), this.fallbackImage];
  }

  loadCart() {
    this.loading = true;
    this.productService.getCart().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.cartData = res;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }

  changeQuantity(productId: number, currentQty: number, change: number, size: string, color?: string | null) {
    const newQty = currentQty + change;
    if (newQty < 1) return;

    this.productService.updateQuantity(productId, newQty, size, color ?? null).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.loadCart();
      },
      error: () => {
        alert('Failed to update quantity.');
      },
    });
  }

  deleteItem(productId: number, size: string, color?: string | null) {
    Swal.fire({
      title: 'Are you sure?',
      text: 'This item will be removed from your shopping bag',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: richHouseUi.modalConfirmColor,
      cancelButtonColor: '#d33',
      confirmButtonText: 'Yes, remove it!',
      cancelButtonText: 'Cancel',
      reverseButtons: true,
    }).then((result) => {
      if (result.isConfirmed) {
        this.productService.removeItem(productId, size, color ?? null).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
          next: () => {
            this.loadCart();

            Swal.fire({
              toast: true,
              position: 'top-end',
              icon: 'success',
              title: 'Item removed',
              showConfirmButton: false,
              timer: 1500,
              timerProgressBar: true,
            });
          },
          error: (err) => {
            if (err.status === 204) {
              this.loadCart();
            } else {
              Swal.fire({
                icon: 'error',
                title: 'Error',
                text: 'Could not remove the item. Please try again.',
                confirmButtonColor: richHouseUi.modalConfirmColor,
              });
            }
          },
        });
      }
    });
  }
}
