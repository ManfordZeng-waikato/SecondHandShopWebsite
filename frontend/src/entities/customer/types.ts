export type CustomerSortBy = 'createdAt' | 'updatedAt' | 'lastInquiryAt';
export type SortDirection = 'asc' | 'desc';
export type CustomerStatus = 'New' | 'Contacted' | 'Qualified' | 'Archived';

export const customerStatusOptions: CustomerStatus[] = [
  'New',
  'Contacted',
  'Qualified',
  'Archived',
];

export interface CustomerListItem {
  id: string;
  name: string | null;
  email: string | null;
  phone: string | null;
  status: CustomerStatus;
  inquiryCount: number;
  lastInquiryAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CustomerDetail {
  id: string;
  name: string | null;
  email: string | null;
  phone: string | null;
  status: CustomerStatus;
  notes: string | null;
  inquiryCount: number;
  lastInquiryAt: string | null;
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
  sortBy?: CustomerSortBy;
  sortDirection?: SortDirection;
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
