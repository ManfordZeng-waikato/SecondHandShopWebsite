export interface CreateInquiryInput {
  productId: string;
  customerName?: string;
  email?: string;
  phoneNumber?: string;
  message: string;
}
