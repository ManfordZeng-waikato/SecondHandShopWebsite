/**
 * Bridges axios 401 handling to React Router without full page reload.
 * {@link AdminAuthProvider} registers {@link setAdminUnauthorizedNavigator} on mount.
 */
let navigateToLordLogin: (() => void) | null = null;

export function setAdminUnauthorizedNavigator(handler: (() => void) | null): void {
  navigateToLordLogin = handler;
}

/** Call after clearing client auth / revoking cookie so the UI moves to login inside the SPA. */
export function notifyAdminUnauthorized(): void {
  if (navigateToLordLogin) {
    navigateToLordLogin();
    return;
  }

  window.location.assign('/lord/login');
}
