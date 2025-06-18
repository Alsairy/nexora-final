import React, { Component, ErrorInfo, ReactNode } from 'react';

import { Button, Card, Container, Typography } from '@mui/material';
import { styled } from '@mui/material/styles';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
}

const ErrorContainer = styled(Container)(({ theme }) => ({
  display: 'flex',
  flexDirection: 'column',
  alignItems: 'center',
  justifyContent: 'center',
  minHeight: '100vh',
  padding: theme.spacing(3),
}));

const ErrorCard = styled(Card)(({ theme }) => ({
  padding: theme.spacing(4),
  maxWidth: 600,
  width: '100%',
  textAlign: 'center',
}));

const ErrorDetails = styled('pre')(({ theme }) => ({
  marginTop: theme.spacing(2),
  padding: theme.spacing(2),
  backgroundColor: theme.palette.grey[100],
  borderRadius: theme.shape.borderRadius,
  overflow: 'auto',
  maxHeight: 300,
  textAlign: 'left',
  fontSize: '0.8rem',
}));

/**
 * Error Boundary component that catches JavaScript errors in its child component tree.
 * It displays a fallback UI instead of crashing the whole application.
 */
class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = {
      hasError: false,
      error: null,
      errorInfo: null,
    };
  }

  static getDerivedStateFromError(error: Error): State {
    // Update state so the next render will show the fallback UI
    return {
      hasError: true,
      error,
      errorInfo: null,
    };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    // Log the error to an error reporting service
    console.error('Error caught by ErrorBoundary:', error, errorInfo);
    
    this.setState({
      error,
      errorInfo,
    });
    
    // Here you could also log to an error monitoring service like Sentry
    // if (process.env.NODE_ENV === 'production') {
    //   Sentry.captureException(error);
    // }
  }

  handleReset = (): void => {
    this.setState({
      hasError: false,
      error: null,
      errorInfo: null,
    });
  };

  render(): ReactNode {
    const { hasError, error, errorInfo } = this.state;
    const { children, fallback } = this.props;

    if (hasError) {
      // You can render any custom fallback UI
      if (fallback) {
        return fallback;
      }

      return (
        <ErrorContainer>
          <ErrorCard elevation={3}>
            <Typography variant="h5" component="h2" gutterBottom>
              Something went wrong
            </Typography>
            <Typography variant="body1" color="textSecondary" paragraph>
              We're sorry, but an error occurred while rendering this component.
            </Typography>
            
            {process.env.NODE_ENV !== 'production' && error && (
              <ErrorDetails>
                <strong>{error.toString()}</strong>
                {errorInfo && errorInfo.componentStack}
              </ErrorDetails>
            )}
            
            <Button 
              variant="contained" 
              color="primary" 
              onClick={this.handleReset}
              sx={{ mt: 2 }}
            >
              Try Again
            </Button>
          </ErrorCard>
        </ErrorContainer>
      );
    }

    return children;
  }
}

export default ErrorBoundary;

