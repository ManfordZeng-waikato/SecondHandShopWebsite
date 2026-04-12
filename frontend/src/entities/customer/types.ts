export type CustomerStatus = 'New' | 'Contacted' | 'Qualified' | 'Archived';
export type CustomerSource = 'Inquiry' | 'Sale' | 'Manual';

export const customerSourceLabels: Record<CustomerSource, string> = {
  Inquiry: 'Inquiry',
  Sale: 'Sale',
  Manual: 'Manual',
};

/** Values for admin list source filter (matches `CustomerSource`). */
export const customerSourceOptions: CustomerSource[] = ['Inquiry', 'Sale', 'Manual'];

/** Short labels for admin filter chips. */
export const customerSourceFilterLabels: Record<CustomerSource, string> = {
  Inquiry: 'Inquiry',
  Sale: 'Sale (buyer)',
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
  primarySource?: CustomerSource;
}

export interface CustomerInquiryQueryParams {
  page?: number;
  pageSize?: number;
}

export interface UpdateCustomerInput {
  name?: string;
  phoneNumber?: string;
  notes?: string;
}

export interface CreateCustomerInput {
  name?: string;
  email?: string;
  phoneNumber?: string;
  status?: CustomerStatus;
  notes?: string;
}

export interface CreateCustomerResult {
  id: string;
}

export interface CustomerConflictDetail {
  existingCustomerId: string;
  conflictField: 'email' | 'phoneNumber';
  message: string;
}

export interface EditableCustomer {
  id: string;
  name: string;
  email: string;
  phone: string;
  notes: string;
}
