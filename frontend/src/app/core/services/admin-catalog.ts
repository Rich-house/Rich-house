import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { apiConfig } from '../config/api.config';
import { AdminDashboardSummary, AdminLookupItem, AdminPagedResponse } from '../../shared/models/admin.models';
import { AdminCategoryDetails, AdminCategoryItem } from '../../shared/models/category.models';
import {
  AdminProductDetails,
  AdminProductDiscountItem,
  AdminProductDiscountQuery,
  AdminProductListItem,
  AdminProductQuery,
} from '../../shared/models/product.models';
import { AdminOfferDetails, AdminOfferItem, AdminOfferQuery } from '../../shared/models/offer.models';

@Injectable({
  providedIn: 'root',
})
export class AdminCatalogService {
  private readonly http = inject(HttpClient);

  getDashboardSummary(): Observable<AdminDashboardSummary> {
    return this.http.get<AdminDashboardSummary>(`${apiConfig.adminCatalog}/summary`);
  }

  getSizeOptions(): Observable<AdminLookupItem[]> {
    return this.http.get<AdminLookupItem[]>(`${apiConfig.adminCatalog}/sizes`);
  }

  getCategories(): Observable<AdminCategoryItem[]> {
    return this.http.get<AdminCategoryItem[]>(`${apiConfig.categories}/admin`);
  }

  getCategoryById(id: number): Observable<AdminCategoryDetails> {
    return this.http.get<AdminCategoryDetails>(`${apiConfig.categories}/admin/${id}`);
  }

  createCategory(formData: FormData): Observable<AdminCategoryDetails> {
    return this.http.post<AdminCategoryDetails>(`${apiConfig.categories}/admin`, formData);
  }

  updateCategory(id: number, formData: FormData): Observable<AdminCategoryDetails> {
    return this.http.put<AdminCategoryDetails>(`${apiConfig.categories}/admin/${id}`, formData);
  }

  deleteCategory(id: number): Observable<void> {
    return this.http.delete<void>(`${apiConfig.categories}/admin/${id}`);
  }

  getProducts(query: AdminProductQuery): Observable<AdminPagedResponse<AdminProductListItem>> {
    return this.http.get<AdminPagedResponse<AdminProductListItem>>(`${apiConfig.products}/admin`, {
      params: this.buildParams(query),
    });
  }

  getProductDiscounts(query: AdminProductDiscountQuery): Observable<AdminPagedResponse<AdminProductDiscountItem>> {
    return this.http.get<AdminPagedResponse<AdminProductDiscountItem>>(`${apiConfig.adminCatalog}/product-discounts`, {
      params: this.buildParams(query),
    });
  }

  getProductById(id: number): Observable<AdminProductDetails> {
    return this.http.get<AdminProductDetails>(`${apiConfig.products}/admin/${id}`);
  }

  createProduct(formData: FormData): Observable<AdminProductDetails> {
    return this.http.post<AdminProductDetails>(apiConfig.products, formData);
  }

  updateProduct(id: number, formData: FormData): Observable<AdminProductDetails> {
    return this.http.put<AdminProductDetails>(`${apiConfig.products}/${id}`, formData);
  }

  deleteProduct(id: number): Observable<void> {
    return this.http.delete<void>(`${apiConfig.products}/${id}`);
  }

  getOffers(query: AdminOfferQuery): Observable<AdminPagedResponse<AdminOfferItem>> {
    return this.http.get<AdminPagedResponse<AdminOfferItem>>(apiConfig.offers, {
      params: this.buildParams(query),
    });
  }

  getOfferById(id: number): Observable<AdminOfferDetails> {
    return this.http.get<AdminOfferDetails>(`${apiConfig.offers}/${id}`);
  }

  createOffer(formData: FormData): Observable<AdminOfferDetails> {
    return this.http.post<AdminOfferDetails>(apiConfig.offers, formData);
  }

  updateOffer(id: number, formData: FormData): Observable<AdminOfferDetails> {
    return this.http.put<AdminOfferDetails>(`${apiConfig.offers}/${id}`, formData);
  }

  toggleOfferActivation(id: number, isActive: boolean): Observable<void> {
    return this.http.patch<void>(`${apiConfig.offers}/${id}/activation`, null, {
      params: new HttpParams().set('isActive', String(isActive)),
    });
  }

  deleteOffer(id: number): Observable<void> {
    return this.http.delete<void>(`${apiConfig.offers}/${id}`);
  }

  private buildParams(query: object): HttpParams {
    let params = new HttpParams();

    Object.entries(query as Record<string, unknown>).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    });

    return params;
  }
}
