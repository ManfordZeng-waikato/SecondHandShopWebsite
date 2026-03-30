import type { Category } from '../../entities/category/types';
import type {
  AdminCustomerQueryParams,
  CustomerDetail,
  CustomerInquiryItem,
  CustomerInquiryQueryParams,
  CustomerListItem,
  CustomerStatus,
  CustomerSortBy,
  SortDirection,
  UpdateCustomerInput,
} from '../../entities/customer/types';
import { customerStatusOptions } from '../../entities/customer/types';
import type { CreateInquiryInput, CreateInquiryResponse } from '../../entities/inquiry/types';
import type {
  CreateProductInput,
  PagedResult,
  Product,
  ProductListItem,
  ProductStatus,
} from '../../entities/product/types';
import { mockCategories, mockProducts } from './data';

let productsStore: Product[] = [...mockProducts];
const categoriesStore: Category[] = [...mockCategories];
const inquiryStore: CreateInquiryInput[] = [];
const FEATURED_SORT_ORDER_MIN = 0;
const FEATURED_SORT_ORDER_MAX = 999;
let featuredStateStore: Record<string, { isFeatured: boolean; featuredSortOrder: number | null }> =
  Object.fromEntries(productsStore.map((product) => [product.id, { isFeatured: false, featuredSortOrder: null }]));

interface MockCustomer {
  id: string;
  name: string | null;
  email: string | null;
  phoneNumber: string | null;
  status: CustomerStatus;
  notes: string | null;
  createdAt: string;
  updatedAt: string;
}

interface MockCustomerInquiry {
  inquiryId: string;
  customerId: string;
  productId: string;
  productTitle: string | null;
  productSlug: string | null;
  message: string;
  inquiryStatus: string;
  createdAt: string;
}

function isoDaysAgo(days: number): string {
  return new Date(Date.now() - days * 24 * 60 * 60 * 1000).toISOString();
}

let customersStore: MockCustomer[] = [
  {
    id: 'cust-001',
    name: 'Emma Stone',
    email: 'emma@example.com',
    phoneNumber: '+64 21 555 123',
    status: 'Contacted',
    notes: 'Prefers WhatsApp after 6 PM.',
    createdAt: isoDaysAgo(14),
    updatedAt: isoDaysAgo(2),
  },
  {
    id: 'cust-002',
    name: 'Liam Parker',
    email: 'liam@example.com',
    phoneNumber: null,
    status: 'New',
    notes: null,
    createdAt: isoDaysAgo(9),
    updatedAt: isoDaysAgo(1),
  },
];

let customerInquiriesStore: MockCustomerInquiry[] = [
  {
    inquiryId: 'inq-001',
    customerId: 'cust-001',
    productId: productsStore[0]?.id ?? 'mock-product-001',
    productTitle: productsStore[0]?.title ?? 'Vintage Cabinet',
    productSlug: productsStore[0]?.slug ?? null,
    message: 'Is this still available? Can I arrange pickup this week?',
    inquiryStatus: 'Sent',
    createdAt: isoDaysAgo(5),
  },
  {
    inquiryId: 'inq-002',
    customerId: 'cust-001',
    productId: productsStore[1]?.id ?? 'mock-product-002',
    productTitle: productsStore[1]?.title ?? 'Antique Side Table',
    productSlug: productsStore[1]?.slug ?? null,
    message: 'Can you share the exact dimensions and condition?',
    inquiryStatus: 'Pending',
    createdAt: isoDaysAgo(3),
  },
  {
    inquiryId: 'inq-003',
    customerId: 'cust-002',
    productId: productsStore[0]?.id ?? 'mock-product-001',
    productTitle: productsStore[0]?.title ?? 'Vintage Cabinet',
    productSlug: productsStore[0]?.slug ?? null,
    message: 'Do you offer delivery to Wellington?',
    inquiryStatus: 'Failed',
    createdAt: isoDaysAgo(2),
  },
];

function wait(delay = 250): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, delay));
}

export async function getMockCategories(): Promise<Category[]> {
  await wait();
  return categoriesStore;
}

export async function getMockProductById(id: string): Promise<Product | null> {
  await wait();
  return productsStore.find((item) => item.id === id && item.status !== 'OffShelf') ?? null;
}

export async function getMockProductBySlug(slug: string): Promise<Product | null> {
  await wait();
  return productsStore.find((item) => item.slug === slug) ?? null;
}

export async function createMockInquiry(input: CreateInquiryInput): Promise<CreateInquiryResponse> {
  await wait();
  inquiryStore.push(input);

  const normalizedEmail = input.email?.trim().toLowerCase() || null;
  const normalizedPhone = input.phoneNumber?.trim() || null;
  const normalizedName = input.customerName?.trim() || null;

  let customer = customersStore.find((item) =>
    (normalizedEmail && item.email?.toLowerCase() === normalizedEmail)
    || (normalizedPhone && item.phoneNumber === normalizedPhone));

  if (!customer) {
    customer = {
      id: crypto.randomUUID(),
      name: normalizedName,
      email: normalizedEmail,
      phoneNumber: normalizedPhone,
      status: 'New',
      notes: null,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };
    customersStore = [customer, ...customersStore];
  } else {
    const updatedCustomer: MockCustomer = {
      ...customer,
      name: normalizedName ?? customer.name,
      email: normalizedEmail ?? customer.email,
      phoneNumber: normalizedPhone ?? customer.phoneNumber,
      updatedAt: new Date().toISOString(),
    };
    customer = updatedCustomer;
    customersStore = customersStore.map((item) => (item.id === updatedCustomer.id ? updatedCustomer : item));
  }

  const matchedProduct = productsStore.find((item) => item.id === input.productId);
  const inquiryId = crypto.randomUUID();
  customerInquiriesStore = [
    {
      inquiryId,
      customerId: customer.id,
      productId: input.productId,
      productTitle: matchedProduct?.title ?? null,
      productSlug: matchedProduct?.slug ?? null,
      message: input.message,
      inquiryStatus: 'Pending',
      createdAt: new Date().toISOString(),
    },
    ...customerInquiriesStore,
  ];

  return { inquiryId };
}

export async function createMockProduct(input: CreateProductInput): Promise<{ id: string }> {
  await wait();
  const matchedCategory = categoriesStore.find((item) => item.id === input.categoryId);
  const createdProduct: Product = {
    id: crypto.randomUUID(),
    title: input.title,
    slug: input.slug,
    description: input.description,
    price: input.price,
    status: 'Available',
    categoryId: input.categoryId,
    categoryName: matchedCategory?.name,
    images: [],
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  };

  productsStore = [createdProduct, ...productsStore];
  featuredStateStore[createdProduct.id] = { isFeatured: false, featuredSortOrder: null };
  return { id: createdProduct.id };
}

function getFeaturedState(productId: string) {
  return featuredStateStore[productId] ?? { isFeatured: false, featuredSortOrder: null };
}

export async function getMockProductsForAdmin(
  params: { status?: ProductStatus; categoryId?: string; isFeatured?: boolean; page?: number; pageSize?: number } = {},
): Promise<PagedResult<{
  id: string; title: string; slug: string; price: number; status: ProductStatus;
  categoryName?: string; imageCount: number; primaryImageUrl?: string;
  isFeatured: boolean; featuredSortOrder: number | null; createdAt: string; updatedAt: string;
}>> {
  await wait();
  let filtered = params.status
    ? productsStore.filter((item) => item.status === params.status)
    : productsStore;

  if (params.categoryId) {
    filtered = filtered.filter((item) => item.categoryId === params.categoryId);
  }

  if (typeof params.isFeatured === 'boolean') {
    filtered = filtered.filter((item) => getFeaturedState(item.id).isFeatured === params.isFeatured);
  }

  const page = Math.max(1, params.page ?? 1);
  const pageSize = Math.min(100, Math.max(1, params.pageSize ?? 50));
  const totalCount = filtered.length;

  const items = filtered
    .slice((page - 1) * pageSize, page * pageSize)
    .map((item) => ({
      ...getFeaturedState(item.id),
      id: item.id,
      title: item.title,
      slug: item.slug,
      price: item.price,
      status: item.status,
      categoryName: item.categoryName,
      imageCount: item.images.length,
      primaryImageUrl: item.images.find((img) => img.isPrimary)?.displayUrl ?? item.images[0]?.displayUrl,
      createdAt: item.createdAt,
      updatedAt: item.updatedAt,
    }));

  return buildPagedResult(items, page, pageSize, totalCount);
}

export async function updateMockProductStatus(
  productId: string,
  status: ProductStatus,
): Promise<void> {
  await wait();
  const shouldClearFeatured = status !== 'Available';

  productsStore = productsStore.map((item) =>
    item.id === productId
      ? {
          ...item,
          status,
          updatedAt: new Date().toISOString(),
        }
      : item,
  );

  if (shouldClearFeatured) {
    featuredStateStore[productId] = {
      isFeatured: false,
      featuredSortOrder: null,
    };
  }
}

export async function updateMockProductFeatured(
  productId: string,
  isFeatured: boolean,
  featuredSortOrder: number | null,
): Promise<void> {
  await wait();

  const product = productsStore.find((item) => item.id === productId);
  if (!product) {
    throw new Error('Product was not found.');
  }

  if (isFeatured && product.status !== 'Available') {
    throw new Error('Only available products can be featured.');
  }

  const normalizedSortOrder = isFeatured ? featuredSortOrder : null;
  if (
    normalizedSortOrder !== null &&
    (normalizedSortOrder < FEATURED_SORT_ORDER_MIN || normalizedSortOrder > FEATURED_SORT_ORDER_MAX)
  ) {
    throw new Error(
      `Featured sort order must be between ${FEATURED_SORT_ORDER_MIN} and ${FEATURED_SORT_ORDER_MAX}. ` +
      'Smaller values appear earlier.',
    );
  }

  featuredStateStore[productId] = {
    isFeatured,
    featuredSortOrder: normalizedSortOrder,
  };

  productsStore = productsStore.map((item) =>
    item.id === productId
      ? {
          ...item,
          updatedAt: new Date().toISOString(),
        }
      : item,
  );
}

function toCustomerListItem(customer: MockCustomer): CustomerListItem {
  const inquiries = customerInquiriesStore.filter((item) => item.customerId === customer.id);
  const lastInquiryAt = inquiries
    .map((item) => item.createdAt)
    .sort((left, right) => new Date(right).getTime() - new Date(left).getTime())[0] ?? null;

  return {
    id: customer.id,
    name: customer.name,
    email: customer.email,
    phone: customer.phoneNumber,
    status: customer.status,
    inquiryCount: inquiries.length,
    lastInquiryAt,
    createdAt: customer.createdAt,
    updatedAt: customer.updatedAt,
  };
}

function toCustomerDetail(customer: MockCustomer): CustomerDetail {
  const summary = toCustomerListItem(customer);
  return {
    id: summary.id,
    name: summary.name,
    email: summary.email,
    phone: summary.phone,
    status: summary.status,
    notes: customer.notes,
    inquiryCount: summary.inquiryCount,
    lastInquiryAt: summary.lastInquiryAt,
    createdAt: summary.createdAt,
    updatedAt: summary.updatedAt,
  };
}

function buildPagedResult<T>(items: T[], page: number, pageSize: number, totalCount: number): PagedResult<T> {
  const totalPages = Math.ceil(totalCount / pageSize);
  return {
    items,
    page,
    pageSize,
    totalCount,
    totalPages,
    hasNextPage: page < totalPages,
    hasPreviousPage: page > 1,
    isFallback: false,
  };
}

function compareNullableDate(left: string | null, right: string | null, direction: SortDirection): number {
  if (!left && !right) {
    return 0;
  }

  if (!left) {
    return direction === 'asc' ? -1 : 1;
  }

  if (!right) {
    return direction === 'asc' ? 1 : -1;
  }

  const diff = new Date(left).getTime() - new Date(right).getTime();
  return direction === 'asc' ? diff : -diff;
}

function compareDate(left: string, right: string, direction: SortDirection): number {
  const diff = new Date(left).getTime() - new Date(right).getTime();
  return direction === 'asc' ? diff : -diff;
}

function normalizeSortBy(sortBy: CustomerSortBy | undefined): CustomerSortBy {
  if (sortBy === 'updatedAt' || sortBy === 'lastInquiryAt' || sortBy === 'createdAt') {
    return sortBy;
  }
  return 'createdAt';
}

function normalizeSortDirection(direction: SortDirection | undefined): SortDirection {
  return direction === 'asc' ? 'asc' : 'desc';
}

export async function getMockCustomersForAdmin(
  params: AdminCustomerQueryParams,
): Promise<PagedResult<CustomerListItem>> {
  await wait();

  const page = Math.max(1, params.page ?? 1);
  const pageSize = Math.min(100, Math.max(1, params.pageSize ?? 20));
  const sortBy = normalizeSortBy(params.sortBy);
  const sortDirection = normalizeSortDirection(params.sortDirection);
  const search = params.search?.trim().toLowerCase() ?? '';
  const statusFilter = params.status;

  let items = customersStore.map(toCustomerListItem);
  if (statusFilter) {
    items = items.filter((item) => item.status === statusFilter);
  }

  if (search.length > 0) {
    items = items.filter((item) =>
      (item.name ?? '').toLowerCase().includes(search)
      || (item.email ?? '').toLowerCase().includes(search)
      || (item.phone ?? '').toLowerCase().includes(search));
  }

  items.sort((left, right) => {
    const sortResult = (() => {
      switch (sortBy) {
        case 'updatedAt':
          return compareDate(left.updatedAt, right.updatedAt, sortDirection);
        case 'lastInquiryAt':
          return compareNullableDate(left.lastInquiryAt, right.lastInquiryAt, sortDirection);
        case 'createdAt':
        default:
          return compareDate(left.createdAt, right.createdAt, sortDirection);
      }
    })();

    if (sortResult !== 0) {
      return sortResult;
    }

    return left.id.localeCompare(right.id);
  });

  const totalCount = items.length;
  const pagedItems = items.slice((page - 1) * pageSize, page * pageSize);
  return buildPagedResult(pagedItems, page, pageSize, totalCount);
}

export async function getMockCustomerDetailForAdmin(customerId: string): Promise<CustomerDetail | null> {
  await wait();
  const customer = customersStore.find((item) => item.id === customerId);
  return customer ? toCustomerDetail(customer) : null;
}

export async function getMockCustomerInquiriesForAdmin(
  customerId: string,
  params: CustomerInquiryQueryParams,
): Promise<PagedResult<CustomerInquiryItem>> {
  await wait();

  const customer = customersStore.find((item) => item.id === customerId);
  if (!customer) {
    throw new Error('Customer was not found.');
  }

  const page = Math.max(1, params.page ?? 1);
  const pageSize = Math.min(100, Math.max(1, params.pageSize ?? 20));

  const inquiries = customerInquiriesStore
    .filter((item) => item.customerId === customerId)
    .sort((left, right) => {
      const createdDiff = new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime();
      if (createdDiff !== 0) {
        return createdDiff;
      }

      return right.inquiryId.localeCompare(left.inquiryId);
    })
    .map((item) => ({
      inquiryId: item.inquiryId,
      productId: item.productId,
      productTitle: item.productTitle,
      productSlug: item.productSlug,
      message: item.message,
      inquiryStatus: item.inquiryStatus,
      createdAt: item.createdAt,
    }));

  const totalCount = inquiries.length;
  const pagedItems = inquiries.slice((page - 1) * pageSize, page * pageSize);
  return buildPagedResult(pagedItems, page, pageSize, totalCount);
}

export async function updateMockCustomerForAdmin(
  customerId: string,
  input: UpdateCustomerInput,
): Promise<void> {
  await wait();

  const customerIndex = customersStore.findIndex((item) => item.id === customerId);
  if (customerIndex < 0) {
    throw new Error('Customer was not found.');
  }

  const normalizedPhone = input.phoneNumber?.trim() || null;
  if (normalizedPhone && !/^[0-9+\-\s()]+$/.test(normalizedPhone)) {
    throw new Error('Phone number can only contain digits, +, -, spaces, and parentheses.');
  }

  const duplicatedPhone = normalizedPhone
    ? customersStore.some((item) => item.id !== customerId && item.phoneNumber === normalizedPhone)
    : false;
  if (duplicatedPhone) {
    throw new Error('The phone number is already used by another customer.');
  }

  const current = customersStore[customerIndex];
  const status = input.status ?? current.status;
  const notes = input.notes === undefined
    ? current.notes
    : (input.notes.trim().length === 0 ? null : input.notes.trim());

  if (!customerStatusOptions.includes(status)) {
    throw new Error(`Unsupported customer status '${status}'.`);
  }

  const normalizedName = input.name !== undefined ? (input.name?.trim() || null) : current.name;
  const targetPhone = normalizedPhone ?? current.phoneNumber;
  const normalizedEmail = current.email?.trim() || null;
  if (!normalizedEmail && !targetPhone) {
    throw new Error('At least one contact method (email or phone) is required.');
  }

  customersStore = customersStore.map((item) =>
    item.id === customerId
      ? {
          ...item,
          name: normalizedName,
          phoneNumber: targetPhone,
          status,
          notes,
          updatedAt: new Date().toISOString(),
        }
      : item,
  );
}

export async function getMockFeaturedProducts(limit = 8): Promise<ProductListItem[]> {
  await wait();

  const safeLimit = Math.max(1, limit);
  const featuredProducts = productsStore
    .filter((item) => item.status === 'Available')
    .filter((item) => getFeaturedState(item.id).isFeatured)
    .sort((left, right) => {
      const leftState = getFeaturedState(left.id);
      const rightState = getFeaturedState(right.id);

      const leftHasSortOrder = leftState.featuredSortOrder !== null;
      const rightHasSortOrder = rightState.featuredSortOrder !== null;
      if (leftHasSortOrder !== rightHasSortOrder) {
        return leftHasSortOrder ? -1 : 1;
      }

      if (leftState.featuredSortOrder !== rightState.featuredSortOrder) {
        return (leftState.featuredSortOrder ?? 0) - (rightState.featuredSortOrder ?? 0);
      }

      const createdAtCompare =
        new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime();
      if (createdAtCompare !== 0) {
        return createdAtCompare;
      }

      return left.id.localeCompare(right.id);
    })
    .slice(0, safeLimit);

  return featuredProducts.map((item) => ({
    id: item.id,
    title: item.title,
    slug: item.slug,
    price: item.price,
    coverImageUrl: item.images.find((img) => img.isPrimary)?.displayUrl ?? item.images[0]?.displayUrl ?? null,
    categoryName: item.categoryName ?? null,
    status: item.status,
    createdAt: item.createdAt,
  }));
}
