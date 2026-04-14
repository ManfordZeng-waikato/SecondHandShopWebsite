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

export type SaleRecordStatus = 'Completed' | 'Cancelled';

export type SaleCancellationReason =
  | 'BuyerBackedOut'
  | 'PaymentFailed'
  | 'AdminMistake'
  | 'OfflineCancelled'
  | 'Other';

export const saleCancellationReasonOptions: SaleCancellationReason[] = [
  'BuyerBackedOut',
  'PaymentFailed',
  'AdminMistake',
  'OfflineCancelled',
  'Other',
];

export const saleCancellationReasonLabels: Record<SaleCancellationReason, string> = {
  BuyerBackedOut: 'Buyer backed out',
  PaymentFailed: 'Payment failed',
  AdminMistake: 'Admin mistake',
  OfflineCancelled: 'Offline cancelled',
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
  status: SaleRecordStatus;
  cancelledAtUtc: string | null;
  cancellationReason: SaleCancellationReason | null;
  cancellationNote: string | null;
  createdByAdminUserId: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface ProductInquiryOption {
  inquiryId: string;
  customerName: string | null;
  email: string | null;
  phoneNumber: string | null;
  message: string;
  createdAt: string;
  linkedSaleId: string | null;
}

/** Input for POST /api/lord/products/{id}/mark-sold — creates a new sale record. */
export interface MarkProductSoldInput {
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

/** Input for POST /api/lord/products/{id}/revert-sale — cancels the current sale. */
export interface RevertProductSaleInput {
  reason: SaleCancellationReason;
  cancellationNote?: string | null;
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
