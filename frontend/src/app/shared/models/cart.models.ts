export interface CartItem {
  productId: number;
  productName: string;
  productImage: string;
  unitPrice: number;
  quantity: number;
  size: string;
  color?: string | null;
  totalItemPrice: number;
}

export interface CartItemIdentity {
  productId: number;
  size: string;
  color?: string | null;
}

export interface CartResponse {
  cartId: number;
  items: CartItem[];
  totalPrice: number;
  totalCount: number;
}

export interface AddToCartRequest {
  productId: number;
  quantity: number;
  color?: string | null;
  productImage?: string;
  productName?: string;
  selectedSize: string;
  unitPrice?: number;
}

export const getCartItemKey = (item: CartItemIdentity): string =>
  `${item.productId}-${item.size}-${item.color ?? ''}`;

export const isSameCartItem = (
  item: CartItemIdentity,
  productId: number,
  selectedSize = '',
  color: string | null = null,
): boolean =>
  item.productId === productId &&
  item.size === selectedSize &&
  (item.color ?? null) === (color ?? null);
