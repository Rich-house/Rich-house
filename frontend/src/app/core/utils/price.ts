const egpPriceFormatter = new Intl.NumberFormat('en-EG', {
  maximumFractionDigits: 0,
});

export const formatEgpPrice = (value: number | null | undefined): string => {
  if (typeof value !== 'number' || Number.isNaN(value)) {
    return 'EGP —';
  }

  return `EGP ${egpPriceFormatter.format(value)}`;
};
