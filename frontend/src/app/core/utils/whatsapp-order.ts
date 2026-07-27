import { CartItem, CartResponse } from '../../models/cart.models';
import { formatEgpPrice } from './price';

const buildCartItemMessage = (item: CartItem, index: number): string => {
  const lines = [`${index + 1}. ${item.productName}`];

  if (item.size.trim()) {
    lines.push(`Size: ${item.size.trim()}`);
  }

  if (item.color?.trim()) {
    lines.push(`Color: ${item.color.trim()}`);
  }

  lines.push(`Quantity: ${item.quantity}`);
  lines.push(`Price: ${formatEgpPrice(item.unitPrice)}`);

  return lines.join('\n');
};

export const buildCartOrderMessage = (cart: CartResponse, notes?: string | null): string => {
  const trimmedNotes = notes?.trim();
  const sections = [
    'Hello Rich House,',
    '',
    'I would like to inquire about these items:',
    '',
    cart.items.map(buildCartItemMessage).join('\n\n'),
    '',
    `Order Total: ${formatEgpPrice(cart.totalPrice)}`,
  ];

  if (trimmedNotes) {
    sections.push('', `Notes: ${trimmedNotes}`);
  }

  return sections.join('\n');
};
