import type { CreateInquiryInput } from '../../../entities/inquiry/types';
import { httpClient } from '../../../shared/api/httpClient';

export async function createInquiry(input: CreateInquiryInput): Promise<{ id: string }> {
  const response = await httpClient.post<{ id: string }>('/api/inquiries', input);
  return response.data;
}
