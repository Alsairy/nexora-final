const API_BASE_URL = (import.meta as any).env?.VITE_API_BASE_URL || 'http://localhost:5000';
const API_VERSION = 'v1';
const API_AUTH_USER = (import.meta as any).env?.VITE_API_AUTH_USER;
const API_AUTH_PASSWORD = (import.meta as any).env?.VITE_API_AUTH_PASSWORD;

export const getApiUrl = (endpoint: string): string => {
  let cleanBaseUrl = API_BASE_URL;
  
  if (endpoint.startsWith('/api/')) {
    const versionedEndpoint = endpoint.replace('/api/', `/api/${API_VERSION}/`);
    return `${cleanBaseUrl}${versionedEndpoint}`;
  }
  if (endpoint.startsWith('/')) {
    return `${cleanBaseUrl}${endpoint}`;
  }
  return endpoint;
};

export const getAuthHeaders = (useBasicAuth: boolean = false): Record<string, string> => {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json'
  };
  
  if (useBasicAuth && API_AUTH_USER && API_AUTH_PASSWORD) {
    const basicAuth = btoa(`${API_AUTH_USER}:${API_AUTH_PASSWORD}`);
    headers['Authorization'] = `Basic ${basicAuth}`;
  } else {
    const token = localStorage.getItem('authToken');
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    } else if (API_AUTH_USER && API_AUTH_PASSWORD) {
      const basicAuth = btoa(`${API_AUTH_USER}:${API_AUTH_PASSWORD}`);
      headers['Authorization'] = `Basic ${basicAuth}`;
    }
  }
  
  return headers;
};

export default {
  baseUrl: API_BASE_URL,
  version: API_VERSION,
  getUrl: getApiUrl,
  getAuthHeaders: getAuthHeaders,
};
