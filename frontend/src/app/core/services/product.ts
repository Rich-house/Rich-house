import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { BehaviorSubject, Observable, of, shareReplay, throwError } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { Auth } from './auth';
import { apiConfig } from '../config/api.config';
import {
  AddToCartRequest,
  CartItem,
  CartResponse,
  isSameCartItem,
} from '../../shared/models/cart.models';
import { richHouseBrand } from '../config/site-settings.config';
import { CategoryItem } from '../../shared/models/category.models';
import {
  CatalogQuery,
  CatalogResponse,
  ProductCard,
  ProductDetails,
} from '../../shared/models/product.models';

@Injectable({
  providedIn: 'root',
})
export class ProductsService {
  private readonly guestCartStorageKey = 'rich_house_guest_cart';
  private http = inject(HttpClient);
  private authService = inject(Auth);
  private categoriesCache$?: Observable<CategoryItem[]>;

  private cartCountSubject = new BehaviorSubject<number>(0);
  cartCount$ = this.cartCountSubject.asObservable();

  constructor() {
    this.updateCartCount();
  }

  getCategories(forceRefresh = false): Observable<CategoryItem[]> {
    if (!this.categoriesCache$ || forceRefresh) {
      this.categoriesCache$ = this.http.get<CategoryItem[]>(apiConfig.categories).pipe(
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
      sort: 'featured',
    }).pipe(map((response) => response.items));
  }

  getProducts(): Observable<ProductCard[]> {
    return this.getCatalog({
      page: 1,
      pageSize: 48,
      sort: 'featured',
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

  updateCartCount() {
    this.getCart().subscribe({
      next: (res) => {
        this.cartCountSubject.next(res.totalCount);
      },
      error: () => this.cartCountSubject.next(0),
    });
  }

  getCart(): Observable<CartResponse> {
    if (!this.authService.hasValidSession()) {
      return of(this.readGuestCart());
    }

    return this.http.get<CartResponse>(`${apiConfig.cart}/GetCart`);
  }

  addToCart(cartItem: AddToCartRequest): Observable<{ message: string }> {
    if (!this.authService.hasValidSession()) {
      const cart = this.readGuestCart();
      const normalizedSize = cartItem.selectedSize || '';
      const normalizedColor = cartItem.color ?? null;
      const existingItem = cart.items.find((item) =>
        isSameCartItem(item, cartItem.productId, normalizedSize, normalizedColor),
      );

      if (existingItem) {
        existingItem.quantity += cartItem.quantity;
        existingItem.totalItemPrice = existingItem.unitPrice * existingItem.quantity;
      } else {
        const unitPrice = cartItem.unitPrice ?? 0;
        cart.items.push({
          productId: cartItem.productId,
          productName: cartItem.productName?.trim() || richHouseBrand.fallbackProductName,
          productImage: cartItem.productImage ?? '',
          unitPrice,
          quantity: cartItem.quantity,
          size: normalizedSize,
          color: normalizedColor,
          totalItemPrice: unitPrice * cartItem.quantity,
        });
      }

      this.persistGuestCart(cart.items);
      return of({ message: 'Product added to cart' });
    }

    return this.http
      .post<{ message: string }>(`${apiConfig.cart}/Add-To-Cart`, {
        productId: cartItem.productId,
        quantity: cartItem.quantity,
        selectedSize: cartItem.selectedSize,
      })
      .pipe(tap(() => this.updateCartCount()));
  }

  updateQuantity(
    productId: number,
    newQuantity: number,
    selectedSize = '',
    color: string | null = null,
  ): Observable<any> {
    if (!this.authService.hasValidSession()) {
      const cart = this.readGuestCart();
      const updatedItems = cart.items.map((item) =>
        isSameCartItem(item, productId, selectedSize, color)
          ? {
              ...item,
              quantity: newQuantity,
              totalItemPrice: item.unitPrice * newQuantity,
            }
          : item,
      );

      this.persistGuestCart(updatedItems);
      return of({ message: 'Cart updated' });
    }

    const body = { productId, newQuantity };
    return this.http.put(`${apiConfig.cart}/update-quantity`, body).pipe(tap(() => this.updateCartCount()));
  }

  removeItem(productId: number, selectedSize = '', color: string | null = null): Observable<any> {
    if (!this.authService.hasValidSession()) {
      const cart = this.readGuestCart();
      const updatedItems = cart.items.filter(
        (item) => !isSameCartItem(item, productId, selectedSize, color),
      );

      this.persistGuestCart(updatedItems);
      return of({ message: 'Item removed' });
    }

    return this.http
      .delete(`${apiConfig.cart}/removeItem/${productId}`)
      .pipe(tap(() => this.updateCartCount()));
  }

  getProductReviews(productId: number | string): Observable<any[]> {
    return this.http.get<any[]>(`${apiConfig.reviews}/product/${productId}`);
  }

  addReview(reviewData: any): Observable<any> {
    return this.http.post(apiConfig.reviews, reviewData);
  }

  private readGuestCart(): CartResponse {
    const storedCart = localStorage.getItem(this.guestCartStorageKey);

    if (!storedCart) {
      return this.buildCartResponse([]);
    }

    try {
      const parsedCart = JSON.parse(storedCart) as Partial<CartResponse>;
      const items = Array.isArray(parsedCart.items)
        ? parsedCart.items
            .map((item) => this.normalizeGuestCartItem(item as Partial<CartItem>))
            .filter((item): item is CartItem => item !== null)
        : [];

      return this.buildCartResponse(items);
    } catch {
      localStorage.removeItem(this.guestCartStorageKey);
      return this.buildCartResponse([]);
    }
  }

  private persistGuestCart(items: CartItem[]): void {
    const guestCart = this.buildCartResponse(items);
    localStorage.setItem(this.guestCartStorageKey, JSON.stringify(guestCart));
    this.cartCountSubject.next(guestCart.totalCount);
  }

  private buildCartResponse(items: CartItem[]): CartResponse {
    return {
      cartId: 0,
      items,
      totalPrice: items.reduce((sum, item) => sum + item.totalItemPrice, 0),
      totalCount: items.reduce((sum, item) => sum + item.quantity, 0),
    };
  }

  private normalizeGuestCartItem(item: Partial<CartItem> | null | undefined): CartItem | null {
    const productId = Number(item?.productId);
    const quantity = Math.max(1, Number(item?.quantity) || 1);
    const unitPrice = Number(item?.unitPrice) || 0;

    if (!Number.isFinite(productId) || productId <= 0) {
      return null;
    }

    return {
      productId,
      productName: item?.productName?.trim() || richHouseBrand.fallbackProductName,
      productImage: item?.productImage ?? '',
      unitPrice,
      quantity,
      size: item?.size ?? '',
      color: item?.color ?? null,
      totalItemPrice: unitPrice * quantity,
    };
  }
}
