import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AxiosError } from 'axios';
import { AdminLoginPage } from '../AdminLoginPage';
import { renderWithProviders } from '../../test/renderWithProviders';
import { loginAdmin } from '../../features/admin/api/adminApi';
import {
  getAdminAuthSnapshot,
  initializeAdminAuth,
  persistSessionAfterLogin,
  revokeLordCookie,
  useAdminAuth,
} from '../../features/admin/auth/adminAuth';

const mockNavigate = vi.fn();
const mockLocation = { state: null } as const;

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
    useLocation: () => mockLocation,
  };
});

vi.mock('../../features/admin/api/adminApi', () => ({
  loginAdmin: vi.fn(),
}));

vi.mock('../../features/admin/auth/adminAuth', () => ({
  getAdminAuthSnapshot: vi.fn(),
  initializeAdminAuth: vi.fn(),
  persistSessionAfterLogin: vi.fn(),
  revokeLordCookie: vi.fn(),
  useAdminAuth: vi.fn(),
}));

describe('AdminLoginPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useAdminAuth).mockReturnValue({
      isAuthInitialized: true,
      isAuthenticated: false,
      mustChangePassword: false,
      currentUser: null,
    });
    vi.mocked(getAdminAuthSnapshot).mockReturnValue({
      isAuthInitialized: true,
      isAuthenticated: true,
      mustChangePassword: false,
      currentUser: null,
    });
    vi.mocked(revokeLordCookie).mockResolvedValue();
    vi.mocked(initializeAdminAuth).mockResolvedValue();
  });

  it('submits credentials and navigates to the product workspace after a successful login', async () => {
    vi.mocked(loginAdmin).mockResolvedValue({
      expiresAt: '2026-04-17T01:00:00Z',
      requiresPasswordChange: false,
    });

    renderWithProviders(<AdminLoginPage />);

    await userEvent.type(screen.getByLabelText(/username/i), 'lord');
    await userEvent.type(screen.getByLabelText(/password/i), 'correct-password');
    await userEvent.click(screen.getByRole('button', { name: /sign in/i }));

    await waitFor(() => {
      expect(loginAdmin).toHaveBeenCalledWith('lord', 'correct-password');
    });
    expect(persistSessionAfterLogin).toHaveBeenCalledWith('2026-04-17T01:00:00Z');
    expect(initializeAdminAuth).toHaveBeenCalled();
    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith('/lord/products', { replace: true });
    });
  });

  it('shows an authentication error when the server rejects the credentials', async () => {
    vi.mocked(loginAdmin).mockRejectedValue(
      new AxiosError('Unauthorized', 'ERR_BAD_REQUEST', undefined, undefined, {
        status: 401,
        statusText: 'Unauthorized',
        headers: {},
        config: {} as never,
        data: {},
      }),
    );

    renderWithProviders(<AdminLoginPage />);

    await userEvent.type(screen.getByLabelText(/username/i), 'lord');
    await userEvent.type(screen.getByLabelText(/password/i), 'wrong-password');
    await userEvent.click(screen.getByRole('button', { name: /sign in/i }));

    expect(await screen.findByText(/invalid username or password/i)).toBeInTheDocument();
  });
});
