export type CustomerStatus = 'New' | 'Contacted' | 'Qualified' | 'Archived';
export type CustomerSource = 'Inquiry' | 'Sale' | 'Manual';

export const customerStatusOptions: CustomerStatus[] = [
  'New',
  'Contacted',
  'Qualified',
  'Archived',
];

export const customerSourceLabels: Record<CustomerSource, string> = {
  Inquiry: 'Inquiry',
  Sale: 'Sale',
  Manual: 'Manual',
};

export interface CustomerListItem {
  id: string;
  name: string | null;
  email: string | null;
  phone: string | null;
  status: CustomerStatus;
  primarySource: CustomerSource;
  inquiryCount: number;
  lastInquiryAt: string | null;
  purchaseCount: number;
  totalSpent: number;
  lastPurchaseAtUtc: string | null;
  lastContactAtUtc: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CustomerDetail {
  id: string;
  name: string | null;
  email: string | null;
  phone: string | null;
  status: CustomerStatus;
  primarySource: CustomerSource;
  notes: string | null;
  inquiryCount: number;
  lastInquiryAt: string | null;
  purchaseCount: number;
  totalSpent: number;
  lastPurchaseAtUtc: string | null;
  lastContactAtUtc: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CustomerInquiryItem {
  inquiryId: string;
  productId: string;
  productTitle: string | null;
  productSlug: string | null;
  message: string;
  inquiryStatus: string;
  createdAt: string;
}

export interface AdminCustomerQueryParams {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: CustomerStatus;
}

export interface CustomerInquiryQueryParams {
  page?: number;
  pageSize?: number;
}

export interface UpdateCustomerInput {
  name?: string;
  phoneNumber?: string;
  status?: CustomerStatus;
  notes?: string;
}

export interface EditableCustomer {
  id: string;
  name: string;
  email: string;
  phone: string;
  status: CustomerStatus;
  notes: string;
}
