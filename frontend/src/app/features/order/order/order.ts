import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ProductsService } from '../../../core/services/product';
import { CartItem, CartResponse, getCartItemKey } from '../../../shared/models/cart.models';
import { productPlaceholderImage, resolveImageUrl } from '../../../core/utils/image-url';
import { buildCartOrderMessage } from '../../../core/utils/whatsapp-order';
import { ImageFallbackDirective } from '../../../shared/directives/image-fallback.directive';
import { WhatsAppButtonComponent } from '../../../shared/components/whatsapp-button/whatsapp-button';
import { EgpPricePipe } from '../../../shared/pipes/egp-price.pipe';

@Component({
  selector: 'app-order',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    ImageFallbackDirective,
    WhatsAppButtonComponent,
    EgpPricePipe,
  ],
  templateUrl: './order.html',
  styleUrls: ['./order.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrderComponent implements OnInit {
  cartData: CartResponse | null = null;
  loading = true;
  notes = '';
  readonly fallbackImage = productPlaceholderImage;
  readonly resolveImage = resolveImageUrl;
  readonly trackByCartItem = (_index: number, item: CartItem): string => getCartItemKey(item);

  constructor(
    private readonly productService: ProductsService,
    private readonly cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loadOrderReview();
  }

  get hasItems(): boolean {
    return (this.cartData?.items.length ?? 0) > 0;
  }

  get whatsAppMessage(): string | undefined {
    if (!this.cartData || this.cartData.items.length === 0) {
      return undefined;
    }

    return buildCartOrderMessage(this.cartData, this.notes);
  }

  loadOrderReview(): void {
    this.loading = true;
    this.productService.getCart().subscribe({
      next: (res) => {
        this.cartData = res;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loading = false;
        this.cdr.markForCheck();
      },
    });
  }
}
