const API_BASE_URL = (import.meta as any).env?.VITE_API_BASE_URL || '';

export const apiConfig = {
  baseUrl: API_BASE_URL,
  endpoints: {
    auth: {
      login: '/api/auth/login',
      register: '/api/auth/register',
      forgotPassword: '/api/auth/forgot-password',
    },
  },
  useMockData: !API_BASE_URL || API_BASE_URL === '',
};

export const mockResponses = {
  login: {
    success: {
      token: 'mock-jwt-token-12345',
      user: {
        id: 1,
        email: 'demo@nexora.com',
        firstName: 'Demo',
        lastName: 'User',
        role: 'Admin',
        tenantId: 1,
      },
    },
    error: { message: 'Invalid credentials' },
  },
  register: {
    success: { message: 'Registration successful' },
    error: { message: 'Email already exists' },
  },
  forgotPassword: {
    success: { message: 'Password reset instructions sent' },
    error: { message: 'Email not found' },
  },
};

export const makeApiCall = async (endpoint: string, options: RequestInit = {}) => {
  if (apiConfig.useMockData) {
    return new Promise((resolve, reject) => {
      setTimeout(() => {
        if (endpoint.includes('login')) {
          const body = JSON.parse(options.body as string);
          if (body.email === 'demo@nexora.com' && body.password === 'demo123') {
            resolve({ ok: true, json: () => Promise.resolve(mockResponses.login.success) });
          } else {
            reject(new Error(mockResponses.login.error.message));
          }
        } else if (endpoint.includes('register')) {
          resolve({ ok: true, json: () => Promise.resolve(mockResponses.register.success) });
        } else if (endpoint.includes('forgot-password')) {
          resolve({ ok: true, json: () => Promise.resolve(mockResponses.forgotPassword.success) });
        } else {
          reject(new Error('Endpoint not found'));
        }
      }, 1000);
    });
  } else {
    return fetch(`${apiConfig.baseUrl}${endpoint}`, options);
  }
};
