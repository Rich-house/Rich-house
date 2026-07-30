export interface AdminPaginationMeta {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface AdminPagedResponse<TItem> {
  items: TItem[];
  meta: AdminPaginationMeta;
}

export interface AdminLookupItem {
  id: number;
  name: string;
  subtitle?: string | null;
}

export interface AdminRecentItem {
  id: number;
  title: string;
  subtitle?: string | null;
  imageUrl?: string | null;
  createdAt: string;
  status: string;
}

export interface AdminDashboardSummary {
  totalProducts: number;
  activeProducts: number;
  inactiveProducts: number;
  totalCategories: number;
  activeOffers: number;
  activeProductDiscounts: number;
  lowStockProducts: number;
  recentProducts: AdminRecentItem[];
  recentOffers: AdminRecentItem[];
}
