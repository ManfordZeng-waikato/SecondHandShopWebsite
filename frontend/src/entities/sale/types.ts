export type PaymentMethod = 'Cash' | 'BankTransfer' | 'Card' | 'Other';

export const paymentMethodOptions: PaymentMethod[] = [
  'Cash',
  'BankTransfer',
  'Card',
  'Other',
];

export const paymentMethodLabels: Record<PaymentMethod, string> = {
  Cash: 'Cash',
  BankTransfer: 'Bank Transfer',
  Card: 'Card',
  Other: 'Other',
};

export interface ProductSaleDto {
  id: string;
  productId: string;
  customerId: string | null;
  inquiryId: string | null;
  listedPriceAtSale: number;
  finalSoldPrice: number;
  buyerName: string | null;
  buyerPhone: string | null;
  buyerEmail: string | null;
  soldAtUtc: string;
  paymentMethod: string | null;
  notes: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface SaveProductSaleInput {
  finalSoldPrice: number;
  soldAtUtc: string;
  customerId?: string | null;
  inquiryId?: string | null;
  buyerName?: string | null;
  buyerPhone?: string | null;
  buyerEmail?: string | null;
  paymentMethod?: string | null;
  notes?: string | null;
}

export interface CustomerSaleItem {
  saleId: string;
  productId: string;
  productTitle: string;
  productSlug: string | null;
  finalSoldPrice: number;
  soldAtUtc: string;
  paymentMethod: string | null;
  inquiryId: string | null;
}
