import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  OnInit,
  inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ProductsService } from '../../../core/services/product';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Auth } from '../../../core/services/auth';
import type { SweetAlertIcon, SweetAlertOptions } from 'sweetalert2';
import { AddToCartRequest } from '../../../shared/models/cart.models';
import { ProductCard, ProductDetails } from '../../../shared/models/product.models';
import { SeoService } from '../../../core/services/seo.service';
import { richHouseUi } from '../../../core/config/site-settings.config';
import { WhatsAppButtonComponent } from '../../../shared/components/whatsapp-button/whatsapp-button';
import { ProductCardComponent } from '../../../shared/components/product-card/product-card';
import {
  productPlaceholderImage,
  resolveCatalogThumbnailUrl,
  resolveImageUrl,
  resolveOptimizedProductImageUrl,
  resolveResponsiveProductImageUrl,
} from '../../../core/utils/image-url';
import { ImageFallbackDirective } from '../../../shared/directives/image-fallback.directive';
import { EgpPricePipe } from '../../../shared/pipes/egp-price.pipe';

@Component({
  selector: 'app-details',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    ImageFallbackDirective,
    WhatsAppButtonComponent,
    ProductCardComponent,
    EgpPricePipe,
  ],
  templateUrl: './details.html',
  styleUrl: './details.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Details implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  product: ProductDetails | null = null;
  relatedProducts: ProductCard[] = [];
  reviews: any[] = [];
  loading = true;
  activeImageIndex = 0;

  newReview = {
    rating: 5,
    comment: '',
  };
  isSubmittingReview = false;

  selectedSize = '';
  quantity = 1;
  isAdding = false;
  sizeSelectionMessage = '';
  readonly fallbackImage = productPlaceholderImage;
  readonly fallbackDescription =
    'A refined formalwear piece designed for an elegant and confident look.';
  readonly sizeLabels: Record<number, string> = {
    1: 'Small',
    2: 'Medium',
    3: 'Large',
    4: 'X-Large',
    5: 'XX-Large',
  };

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly productService: ProductsService,
    private readonly cdr: ChangeDetectorRef,
    public readonly authService: Auth,
    private readonly seoService: SeoService,
  ) {}

  ngOnInit(): void {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const slug = params.get('slug');
      const id = params.get('id');
      if (slug) {
        this.loadProductBySlug(slug);
      } else if (id) {
        this.loadProduct(id);
      } else {
        this.loading = false;
      }
    });
  }

  get galleryImages(): string[] {
    const images = this.product?.imageUrls ?? [];
    return images.length > 0
      ? images.map((imageUrl) => resolveOptimizedProductImageUrl(imageUrl))
      : [this.fallbackImage];
  }

  get galleryThumbnails(): string[] {
    const images = this.product?.imageUrls ?? [];
    return images.length > 0
      ? images.map((imageUrl) => resolveCatalogThumbnailUrl(imageUrl))
      : [this.fallbackImage];
  }

  get hasMultipleImages(): boolean {
    return this.galleryImages.length > 1;
  }

  get activeGalleryImage(): string {
    return this.galleryImages[this.activeImageIndex] ?? this.fallbackImage;
  }

  get activeGalleryImageSrcset(): string {
    const source = this.product?.imageUrls[this.activeImageIndex];
    return `${resolveResponsiveProductImageUrl(source)} 720w, ${this.activeGalleryImage} 1024w`;
  }

  get activeGalleryImageFallbacks(): readonly string[] {
    return [resolveImageUrl(this.product?.imageUrls[this.activeImageIndex]), this.fallbackImage];
  }

  galleryThumbnailFallbacks(index: number): readonly string[] {
    return [resolveImageUrl(this.product?.imageUrls[index]), this.fallbackImage];
  }

  get productSummary(): string {
    const shortDescription = this.sanitizeCustomerCopy(this.product?.shortDescription);
    const longDescription = this.sanitizeCustomerCopy(this.product?.description);
    const source = shortDescription || longDescription;

    if (!source) {
      return this.fallbackDescription;
    }

    const sentenceMatch = source.match(/^.*?[.!?](?:\s|$)/);
    const summary = sentenceMatch?.[0]?.trim() || source.trim();
    return summary.length > 170 ? `${summary.slice(0, 167).trimEnd()}...` : summary;
  }

  get displayDescription(): string {
    return this.sanitizeCustomerCopy(this.product?.description) || this.fallbackDescription;
  }

  get displayCategory(): string {
    return this.product?.categoryName?.trim() || 'Ready-to-Wear';
  }

  get availabilityLabel(): string {
    return (this.product?.stockQuantity ?? 0) > 0 ? 'In stock' : 'Currently unavailable';
  }

  get hasCompareAtPrice(): boolean {
    const product = this.product;
    return !!product?.compareAtPrice && product.compareAtPrice > product.price;
  }

  get discountPercent(): number | null {
    const product = this.product;
    if (!product || !this.hasCompareAtPrice || !product.compareAtPrice) {
      return null;
    }

    return Math.round(((product.compareAtPrice - product.price) / product.compareAtPrice) * 100);
  }

  get hasSizes(): boolean {
    return !!this.product?.sizes?.length;
  }

  get sizeIsRequired(): boolean {
    return this.hasSizes;
  }

  get maxQuantity(): number {
    const stockQuantity = this.product?.stockQuantity ?? 1;
    return Math.max(1, Math.min(99, stockQuantity));
  }

  get canSubmitCart(): boolean {
    const product = this.product;
    if (!product || product.stockQuantity <= 0 || this.isAdding) {
      return false;
    }

    return !this.sizeIsRequired || !!this.selectedSize;
  }

  loadReviews(productId: string | number): void {
    this.productService
      .getProductReviews(productId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
      next: (data) => {
        this.reviews = data;
        this.cdr.detectChanges();
      },
      error: () => undefined,
    });
  }

  submitReview(): void {
    if (!this.newReview.comment.trim()) {
      void this.showMessage('Note', 'Please write your comment before submitting.', 'info');
      return;
    }

    const product = this.product;
    if (!product) {
      return;
    }

    this.isSubmittingReview = true;
    const reviewData = {
      productId: product.id,
      rating: Number(this.newReview.rating),
      comment: this.newReview.comment,
    };

    this.productService.addReview(reviewData).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.isSubmittingReview = false;
        void this.showAlert({
          icon: 'success',
          title: 'Review Submitted',
          text: 'Thank you for your feedback!',
          timer: 2000,
          showConfirmButton: false,
        });
        this.loadReviews(product.id);
        this.newReview = { rating: 5, comment: '' };
        this.cdr.detectChanges();
      },
      error: () => {
        this.isSubmittingReview = false;
        void this.showMessage(
          'Error',
          'Failed to post review. Please make sure you are logged in.',
          'error',
        );
        this.cdr.detectChanges();
      },
    });
  }

  getStars(rating: number): number[] {
    return Array(rating).fill(0);
  }

  getSizeLabel(size: string | number): string {
    const numSize = Number(size);
    return this.sizeLabels[numSize] || String(size);
  }

  selectImage(index: number): void {
    if (index < 0 || index >= this.galleryImages.length) {
      return;
    }

    this.activeImageIndex = index;
  }

  previousImage(): void {
    if (!this.hasMultipleImages) {
      return;
    }

    const previousIndex =
      this.activeImageIndex === 0 ? this.galleryImages.length - 1 : this.activeImageIndex - 1;
    this.selectImage(previousIndex);
  }

  nextImage(): void {
    if (!this.hasMultipleImages) {
      return;
    }

    const nextIndex =
      this.activeImageIndex === this.galleryImages.length - 1 ? 0 : this.activeImageIndex + 1;
    this.selectImage(nextIndex);
  }

  onGalleryKeydown(event: KeyboardEvent): void {
    if (!this.hasMultipleImages) {
      return;
    }

    if (event.key === 'ArrowLeft') {
      event.preventDefault();
      this.previousImage();
    }

    if (event.key === 'ArrowRight') {
      event.preventDefault();
      this.nextImage();
    }
  }

  onSizeChange(size: string): void {
    this.selectedSize = size;
    this.sizeSelectionMessage = '';
  }

  increaseQuantity(): void {
    this.quantity = Math.min(this.maxQuantity, this.quantity + 1);
  }

  decreaseQuantity(): void {
    this.quantity = Math.max(1, this.quantity - 1);
  }

  trackByProductId(_index: number, product: ProductCard): number {
    return product.id;
  }

  loadProduct(id: string): void {
    this.loading = true;
    this.productService.getProductById(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.handleLoadedProduct(res);
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }

  loadProductBySlug(slug: string): void {
    this.loading = true;
    this.productService.getProductBySlug(slug).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.handleLoadedProduct(res);
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }

  loadRelated(catId: number, currentId: number): void {
    this.productService
      .getProductsByCategoryId(catId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
      next: (res: ProductCard[]) => {
        this.relatedProducts = res.filter((product) => product.id !== currentId);
        this.cdr.detectChanges();
      },
      error: () => undefined,
    });
  }

  addToCart(): void {
    const product = this.product;
    if (!product) {
      return;
    }

    if (this.sizeIsRequired && !this.selectedSize) {
      this.sizeSelectionMessage = 'Please select a size before adding this piece to your bag.';
      this.cdr.detectChanges();
      return;
    }

    this.isAdding = true;
    const cartPayload: AddToCartRequest = {
      productId: product.id,
      quantity: this.quantity,
      productImage: product.imageUrls[0] ?? '',
      productName: product.name,
      selectedSize: this.selectedSize || '',
      unitPrice: product.price,
    };

    this.productService.addToCart(cartPayload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.isAdding = false;
        void this.showAlert({
          position: 'top-end',
          icon: 'success',
          title: 'Added to bag',
          showConfirmButton: false,
          timer: 1500,
          toast: true,
        });
        this.cdr.detectChanges();
      },
      error: () => {
        this.isAdding = false;
        void this.showAlert({
          icon: 'error',
          title: 'Oops...',
          text: 'Failed to add item to your bag. Please try again.',
          confirmButtonColor: richHouseUi.modalConfirmColor,
        });
        this.cdr.detectChanges();
      },
    });
  }

  private handleLoadedProduct(product: ProductDetails): void {
    this.product = product;
    this.activeImageIndex = 0;
    this.quantity = 1;
    this.selectedSize = '';
    this.sizeSelectionMessage = '';
    this.loading = false;
    this.seoService.setProductSeo(product, this.router.url);
    this.loadReviews(product.id);
    this.loadRelated(product.categoryId, product.id);
    this.cdr.detectChanges();
  }

  private async showAlert(options: SweetAlertOptions): Promise<void> {
    const { default: Swal } = await import('sweetalert2');
    await Swal.fire(options);
  }

  private async showMessage(
    title: string,
    message: string,
    icon: SweetAlertIcon,
  ): Promise<void> {
    const { default: Swal } = await import('sweetalert2');
    await Swal.fire(title, message, icon);
  }

  private sanitizeCustomerCopy(copy?: string | null): string {
    const cleaned = copy?.replace(/\s+/g, ' ').trim() ?? '';
    if (!cleaned) {
      return '';
    }

    if (
      /provisional catalog entry/i.test(cleaned)
      || /inferred from the supplied photography/i.test(cleaned)
    ) {
      return '';
    }

    return cleaned;
  }
}
