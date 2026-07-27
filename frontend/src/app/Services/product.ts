import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { BehaviorSubject, Observable, shareReplay, throwError } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { Auth } from './auth';
import { apiConfig } from '../core/config/api.config';
import { CategoryItem } from '../models/category.models';
import {
  CatalogQuery,
  CatalogResponse,
  ProductCard,
  ProductDetails,
} from '../models/product.models';

@Injectable({
  providedIn: 'root'
})
export class ProductsService {
  private http = inject(HttpClient); 
  private authService = inject(Auth);
  private categoriesCache$?: Observable<CategoryItem[]>;

  private cartCountSubject = new BehaviorSubject<number>(0);
  cartCount$ = this.cartCountSubject.asObservable();

  private searchQuerySubject = new BehaviorSubject<string>('');
  searchQuery$ = this.searchQuerySubject.asObservable();

  constructor() {
    if (this.authService.hasValidSession()) {
      this.updateCartCount();
    }
  }

  
  getCategories(forceRefresh = false): Observable<CategoryItem[]> {
    if (!this.categoriesCache$ || forceRefresh) {
      this.categoriesCache$ = this.http
        .get<CategoryItem[]>(apiConfig.categories)
        .pipe(
          shareReplay({ bufferSize: 1, refCount: false }),
          catchError((error) => {
            this.categoriesCache$ = undefined;
            return throwError(() => error);
          }),
        );
    }

    return this.categoriesCache$;
  }

  deleteCategory(id: number): Observable<any> {
    return this.http.delete(`${apiConfig.categories}/${id}`);
  }

  getCategoryById(id: number): Observable<CategoryItem> {
    return this.http.get<CategoryItem>(`${apiConfig.categories}/GetById/${id}`);
  }

  getCategoryBySlug(slug: string): Observable<CategoryItem> {
    return this.http.get<CategoryItem>(`${apiConfig.categories}/slug/${slug}`);
  }

  updateCategory(id: number, categoryData: { name: string }): Observable<any> {
    const body = { id, name: categoryData.name };
    return this.http.put(`${apiConfig.categories}/Edit/${id}`, body);
  }

  addCategory(categoryData: { name: string }): Observable<any> {
    return this.http.post(apiConfig.categories, categoryData);
  }

  getCatalog(query: CatalogQuery = {}): Observable<CatalogResponse<ProductCard>> {
    let params = new HttpParams();

    Object.entries(query).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    });

    return this.http.get<CatalogResponse<ProductCard>>(`${apiConfig.products}/catalog`, { params });
  }

  getOfferProducts(query: CatalogQuery = {}): Observable<CatalogResponse<ProductCard>> {
    let params = new HttpParams();

    Object.entries(query).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    });

    return this.http.get<CatalogResponse<ProductCard>>(`${apiConfig.products}/offers`, { params });
  }

  getProductsByCategoryId(id: number): Observable<ProductCard[]> {
    return this.getCatalog({
      categoryId: id,
      page: 1,
      pageSize: 48,
      sort: 'featured'
    }).pipe(map((response) => response.items));
  }

  getProductsByCategorySlug(slug: string): Observable<ProductCard[]> {
    return this.getCatalog({
      categorySlug: slug,
      page: 1,
      pageSize: 48,
      sort: 'featured'
    }).pipe(map((response) => response.items));
  }


  getProducts(): Observable<ProductCard[]> {
    return this.getCatalog({
      page: 1,
      pageSize: 48,
      sort: 'featured'
    }).pipe(map((response) => response.items));
  }

  getProductById(id: string | number): Observable<ProductDetails> {
    return this.http.get<ProductDetails>(`${apiConfig.products}/${id}`);
  }

  getProductBySlug(slug: string): Observable<ProductDetails> {
    return this.http.get<ProductDetails>(`${apiConfig.products}/slug/${slug}`);
  }

  addProduct(productData: FormData): Observable<any> {
    return this.http.post<any>(apiConfig.products, productData);
  }

deleteProduct(id: number): Observable<any> {
  return this.http.delete(`${apiConfig.products}/${id}`);
}

  updateProduct(id: number, productData: FormData): Observable<any> {
    return this.http.put<any>(`${apiConfig.products}/${id}`, productData);
  }

  searchProducts(query: string): Observable<ProductCard[]> {
    return this.getCatalog({
      search: query,
      page: 1,
      pageSize: 48,
      sort: 'featured'
    }).pipe(map((response) => response.items));
  }

  setSearchQuery(query: string) {
    this.searchQuerySubject.next(query);
  }


  updateCartCount() {
    if (!this.authService.hasValidSession()) {
      this.cartCountSubject.next(0);
      return;
    }

    this.getCart().subscribe({
      next: (res) => {
        const items = res.data?.items || res.items || [];
        this.cartCountSubject.next(items.length);
      },
      error: () => this.cartCountSubject.next(0)
    });
  }

  getCart(): Observable<any> {
    return this.http.get(`${apiConfig.cart}/GetCart`);
  }

  addToCart(cartItem: any): Observable<any> {
    return this.http.post(`${apiConfig.cart}/Add-To-Cart`, cartItem)
      .pipe(tap(() => this.updateCartCount()));
  }

  updateQuantity(productId: number, newQuantity: number): Observable<any> {
    const body = { productId, newQuantity };
    return this.http.put(`${apiConfig.cart}/update-quantity`, body);
  }

  removeItem(productId: number): Observable<any> {
    return this.http.delete(`${apiConfig.cart}/removeItem/${productId}`)
      .pipe(tap(() => this.updateCartCount()));
  }


  getProductReviews(productId: number | string): Observable<any[]> {
    return this.http.get<any[]>(`${apiConfig.reviews}/product/${productId}`);
  }

  addReview(reviewData: any): Observable<any> {
    return this.http.post(apiConfig.reviews, reviewData);
  }

  submitOrder(orderData: any): Observable<any> {
    return this.http.post(`${apiConfig.orders}/checkout`, orderData);
  }
}
