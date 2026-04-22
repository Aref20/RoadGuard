export const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080/api';

export function getAuthToken(): string | null {
  if (typeof window !== 'undefined') {
    return localStorage.getItem('admin_jwt_token');
  }
  return null;
}

export function setAuthToken(token: string) {
  if (typeof window !== 'undefined') {
    localStorage.setItem('admin_jwt_token', token);
  }
}

export function removeAuthToken() {
  if (typeof window !== 'undefined') {
    localStorage.removeItem('admin_jwt_token');
  }
}

async function fetchWithAuth(endpoint: string, options: RequestInit = {}) {
  const token = getAuthToken();
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string> || {})
  };

  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    ...options,
    headers
  });

  if (response.status === 401) {
    removeAuthToken();
    window.location.href = '/login';
    throw new Error('Unauthorized');
  }

  return response;
}

export const api = {
  getHealth: async () => {
    const res = await fetchWithAuth('/admin/health-overview');
    if (!res.ok) throw new Error('Failed to fetch health');
    return res.json();
  },
  getUsers: async () => {
    const res = await fetchWithAuth('/admin/users');
    if (!res.ok) throw new Error('Failed to fetch users');
    return res.json();
  },
  getSessions: async () => {
    const res = await fetchWithAuth('/admin/sessions');
    if (!res.ok) throw new Error('Failed to fetch sessions');
    return res.json();
  },
  getProviderSettings: async () => {
    const res = await fetchWithAuth('/admin/provider-settings');
    if (!res.ok) throw new Error('Failed to fetch provider settings');
    return res.json();
  },
  updateProviderSettings: async (payload: any) => {
    const res = await fetchWithAuth('/admin/provider-settings', {
      method: 'PUT',
      body: JSON.stringify(payload)
    });
    if (!res.ok) throw new Error('Failed to update provider settings');
    return res.json();
  }
};
