const adminSessionKey = 'shs.admin.logged_in';

export function isAdminLoggedIn(): boolean {
  return sessionStorage.getItem(adminSessionKey) === 'true';
}

export function loginAsAdmin(): void {
  sessionStorage.setItem(adminSessionKey, 'true');
}

export function logoutAdmin(): void {
  sessionStorage.removeItem(adminSessionKey);
}
