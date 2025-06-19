/**
 * Application route constants
 *
 * This file contains all route paths used in the application.
 * Always use these constants instead of hardcoding paths.
 */

export enum ROUTES {
  // Authentication
  LOGIN = '/login',
  REGISTER = '/register',
  FORGOT_PASSWORD = '/forgot-password',
  RESET_PASSWORD = '/reset-password',

  // Main sections
  DASHBOARD = '/dashboard',
  ADMIN = '/admin',
  SETTINGS = '/settings',
  PROFILE = '/profile',

  // Feature modules
  PAYMENT_GATEWAY = '/payment-gateway',
  SMS_GATEWAY = '/sms-gateway',
  E_WALLET = '/e-wallet',
  API_GATEWAY = '/api-gateway',
  E_SIGNATURE = '/e-signature',
  WHATSAPP_CHATBOT = '/whatsapp-chatbot',
  LOAN_MARKETPLACE = '/loan-marketplace',

  // User management
  USER_MANAGEMENT = '/user-management',
  USER_DETAILS = '/user-management/:id',
  USER_CREATE = '/user-management/create',
  USER_EDIT = '/user-management/edit/:id',

  // Admin console
  ADMIN_CONSOLE = '/admin-console',
  ADMIN_SETTINGS = '/admin-console/settings',
  ADMIN_USERS = '/admin-console/users',
  ADMIN_ROLES = '/admin-console/roles',

  // Compliance
  COMPLIANCE_DASHBOARD = '/compliance-dashboard',
  PRIVACY_POLICY = '/privacy-policy',
  TERMS_OF_SERVICE = '/terms-of-service',

  // Error pages
  NOT_FOUND = '/404',
  SERVER_ERROR = '/500',
  UNAUTHORIZED = '/401',
  FORBIDDEN = '/403',
}

// IDs for specific entities that are commonly used
export const ENTITY_IDS = {
  DEFAULT_TENANT_ID: 1,
  SYSTEM_USER_ID: 1,
  DEFAULT_ROLE_ID: 1,
  ADMIN_ROLE_ID: 2,
  USER_ROLE_ID: 3,
};

// Query parameter constants
export const QUERY_PARAMS = {
  PAGE: 'page',
  PAGE_SIZE: 'pageSize',
  SORT: 'sort',
  FILTER: 'filter',
  SEARCH: 'search',
  TAB: 'tab',
  VIEW: 'view',
};

// Fragment identifiers
export const FRAGMENTS = {
  OVERVIEW: '#overview',
  DETAILS: '#details',
  SETTINGS: '#settings',
  HISTORY: '#history',
};

// Route builder functions
export const buildUserDetailsRoute = (userId: number | string): string =>
  ROUTES.USER_DETAILS.replace(':id', userId.toString());

export const buildUserEditRoute = (userId: number | string): string =>
  ROUTES.USER_EDIT.replace(':id', userId.toString());

export default ROUTES;
