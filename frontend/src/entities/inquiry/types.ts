export interface CreateInquiryInput {
  productId: string;
  customerName?: string;
  email?: string;
  phoneNumber?: string;
  message: string;
  turnstileToken: string;
}

/** Matches WebApi `CreateInquiryResponse` (camelCase JSON: inquiryId). */
export interface CreateInquiryResponse {
  inquiryId: string;
}
