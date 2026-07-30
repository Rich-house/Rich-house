export interface AdminOfferQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: string | null;
}

export interface AdminOfferItem {
  id: number;
  title: string;
  imageUrl: string | null;
  discountType: string;
  discountValue: number;
  startDate: string;
  endDate: string;
  isActive: boolean;
  productCount: number;
  categoryCount: number;
  status: string;
}

export interface AdminOfferDetails {
  id: number;
  title: string;
  description: string | null;
  imageUrl: string | null;
  discountType: string;
  discountValue: number;
  startDate: string;
  endDate: string;
  isActive: boolean;
  productIds: number[];
  categoryIds: number[];
}
