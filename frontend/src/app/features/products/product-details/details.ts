import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ProductsService } from '../../../core/services/product';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Auth } from '../../../core/services/auth';
import Swal from 'sweetalert2';
import { AddToCartRequest } from '../../../shared/models/cart.models';
import { ProductCard, ProductDetails } from '../../../shared/models/product.models';
import { SeoService } from '../../../core/services/seo.service';
import { richHouseUi } from '../../../core/config/site-settings.config';
import { WhatsAppButtonComponent } from '../../../shared/components/whatsapp-button/whatsapp-button';
import {
  productPlaceholderImage,
  resolveCatalogThumbnailUrl,
  resolveImageUrl,
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
    EgpPricePipe,
  ],
  templateUrl: './details.html',
  styleUrl: './details.css',
})
export class Details implements OnInit {
  product: ProductDetails | null = null;
  relatedProducts: ProductCard[] = [];
  reviews: any[] = [];
  loading = true;

  newReview = {
    rating: 5,
    comment: '',
  };
  isSubmittingReview = false;

  selectedSize: string = '';
  quantity: number = 1;
  isAdding = false;
  readonly fallbackImage = productPlaceholderImage;

  sizeLabels: { [key: number]: string } = {
    1: 'Small',
    2: 'Medium',
    3: 'Large',
    4: 'X-Large',
    5: 'XX-Large',
  };

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private productService: ProductsService,
    private cdr: ChangeDetectorRef,
    public authService: Auth,
    private seoService: SeoService,
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
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

  loadReviews(productId: string | number) {
    this.productService.getProductReviews(productId).subscribe({
      next: (data) => {
        this.reviews = data;
        this.cdr.detectChanges();
      },
      error: () => undefined,
    });
  }

  submitReview() {
    if (!this.newReview.comment.trim()) {
      Swal.fire('Note', 'Please write your comment before submitting.', 'info');
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

    this.productService.addReview(reviewData).subscribe({
      next: () => {
        this.isSubmittingReview = false;
        Swal.fire({
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
        Swal.fire('Error', 'Failed to post review. Please make sure you are logged in.', 'error');
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

  loadProduct(id: string) {
    this.loading = true;
    this.productService.getProductById(id).subscribe({
      next: (res) => {
        this.handleLoadedProduct(res);
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }

  loadProductBySlug(slug: string) {
    this.loading = true;
    this.productService.getProductBySlug(slug).subscribe({
      next: (res) => {
        this.handleLoadedProduct(res);
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }

  loadRelated(catId: number, currentId: number) {
    this.productService.getProductsByCategoryId(catId).subscribe({
      next: (res: ProductCard[]) => {
        this.relatedProducts = res.filter((product) => product.id !== currentId);
        this.cdr.detectChanges();
      },
      error: () => undefined,
    });
  }

  get galleryImages(): string[] {
    const images = this.product?.imageUrls ?? [];
    return images.length > 0
      ? images.map((imageUrl) => resolveImageUrl(imageUrl))
      : [this.fallbackImage];
  }

  resolveRelatedProductImage(product: ProductCard): string {
    return resolveCatalogThumbnailUrl(product.imageUrls[0]);
  }

  addToCart() {
    const product = this.product;
    if (!product) {
      return;
    }

    if (product.sizes.length > 0 && !this.selectedSize) {
      Swal.fire({
        title: 'Select Size',
        text: 'Please choose a size before adding to cart',
        icon: 'warning',
        confirmButtonColor: richHouseUi.modalConfirmColor,
        confirmButtonText: 'Got it!',
      });
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

    this.productService.addToCart(cartPayload).subscribe({
      next: () => {
        this.isAdding = false;
        Swal.fire({
          position: 'top-end',
          icon: 'success',
          title: 'Added to cart!',
          showConfirmButton: false,
          timer: 1500,
          toast: true,
        });
        this.cdr.detectChanges();
      },
      error: () => {
        this.isAdding = false;
        Swal.fire({
          icon: 'error',
          title: 'Oops...',
          text: 'Failed to add item to cart. Please try again.',
        });
        this.cdr.detectChanges();
      },
    });
  }

  private handleLoadedProduct(product: ProductDetails) {
    this.product = product;
    this.loading = false;
    this.seoService.setProductSeo(product, this.router.url);
    this.loadReviews(product.id);
    this.loadRelated(product.categoryId, product.id);
    this.cdr.detectChanges();
  }
}
