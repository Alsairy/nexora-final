import React, { useState, useEffect } from 'react';
import {
  Badge,
  IconButton,
  Menu,
  MenuItem,
  Typography,
  Box,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Chip,
  Button,
  Divider,
  Alert,
  CircularProgress,
  Tooltip
} from '@mui/material';
import {
  Notifications as NotificationsIcon,
  Payment as PaymentIcon,
  Sms as SmsIcon,
  Warning as WarningIcon,
  TrendingUp as TrendingUpIcon,
  CheckCircle as CheckCircleIcon,
  Error as ErrorIcon,
  Info as InfoIcon,
  Close as CloseIcon
} from '@mui/icons-material';
import { notificationService, NotificationData, NotificationHistory } from '../services/NotificationService';

interface NotificationCenterProps {
  onNotificationClick?: (notification: NotificationData) => void;
}

const NotificationCenter: React.FC<NotificationCenterProps> = ({ onNotificationClick }) => {
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [notifications, setNotifications] = useState<NotificationData[]>([]);
  const [notificationHistory, setNotificationHistory] = useState<NotificationHistory[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [isConnected, setIsConnected] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [connectionError, setConnectionError] = useState<string | null>(null);

  useEffect(() => {
    const initializeNotifications = async () => {
      try {
        setIsLoading(true);
        await notificationService.connect();
        setIsConnected(true);
        setConnectionError(null);
      } catch (error) {
        console.error('Failed to initialize notifications:', error);
        setConnectionError('Failed to connect to notification service');
        setIsConnected(false);
      } finally {
        setIsLoading(false);
      }
    };

    initializeNotifications();

    const handlePaymentStatus = (notification: NotificationData) => {
      addNotification(notification);
    };

    const handleSmsDelivery = (notification: NotificationData) => {
      addNotification(notification);
    };

    const handlePaymentFailure = (notification: NotificationData) => {
      addNotification(notification);
    };

    const handleBillingThreshold = (notification: NotificationData) => {
      addNotification(notification);
    };

    const handleMultiChannel = (notification: NotificationData) => {
      addNotification(notification);
    };

    const handleConnectionClosed = () => {
      setIsConnected(false);
      setConnectionError('Connection lost');
    };

    const handleReconnected = () => {
      setIsConnected(true);
      setConnectionError(null);
    };

    const handleNotificationHistory = (data: { history: NotificationHistory[] }) => {
      setNotificationHistory(data.history);
    };

    notificationService.subscribe('paymentStatus', handlePaymentStatus);
    notificationService.subscribe('smsDelivery', handleSmsDelivery);
    notificationService.subscribe('paymentFailure', handlePaymentFailure);
    notificationService.subscribe('billingThreshold', handleBillingThreshold);
    notificationService.subscribe('multiChannel', handleMultiChannel);
    notificationService.subscribe('connectionClosed', handleConnectionClosed);
    notificationService.subscribe('reconnected', handleReconnected);
    notificationService.subscribe('notificationHistory', handleNotificationHistory);

    return () => {
      notificationService.unsubscribe('paymentStatus', handlePaymentStatus);
      notificationService.unsubscribe('smsDelivery', handleSmsDelivery);
      notificationService.unsubscribe('paymentFailure', handlePaymentFailure);
      notificationService.unsubscribe('billingThreshold', handleBillingThreshold);
      notificationService.unsubscribe('multiChannel', handleMultiChannel);
      notificationService.unsubscribe('connectionClosed', handleConnectionClosed);
      notificationService.unsubscribe('reconnected', handleReconnected);
      notificationService.unsubscribe('notificationHistory', handleNotificationHistory);
      notificationService.disconnect();
    };
  }, []);

  const addNotification = (notification: NotificationData) => {
    setNotifications(prev => [notification, ...prev.slice(0, 49)]); // Keep last 50 notifications
    setUnreadCount(prev => prev + 1);
  };

  const handleClick = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
    if (unreadCount > 0) {
      loadNotificationHistory();
    }
  };

  const handleClose = () => {
    setAnchorEl(null);
  };

  const handleNotificationClick = async (notification: NotificationData, index: number) => {
    if (onNotificationClick) {
      onNotificationClick(notification);
    }

    setNotifications(prev => prev.filter((_, i) => i !== index));
    setUnreadCount(prev => Math.max(0, prev - 1));

    handleClose();
  };

  const loadNotificationHistory = async () => {
    try {
      await notificationService.getNotificationHistory(20, 1);
    } catch (error) {
      console.error('Failed to load notification history:', error);
    }
  };

  const clearAllNotifications = () => {
    setNotifications([]);
    setUnreadCount(0);
  };

  const getNotificationIcon = (type: string) => {
    switch (type) {
      case 'paymentStatus':
        return <PaymentIcon color="primary" />;
      case 'smsDelivery':
        return <SmsIcon color="info" />;
      case 'paymentFailure':
        return <ErrorIcon color="error" />;
      case 'billingThreshold':
        return <WarningIcon color="warning" />;
      case 'multiChannel':
        return <InfoIcon color="info" />;
      default:
        return <NotificationsIcon />;
    }
  };

  const getNotificationSeverity = (type: string, data: any) => {
    switch (type) {
      case 'paymentFailure':
        return 'error';
      case 'billingThreshold':
        return data.severity === 'Critical' || data.severity === 'Emergency' ? 'error' : 'warning';
      case 'paymentStatus':
        return data.status === 'Completed' ? 'success' : 'info';
      case 'smsDelivery':
        return data.status === 'Delivered' ? 'success' : data.status === 'Failed' ? 'error' : 'info';
      default:
        return 'info';
    }
  };

  const formatNotificationMessage = (notification: NotificationData) => {
    const { type, data } = notification;
    
    switch (type) {
      case 'paymentStatus':
        return `Payment ${data.paymentId} is ${data.status.toLowerCase()}`;
      case 'smsDelivery':
        return `SMS to ${data.phoneNumber} was ${data.status.toLowerCase()}`;
      case 'paymentFailure':
        return `Payment failed: ${data.errorMessage}`;
      case 'billingThreshold':
        return `${data.service} usage: ${data.percentageUsed}% of threshold reached`;
      default:
        return data.message || 'New notification';
    }
  };

  const formatTimestamp = (timestamp: string) => {
    const date = new Date(timestamp);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    return date.toLocaleDateString();
  };

  return (
    <>
      <Tooltip title={isConnected ? 'Notifications' : 'Notifications (Disconnected)'}>
        <IconButton
          color="inherit"
          onClick={handleClick}
          sx={{
            position: 'relative',
            opacity: isConnected ? 1 : 0.6
          }}
        >
          <Badge badgeContent={unreadCount} color="error" max={99}>
            <NotificationsIcon />
          </Badge>
          {isLoading && (
            <CircularProgress
              size={24}
              sx={{
                position: 'absolute',
                top: '50%',
                left: '50%',
                marginTop: '-12px',
                marginLeft: '-12px',
              }}
            />
          )}
        </IconButton>
      </Tooltip>

      <Menu
        anchorEl={anchorEl}
        open={Boolean(anchorEl)}
        onClose={handleClose}
        PaperProps={{
          sx: {
            width: 400,
            maxHeight: 600,
            overflow: 'hidden',
            display: 'flex',
            flexDirection: 'column'
          }
        }}
        transformOrigin={{ horizontal: 'right', vertical: 'top' }}
        anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
      >
        <Box sx={{ p: 2, borderBottom: 1, borderColor: 'divider' }}>
          <Box display="flex" justifyContent="space-between" alignItems="center">
            <Typography variant="h6">
              Notifications
            </Typography>
            <Box display="flex" alignItems="center" gap={1}>
              {connectionError && (
                <Chip
                  label="Offline"
                  color="error"
                  size="small"
                  icon={<ErrorIcon />}
                />
              )}
              {isConnected && (
                <Chip
                  label="Live"
                  color="success"
                  size="small"
                  icon={<CheckCircleIcon />}
                />
              )}
              {notifications.length > 0 && (
                <Button
                  size="small"
                  onClick={clearAllNotifications}
                  startIcon={<CloseIcon />}
                >
                  Clear All
                </Button>
              )}
            </Box>
          </Box>
        </Box>

        {connectionError && (
          <Alert severity="warning" sx={{ m: 1 }}>
            {connectionError}
          </Alert>
        )}

        <Box sx={{ flex: 1, overflow: 'auto' }}>
          {notifications.length === 0 ? (
            <Box sx={{ p: 3, textAlign: 'center' }}>
              <NotificationsIcon sx={{ fontSize: 48, color: 'text.secondary', mb: 1 }} />
              <Typography variant="body2" color="text.secondary">
                No new notifications
              </Typography>
            </Box>
          ) : (
            <List sx={{ p: 0 }}>
              {notifications.map((notification, index) => (
                <React.Fragment key={`${notification.type}-${notification.timestamp}-${index}`}>
                  <ListItem
                    button
                    onClick={() => handleNotificationClick(notification, index)}
                    sx={{
                      '&:hover': {
                        backgroundColor: 'action.hover'
                      }
                    }}
                  >
                    <ListItemIcon>
                      {getNotificationIcon(notification.type)}
                    </ListItemIcon>
                    <ListItemText
                      primary={
                        <Box display="flex" justifyContent="space-between" alignItems="flex-start">
                          <Typography variant="body2" sx={{ fontWeight: 500 }}>
                            {formatNotificationMessage(notification)}
                          </Typography>
                          <Typography variant="caption" color="text.secondary" sx={{ ml: 1, flexShrink: 0 }}>
                            {formatTimestamp(notification.timestamp)}
                          </Typography>
                        </Box>
                      }
                      secondary={
                        <Box mt={0.5}>
                          <Chip
                            label={notification.type.replace(/([A-Z])/g, ' $1').trim()}
                            size="small"
                            color={getNotificationSeverity(notification.type, notification.data) as any}
                            variant="outlined"
                          />
                        </Box>
                      }
                    />
                  </ListItem>
                  {index < notifications.length - 1 && <Divider />}
                </React.Fragment>
              ))}
            </List>
          )}
        </Box>

        {notifications.length > 0 && (
          <Box sx={{ p: 1, borderTop: 1, borderColor: 'divider' }}>
            <Button
              fullWidth
              size="small"
              onClick={loadNotificationHistory}
              disabled={!isConnected}
            >
              View All Notifications
            </Button>
          </Box>
        )}
      </Menu>
    </>
  );
};

export default NotificationCenter;
