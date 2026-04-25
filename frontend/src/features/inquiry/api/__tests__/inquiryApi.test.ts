import { httpClient } from '../../../../shared/api/httpClient';
import { createInquiry } from '../inquiryApi';

vi.mock('../../../../shared/api/httpClient', () => ({
  httpClient: {
    post: vi.fn(),
  },
}));

describe('inquiryApi', () => {
  it('posts inquiry input and returns the created inquiry response', async () => {
    const input = {
      productId: 'product-1',
      customerName: 'Alice',
      email: 'alice@example.com',
      message: 'Is this available?',
      turnstileToken: 'turnstile-token',
    };
    vi.mocked(httpClient.post).mockResolvedValue({ data: { inquiryId: 'inquiry-1' } });

    await expect(createInquiry(input)).resolves.toEqual({ inquiryId: 'inquiry-1' });

    expect(httpClient.post).toHaveBeenCalledWith('/api/inquiries', input);
  });
});
