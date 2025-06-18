import React, { Suspense } from 'react';
import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';

import { CircularProgress, CssBaseline, ThemeProvider } from '@mui/material';

import CookieBanner from './components/CookieBanner';
import ErrorBoundary from './components/ErrorBoundary';
import Layout from './components/Layout';
import ProtectedRoute from './components/ProtectedRoute';
import { AuthProvider } from './contexts/AuthContext';
import { ThemeProvider as CustomThemeProvider } from './contexts/ThemeContext';
import ROUTES from './routes';

// Lazy-loaded components
const Dashboard = React.lazy(() => import('./pages/Dashboard'));
const Login = React.lazy(() => import('./pages/Login'));
const Register = React.lazy(() => import('./pages/Register'));
const ForgotPassword = React.lazy(() => import('./pages/ForgotPassword'));
const PaymentGateway = React.lazy(() => import('./pages/PaymentGateway'));
const SmsGateway = React.lazy(() => import('./pages/SmsGateway'));
const EWallet = React.lazy(() => import('./pages/EWallet'));
const ApiGateway = React.lazy(() => import('./pages/ApiGateway'));
const ESignature = React.lazy(() => import('./pages/ESignature'));
const WhatsAppChatbot = React.lazy(() => import('./pages/WhatsAppChatbot'));
const LoanMarketplace = React.lazy(() => import('./pages/LoanMarketplace'));
const UserManagement = React.lazy(() => import('./pages/UserManagement'));
const AdminConsole = React.lazy(() => import('./pages/AdminConsole'));
const Settings = React.lazy(() => import('./pages/Settings'));
const PrivacyPolicy = React.lazy(() => import('./pages/PrivacyPolicy'));
const NotFound = React.lazy(() => import('./pages/NotFound'));

// Loading fallback
const LoadingFallback = () => (
  <div style={{ 
    display: 'flex', 
    justifyContent: 'center', 
    alignItems: 'center', 
    height: '100vh' 
  }}>
    <CircularProgress aria-label="Loading content" />
  </div>
);

const App: React.FC = () => {
  return (
    <ErrorBoundary>
      <CustomThemeProvider>
        {(theme) => (
          <ThemeProvider theme={theme}>
            <CssBaseline />
            <AuthProvider>
              <Router>
                <Layout>
                  <ErrorBoundary>
                    <Suspense fallback={<LoadingFallback />}>
                      <Routes>
                        {/* Public routes */}
                        <Route path={ROUTES.LOGIN} element={<Login />} />
                        <Route path={ROUTES.REGISTER} element={<Register />} />
                        <Route path={ROUTES.FORGOT_PASSWORD} element={<ForgotPassword />} />
                        <Route path={ROUTES.PRIVACY_POLICY} element={<PrivacyPolicy />} />
                        
                        {/* Protected routes */}
                        <Route element={<ProtectedRoute />}>
                          <Route path="/" element={<Dashboard />} />
                          <Route path={ROUTES.DASHBOARD} element={<Dashboard />} />
                          <Route path={ROUTES.PAYMENT_GATEWAY} element={<PaymentGateway />} />
                          <Route path={ROUTES.SMS_GATEWAY} element={<SmsGateway />} />
                          <Route path={ROUTES.E_WALLET} element={<EWallet />} />
                          <Route path={ROUTES.API_GATEWAY} element={<ApiGateway />} />
                          <Route path={ROUTES.E_SIGNATURE} element={<ESignature />} />
                          <Route path={ROUTES.WHATSAPP_CHATBOT} element={<WhatsAppChatbot />} />
                          <Route path={ROUTES.LOAN_MARKETPLACE} element={<LoanMarketplace />} />
                          <Route path={ROUTES.USER_MANAGEMENT} element={<UserManagement />} />
                          <Route path={ROUTES.ADMIN_CONSOLE} element={<AdminConsole />} />
                          <Route path={ROUTES.SETTINGS} element={<Settings />} />
                        </Route>
                        
                        {/* 404 route */}
                        <Route path="*" element={<NotFound />} />
                      </Routes>
                    </Suspense>
                  </ErrorBoundary>
                  
                  {/* Cookie consent banner */}
                  <CookieBanner />
                </Layout>
              </Router>
            </AuthProvider>
          </ThemeProvider>
        )}
      </CustomThemeProvider>
    </ErrorBoundary>
  );
};

export default App;

