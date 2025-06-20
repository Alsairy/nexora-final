const API_BASE_URL = (import.meta as any).env?.VITE_API_BASE_URL || 'https://user:d6ac29f52eda7da9ac932c93506ddcde@code-review-app-tunnel-mjvxteqa.devinapps.com';
const API_VERSION = 'v1';

export const getApiUrl = (endpoint: string): string => {
  if (endpoint.startsWith('/api/')) {
    const versionedEndpoint = endpoint.replace('/api/', `/api/${API_VERSION}/`);
    return `${API_BASE_URL}${versionedEndpoint}`;
  }
  if (endpoint.startsWith('/')) {
    return `${API_BASE_URL}${endpoint}`;
  }
  return endpoint;
};

export const getAuthHeaders = (): Record<string, string> => {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json'
  };
  
  const basicAuth = btoa('user:d6ac29f52eda7da9ac932c93506ddcde');
  headers['Authorization'] = `Basic ${basicAuth}`;
  
  return headers;
};

export default {
  baseUrl: API_BASE_URL,
  version: API_VERSION,
  getUrl: getApiUrl,
  getAuthHeaders: getAuthHeaders,
};
