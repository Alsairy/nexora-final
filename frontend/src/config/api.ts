const API_BASE_URL = (import.meta as any).env?.VITE_API_BASE_URL || 'https://code-review-app-tunnel-mjvxteqa.devinapps.com';
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
  return {
    'Content-Type': 'application/json'
  };
};

export default {
  baseUrl: API_BASE_URL,
  version: API_VERSION,
  getUrl: getApiUrl,
  getAuthHeaders: getAuthHeaders,
};
