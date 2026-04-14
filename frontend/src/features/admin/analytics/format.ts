const currency = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
  maximumFractionDigits: 0,
});

const currencyPrecise = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
  maximumFractionDigits: 2,
});

const integer = new Intl.NumberFormat('en-US');

const percent = new Intl.NumberFormat('en-US', {
  style: 'percent',
  maximumFractionDigits: 1,
});

export function formatCurrency(value: number): string {
  return currency.format(value);
}

export function formatCurrencyPrecise(value: number): string {
  return currencyPrecise.format(value);
}

export function formatInt(value: number): string {
  return integer.format(value);
}

export function formatPercent(ratio: number): string {
  return percent.format(ratio);
}

export function formatMonthLabel(iso: string): string {
  const date = new Date(iso);
  return date.toLocaleString('en-US', { month: 'short', year: '2-digit' });
}
