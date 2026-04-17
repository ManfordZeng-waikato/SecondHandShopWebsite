import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AxiosError } from 'axios';
import { AdminChangePasswordPage } from '../AdminChangePasswordPage';
import { renderWithProviders } from '../../test/renderWithProviders';
import { changeAdminInitialPassword } from '../../features/admin/api/adminApi';
import { clearAuth } from '../../features/admin/auth/adminAuth';

const mockNavigate = vi.fn();

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

vi.mock('../../features/admin/api/adminApi', async () => {
  const actual = await vi.importActual<typeof import('../../features/admin/api/adminApi')>('../../features/admin/api/adminApi');
  return {
    ...actual,
    changeAdminInitialPassword: vi.fn(),
  };
});

vi.mock('../../features/admin/auth/adminAuth', async () => {
  const actual = await vi.importActual<typeof import('../../features/admin/auth/adminAuth')>('../../features/admin/auth/adminAuth');
  return {
    ...actual,
    clearAuth: vi.fn(),
  };
});

describe('AdminChangePasswordPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows a validation error when any field is missing', async () => {
    renderWithProviders(<AdminChangePasswordPage />);

    await userEvent.click(screen.getByRole('button', { name: /update password/i }));

    expect(await screen.findByText(/all fields are required/i)).toBeInTheDocument();
  });

  it('clears auth and redirects to login after a successful password change', async () => {
    vi.mocked(changeAdminInitialPassword).mockResolvedValue({
      success: true,
      requiresReLogin: true,
      message: 'Password changed successfully',
    });

    renderWithProviders(<AdminChangePasswordPage />);

    await userEvent.type(screen.getByLabelText(/current password/i), 'oldpass1');
    await userEvent.type(screen.getByLabelText(/^new password$/i), 'newpass1');
    await userEvent.type(screen.getByLabelText(/confirm new password/i), 'newpass1');
    await userEvent.click(screen.getByRole('button', { name: /update password/i }));

    await waitFor(() => {
      expect(changeAdminInitialPassword).toHaveBeenCalledWith({
        currentPassword: 'oldpass1',
        newPassword: 'newpass1',
        confirmNewPassword: 'newpass1',
      });
    });
    expect(clearAuth).toHaveBeenCalled();
    expect(mockNavigate).toHaveBeenCalledWith('/lord/login', {
      replace: true,
      state: { passwordChanged: true },
    });
  });

  it('shows the API message when the server rejects the new password', async () => {
    vi.mocked(changeAdminInitialPassword).mockRejectedValue(
      new AxiosError('Bad Request', 'ERR_BAD_REQUEST', undefined, undefined, {
        status: 400,
        statusText: 'Bad Request',
        headers: {},
        config: {} as never,
        data: { message: 'Password must contain at least one letter and one digit.' },
      }),
    );

    renderWithProviders(<AdminChangePasswordPage />);

    await userEvent.type(screen.getByLabelText(/current password/i), 'oldpass1');
    await userEvent.type(screen.getByLabelText(/^new password$/i), 'short');
    await userEvent.type(screen.getByLabelText(/confirm new password/i), 'short');
    await userEvent.click(screen.getByRole('button', { name: /update password/i }));

    expect(await screen.findByText(/password must contain at least one letter and one digit/i)).toBeInTheDocument();
  });
});
