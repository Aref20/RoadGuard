export const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080/api';
export const API_ORIGIN = API_BASE_URL.endsWith('/api')
  ? API_BASE_URL.slice(0, -4)
  : API_BASE_URL;

export type ProviderConfig = {
  providerKey: string;
  displayName: string;
  isEnabled: boolean;
  isSelected: boolean;
  priorityOrder: number;
  isConfigured: boolean;
  healthStatus: string;
  lastFailureReason?: string | null;
  lastSuccessAt?: string | null;
  lastFailureAt?: string | null;
  updatedAt: string;
};

export type SystemHealth = {
  totalUsers: number;
  totalSessions: number;
  activeSessions: number;
  autoStartedSessions: number;
  totalViolations: number;
  totalAlerts: number;
  databaseStatus: string;
  selectedProvider?: string | null;
  providerHealth?: string | null;
  serverTime: string;
};

export type AdminUser = {
  id: string;
  email: string;
  isActive: boolean;
  createdAt: string;
  role: string;
};

export type AdminSession = {
  id: string;
  userId: string;
  startedAt: string;
  endedAt?: string | null;
  status: string;
  wasAutoStarted: boolean;
  sessionStartReason: string;
  sessionEndReason: string;
  overspeedEventCount: number;
  alertEventCount: number;
  totalDistanceMeters: number;
  averageSpeedKph: number;
  maxSpeedKph: number;
};

export type LoginResponse = {
  token: string;
  role: string;
  email: string;
};

export class ApiError extends Error {
  readonly status: number;
  readonly code?: string;

  constructor(message: string, status: number, code?: string) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.code = code;
  }
}

type JwtClaims = {
  role?: string;
  [key: string]: unknown;
};

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

export function getTokenClaims(token: string): JwtClaims | null {
  try {
    const payload = token.split('.')[1];
    if (!payload) {
      return null;
    }

    const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
    const decoded = typeof window !== 'undefined'
      ? window.atob(normalized)
      : Buffer.from(normalized, 'base64').toString('utf8');

    return JSON.parse(decoded) as JwtClaims;
  } catch {
    return null;
  }
}

async function parseError(response: Response): Promise<ApiError> {
  let message = 'Request failed';
  let code: string | undefined;

  try {
    const payload = await response.json();
    message = payload.message || payload.title || payload.code || message;
    code = payload.code;
  } catch {
    message = response.statusText || message;
  }

  return new ApiError(message, response.status, code);
}

async function fetchWithAuth(endpoint: string, options: RequestInit = {}) {
  const token = getAuthToken();
  const headers: Record<string, string> = {
    ...(options.body ? { 'Content-Type': 'application/json' } : {}),
    ...((options.headers as Record<string, string>) || {}),
  };

  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    ...options,
    headers,
  });

  if (!response.ok) {
    const error = await parseError(response);
    if (response.status === 401 || (response.status === 403 && ['AUTH_FORBIDDEN', 'AUTH_ACCOUNT_DISABLED'].includes(error.code || ''))) {
      removeAuthToken();
    }

    throw error;
  }

  return response;
}

export const api = {
  async login(email: string, password: string): Promise<LoginResponse> {
    const response = await fetch(`${API_BASE_URL}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
    });

    if (!response.ok) {
      throw await parseError(response);
    }

    return response.json();
  },

  async getHealth(): Promise<SystemHealth> {
    const response = await fetchWithAuth('/admin/health-overview');
    return response.json();
  },

  async getUsers(): Promise<AdminUser[]> {
    const response = await fetchWithAuth('/admin/users');
    return response.json();
  },

  async getSessions(): Promise<AdminSession[]> {
    const response = await fetchWithAuth('/admin/sessions');
    return response.json();
  },

  async getProviderSettings(): Promise<ProviderConfig[]> {
    const response = await fetchWithAuth('/admin/provider-settings');
    return response.json();
  },

  async getProviderHealth(): Promise<ProviderConfig[]> {
    const response = await fetchWithAuth('/admin/provider-health');
    return response.json();
  },

  async updateProviderSettings(payload: ProviderConfig[]) {
    const response = await fetchWithAuth('/admin/provider-settings', {
      method: 'PUT',
      body: JSON.stringify(
        payload.map((provider) => ({
          providerKey: provider.providerKey,
          isEnabled: provider.isEnabled,
          isSelected: provider.isSelected,
          priorityOrder: provider.priorityOrder,
        })),
      ),
    });

    return response.json();
  },

  async createUser(payload: { email: string; password: string }) {
    const response = await fetchWithAuth('/admin/users', {
      method: 'POST',
      body: JSON.stringify(payload),
    });

    return response.json();
  },

  async updateUserStatus(id: string, isActive: boolean) {
    const response = await fetchWithAuth(`/admin/users/${id}/status`, {
      method: 'PUT',
      body: JSON.stringify({ isActive }),
    });

    return response.json();
  },

  async resetUserPassword(id: string, password: string) {
    const response = await fetchWithAuth(`/admin/users/${id}/reset-password`, {
      method: 'PUT',
      body: JSON.stringify({ password }),
    });

    return response.json();
  },
};
