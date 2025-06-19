import {
  CheckCircle as CheckCircleIcon,
  Close as CloseIcon,
  Error as ErrorIcon,
  Info as InfoIcon,
  Warning as WarningIcon,
} from '@mui/icons-material';
import type { AlertProps, SnackbarProps } from '@mui/material';
import { Alert, AlertTitle, IconButton, Snackbar } from '@mui/material';
import { styled } from '@mui/material/styles';
import React, { forwardRef } from 'react';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface ToastProps extends Omit<SnackbarProps, 'onClose'> {
  type?: ToastType;
  title?: string;
  message: string;
  onClose?: () => void;
  autoHideDuration?: number;
  alertProps?: Omit<AlertProps, 'severity'>;
}

const StyledAlert = styled(Alert)(({ theme }) => ({
  width: '100%',
  alignItems: 'center',
}));

/**
 * Accessible toast notification component with different types and auto-hide functionality.
 */
const Toast = forwardRef<HTMLDivElement, ToastProps>(
  (
    {
      type = 'info',
      title,
      message,
      open,
      onClose,
      autoHideDuration = 6000,
      anchorOrigin = { vertical: 'bottom', horizontal: 'left' },
      alertProps,
      ...rest
    },
    ref,
  ) => {
    // Get icon based on type
    const getIcon = () => {
      switch (type) {
        case 'success':
          return <CheckCircleIcon fontSize="inherit" />;
        case 'error':
          return <ErrorIcon fontSize="inherit" />;
        case 'warning':
          return <WarningIcon fontSize="inherit" />;
        case 'info':
        default:
          return <InfoIcon fontSize="inherit" />;
      }
    };

    // Handle close
    const handleClose = (event: React.SyntheticEvent | Event, reason?: string) => {
      if (reason === 'clickaway') {
        return;
      }

      if (onClose) {
        onClose();
      }
    };

    return (
      <Snackbar
        open={open}
        autoHideDuration={autoHideDuration}
        onClose={handleClose}
        anchorOrigin={anchorOrigin}
        ref={ref}
        {...rest}
      >
        <StyledAlert
          elevation={6}
          variant="filled"
          severity={type}
          icon={getIcon()}
          action={
            <IconButton
              size="small"
              aria-label="close"
              color="inherit"
              onClick={handleClose as any}
            >
              <CloseIcon fontSize="small" />
            </IconButton>
          }
          {...alertProps}
        >
          {title && <AlertTitle>{title}</AlertTitle>}
          {message}
        </StyledAlert>
      </Snackbar>
    );
  },
);

Toast.displayName = 'Toast';

export default Toast;
