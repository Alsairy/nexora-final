const API_BASE_URL = (import.meta as any).env?.VITE_API_BASE_URL || 'https://user:d6ac29f52eda7da9ac932c93506ddcde@code-review-app-tunnel-mjvxteqa.devinapps.com';

export const getApiUrl = (endpoint: string): string => {
  if (endpoint.startsWith('/')) {
    return `${API_BASE_URL}${endpoint}`;
  }
  return endpoint;
};

export default {
  baseUrl: API_BASE_URL,
  getUrl: getApiUrl,
};
