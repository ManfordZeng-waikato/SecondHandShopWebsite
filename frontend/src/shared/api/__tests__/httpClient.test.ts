import { AxiosError, type AxiosAdapter, type AxiosResponse } from 'axios';
import { waitFor } from '@testing-library/react';
import { httpClient } from '../httpClient';
import {
  clearAuth,
  revokeLordCookie,
  setSessionHintExpiresAt,
} from '../../../features/admin/auth/adminAuth';
import { notifyAdminUnauthorized } from '../../../features/admin/auth/adminUnauthorizedNavigation';

vi.mock('../../../features/admin/auth/adminAuth', () => ({
  clearAuth: vi.fn(),
  revokeLordCookie: vi.fn().mockResolvedValue(undefined),
  setSessionHintExpiresAt: vi.fn(),
}));

vi.mock('../../../features/admin/auth/adminUnauthorizedNavigation', () => ({
  notifyAdminUnauthorized: vi.fn(),
}));

function okAdapter(headers: AxiosResponse['headers'] = {}): AxiosAdapter {
  return async (config) => ({
    data: { ok: true },
    status: 200,
    statusText: 'OK',
    headers,
    config,
  });
}

function failingAdapter(status: number): AxiosAdapter {
  return async (config) => {
    throw new AxiosError('Request failed', AxiosError.ERR_BAD_RESPONSE, config, undefined, {
      data: { error: 'failed' },
      status,
      statusText: 'Error',
      headers: {},
      config,
    });
  };
}

describe('httpClient', () => {
  beforeEach(() => {
    vi.mocked(revokeLordCookie).mockResolvedValue(undefined);
  });

  it('redirects to admin login after a protected lord route returns 401', async () => {
    await expect(
      httpClient.get('/api/lord/products', { adapter: failingAdapter(401) }),
    ).rejects.toMatchObject({ response: { status: 401 } });

    expect(clearAuth).toHaveBeenCalledOnce();
    expect(revokeLordCookie).toHaveBeenCalledOnce();
    await waitFor(() => {
      expect(notifyAdminUnauthorized).toHaveBeenCalledOnce();
    });
  });

  it('does not redirect when a public route returns 401', async () => {
    await expect(
      httpClient.get('/api/products/private-preview', { adapter: failingAdapter(401) }),
    ).rejects.toMatchObject({ response: { status: 401 } });

    expect(clearAuth).not.toHaveBeenCalled();
    expect(revokeLordCookie).not.toHaveBeenCalled();
    expect(notifyAdminUnauthorized).not.toHaveBeenCalled();
  });

  it('does not redirect for expected admin bootstrap 401 responses', async () => {
    await expect(
      httpClient.get('/api/lord/auth/me', { adapter: failingAdapter(401) }),
    ).rejects.toMatchObject({ response: { status: 401 } });

    expect(clearAuth).not.toHaveBeenCalled();
    expect(revokeLordCookie).not.toHaveBeenCalled();
    expect(notifyAdminUnauthorized).not.toHaveBeenCalled();
  });

  it('propagates non-401 errors without clearing admin auth', async () => {
    await expect(
      httpClient.get('/api/lord/products', { adapter: failingAdapter(500) }),
    ).rejects.toMatchObject({ response: { status: 500 } });

    expect(clearAuth).not.toHaveBeenCalled();
    expect(revokeLordCookie).not.toHaveBeenCalled();
    expect(notifyAdminUnauthorized).not.toHaveBeenCalled();
  });

  it('persists the sliding session expiry header from successful responses', async () => {
    await httpClient.get('/api/lord/products', {
      adapter: okAdapter({ 'x-admin-session-expires-at': '2026-04-25T01:00:00Z' }),
    });

    expect(setSessionHintExpiresAt).toHaveBeenCalledWith('2026-04-25T01:00:00Z');
  });
});
