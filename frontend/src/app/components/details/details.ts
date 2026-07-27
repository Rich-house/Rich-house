import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ProductsService } from '../../Services/product';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Auth } from '../../Services/auth';
import Swal from 'sweetalert2';
import { ProductCard, ProductDetails } from '../../models/product.models';
import { SeoService } from '../../core/services/seo.service';
import { WhatsAppButtonComponent } from '../../shared/components/whatsapp-button/whatsapp-button';
import {
  productPlaceholderImage,
  resolveCatalogThumbnailUrl,
  resolveImageUrl,
} from '../../core/utils/image-url';
import { ImageFallbackDirective } from '../../shared/directives/image-fallback.directive';

@Component({
  selector: 'app-details',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, ImageFallbackDirective, WhatsAppButtonComponent],
  templateUrl: './details.html'
})
export class Details implements OnInit {
  product: ProductDetails | null = null;
  relatedProducts: ProductCard[] = [];
  reviews: any[] = [];
  loading = true;
  loadingRelated = false;

  newReview = {
    rating: 5,
    comment: ''
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
    5: 'XX-Large'
  };

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private productService: ProductsService,
    private cdr: ChangeDetectorRef,
    public authService: Auth,
    private seoService: SeoService
  ) { }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
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
      error: (err) => console.error('Error loading reviews', err)
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
      comment: this.newReview.comment
    };

    this.productService.addReview(reviewData).subscribe({
      next: () => {
        this.isSubmittingReview = false;
        Swal.fire({
          icon: 'success',
          title: 'Review Submitted',
          text: 'Thank you for your feedback!',
          timer: 2000,
          showConfirmButton: false
        });
        this.loadReviews(product.id);
        this.newReview = { rating: 5, comment: '' }; 
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isSubmittingReview = false;
        Swal.fire('Error', 'Failed to post review. Please make sure you are logged in.', 'error');
        this.cdr.detectChanges();
      }
    });
  }

  getStars(rating: number): number[] {
    return Array(rating).fill(0);
  }

  getSizeLabel(size: any): string {
    const numSize = Number(size);
    return this.sizeLabels[numSize] || size;
  }

  loadProduct(id: string) {
    this.loading = true;
    this.productService.getProductById(id).subscribe({
      next: (res) => {
        this.handleLoadedProduct(res);
      },
      error: (err) => {
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  loadProductBySlug(slug: string) {
    this.loading = true;
    this.productService.getProductBySlug(slug).subscribe({
      next: (res) => {
        this.handleLoadedProduct(res);
      },
      error: (err) => {
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  loadRelated(catId: number, currentId: number) {
    this.loadingRelated = true;
    this.productService.getProductsByCategoryId(catId).subscribe({
      next: (res: ProductCard[]) => {
        this.relatedProducts = res.filter((product) => product.id !== currentId);
        this.loadingRelated = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.loadingRelated = false;
      }
    });
  }

  get galleryImages(): string[] {
    const images = this.product?.imageUrls ?? [];
    return images.length > 0 ? images.map((imageUrl) => resolveImageUrl(imageUrl)) : [this.fallbackImage];
  }

  resolveRelatedProductImage(product: ProductCard): string {
    return resolveCatalogThumbnailUrl(product.imageUrls[0]);
  }

  get productWhatsAppLabel(): string {
    return this.product ? `Ask about ${this.product.name}` : 'Ask on WhatsApp';
  }

  addToCart() {
    const product = this.product;
    if (!product) {
      return;
    }

    if (!this.authService.hasValidSession()) {
      Swal.fire({
        title: 'Login Required',
        text: 'Please login first to add items to your bag!',
        icon: 'info',
        showCancelButton: true,
        confirmButtonColor: '#0d6efd',
        confirmButtonText: 'Go to Login',
        cancelButtonText: 'Cancel'
      }).then((result) => {
        if (result.isConfirmed) {
          this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
        }
      });
      return;
    }

    if (product.sizes.length > 0 && !this.selectedSize) {
      Swal.fire({
        title: 'Select Size',
        text: 'Please choose a size before adding to cart',
        icon: 'warning',
        confirmButtonColor: '#0d6efd',
        confirmButtonText: 'Got it!'
      });
      return;
    }

    this.isAdding = true;
    const cartPayload = {
      productId: product.id,
      quantity: this.quantity,
      selectedSize: this.selectedSize
    };

    this.productService.addToCart(cartPayload).subscribe({
      next: (res) => {
        this.isAdding = false;
        Swal.fire({
          position: 'top-end',
          icon: 'success',
          title: 'Added to cart!',
          showConfirmButton: false,
          timer: 1500,
          toast: true
        });
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isAdding = false;
        Swal.fire({
          icon: 'error',
          title: 'Oops...',
          text: 'Failed to add item to cart. Please try again.',
        });
        this.cdr.detectChanges();
      }
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
