import type { CreateInquiryInput, CreateInquiryResponse } from '../../../entities/inquiry/types';
import { httpClient } from '../../../shared/api/httpClient';

import './createInquiryResponse.contract';

export async function createInquiry(input: CreateInquiryInput): Promise<CreateInquiryResponse> {
  const response = await httpClient.post<CreateInquiryResponse>('/api/inquiries', input);
  return response.data;
}
