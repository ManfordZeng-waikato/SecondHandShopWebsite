import type { CreateInquiryInput } from '../../../entities/inquiry/types';
import { httpClient } from '../../../shared/api/httpClient';
import { env } from '../../../shared/config/env';
import { createMockInquiry } from '../../../shared/mock/mockApi';

export async function createInquiry(input: CreateInquiryInput): Promise<{ id: string }> {
  if (env.useMockApi) {
    return createMockInquiry(input);
  }

  const response = await httpClient.post<{ id: string }>('/api/inquiries', input);
  return response.data;
}
