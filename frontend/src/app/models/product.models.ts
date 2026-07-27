export interface ProductCard {
  id: number;
  categoryId: number;
  name: string;
  slug: string | null;
  shortDescription: string | null;
  description: string;
  stockQuantity: number;
  price: number;
  compareAtPrice: number | null;
  offerPrice: number | null;
  offerStart: string | null;
  offerEnd: string | null;
  status: string;
  isFeatured: boolean;
  isBestSeller: boolean;
  isNewArrival: boolean;
  isOnOffer: boolean;
  categoryName: string | null;
  categorySlug: string | null;
  rating?: number | null;
  imageUrls: string[];
  sizes: string[];
}

export interface ProductVariant {
  id: number;
  optionKey: string;
  sku: string | null;
  barcode: string | null;
  colorName: string;
  colorSlug: string;
  sizeId: number;
  sizeName: string | null;
  stockQuantity: number;
  priceOverride: number | null;
  isActive: boolean;
  sortOrder: number;
}

export interface ProductDetails {
  id: number;
  categoryName: string | null;
  categorySlug?: string | null;
  name: string;
  description: string;
  price: number;
  stockQuantity: number;
  categoryId: number;
  imageUrls: string[];
  sizes: string[];
  slug: string | null;
  sku: string | null;
  barcode: string | null;
  status: string;
  shortDescription: string | null;
  compareAtPrice: number | null;
  offerPrice: number | null;
  offerStart: string | null;
  offerEnd: string | null;
  isActive: boolean;
  isFeatured: boolean;
  isBestSeller: boolean;
  isNewArrival: boolean;
  seoTitle: string | null;
  seoDescription: string | null;
  mainImageIndex?: number | null;
  variants: ProductVariant[];
}

export interface CatalogQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  categoryId?: number;
  categorySlug?: string;
  size?: string;
  color?: string;
  minPrice?: number;
  maxPrice?: number;
  featured?: boolean;
  bestSeller?: boolean;
  newArrival?: boolean;
  onOffer?: boolean;
  inStock?: boolean;
  sort?: string;
}

export interface PaginationMeta {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface CatalogResponse<TItem> {
  items: TItem[];
  meta: PaginationMeta;
}
