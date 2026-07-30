export interface CategoryItem {
  id: number;
  name: string;
  slug: string | null;
  imageUrl: string | null;
}

export interface AdminCategoryItem {
  id: number;
  name: string;
  slug: string;
  imageUrl: string | null;
  isActive: boolean;
  displayOrder: number;
  productCount: number;
}

export interface AdminCategoryDetails {
  id: number;
  name: string;
  slug: string;
  imageUrl: string | null;
  isActive: boolean;
  displayOrder: number;
}
