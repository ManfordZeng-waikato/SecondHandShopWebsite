import { act, render, screen } from '@testing-library/react';

function mockFetchResponse(body: unknown, init: ResponseInit = {}) {
  return Promise.resolve(
    new Response(JSON.stringify(body), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
      ...init,
    }),
  );
}

describe('adminAuth', () => {
  beforeEach(() => {
    vi.resetModules();
    sessionStorage.clear();
    vi.stubGlobal('fetch', vi.fn());
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('initializeAdminAuth loads the current admin from the server', async () => {
    vi.mocked(fetch).mockResolvedValue(
      await mockFetchResponse({
        isAuthenticated: true,
        userId: 'admin-1',
        userName: 'lord',
        email: 'lord@example.test',
        role: 'Admin',
        mustChangePassword: false,
      }),
    );
    const { getAdminAuthSnapshot, initializeAdminAuth } = await import('../adminAuth');

    await initializeAdminAuth();

    expect(fetch).toHaveBeenCalledWith('https://localhost:7266/api/lord/auth/me', {
      method: 'GET',
      credentials: 'include',
    });
    expect(getAdminAuthSnapshot()).toEqual({
      isAuthInitialized: true,
      isAuthenticated: true,
      mustChangePassword: false,
      currentUser: {
        userId: 'admin-1',
        userName: 'lord',
        email: 'lord@example.test',
        role: 'Admin',
      },
    });
  });

  it('persistSessionAfterLogin stores the session expiry hint', async () => {
    const { persistSessionAfterLogin } = await import('../adminAuth');

    persistSessionAfterLogin('2026-04-25T01:00:00Z');

    expect(JSON.parse(sessionStorage.getItem('shs.admin.sessionHint') ?? '{}')).toEqual({
      expiresAt: '2026-04-25T01:00:00Z',
    });
  });

  it('revokeLordCookie posts to the logout endpoint and ignores failures', async () => {
    vi.mocked(fetch).mockRejectedValueOnce(new Error('network down'));
    const { revokeLordCookie } = await import('../adminAuth');

    await expect(revokeLordCookie()).resolves.toBeUndefined();

    expect(fetch).toHaveBeenCalledWith('https://localhost:7266/api/lord/auth/logout', {
      method: 'POST',
      credentials: 'include',
    });
  });

  it('useAdminAuth subscribes to auth snapshot changes', async () => {
    vi.mocked(fetch).mockResolvedValue(
      await mockFetchResponse({
        isAuthenticated: true,
        userId: 'admin-1',
        userName: 'lord',
        email: 'lord@example.test',
        role: 'Admin',
        mustChangePassword: false,
      }),
    );
    const { initializeAdminAuth, useAdminAuth } = await import('../adminAuth');

    function Probe() {
      const auth = useAdminAuth();
      return <div>{auth.currentUser?.userName ?? (auth.isAuthInitialized ? 'logged out' : 'loading')}</div>;
    }

    render(<Probe />);
    expect(screen.getByText('loading')).toBeInTheDocument();

    await act(async () => {
      await initializeAdminAuth();
    });

    expect(await screen.findByText('lord')).toBeInTheDocument();
  });
});
