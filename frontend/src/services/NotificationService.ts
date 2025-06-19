import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

export interface NotificationData {
  type: string;
  data: any;
  timestamp: string;
  priority?: string;
}

export interface PaymentNotification {
  paymentId: string;
  transactionId: string;
  status: 'Pending' | 'Processing' | 'Completed' | 'Failed' | 'Cancelled' | 'Refunded';
  amount: number;
  currency: string;
  paymentMethod: string;
  timestamp: string;
  message: string;
  metadata: Record<string, any>;
}

export interface SmsDeliveryNotification {
  messageId: string;
  phoneNumber: string;
  status: 'Queued' | 'Sent' | 'Delivered' | 'Failed' | 'Expired' | 'Rejected';
  cost: number;
  currency: string;
  provider: string;
  sentAt: string;
  deliveredAt?: string;
  errorMessage?: string;
  retryCount: number;
  metadata: Record<string, any>;
}

export interface PaymentFailureAlert {
  paymentId: string;
  transactionId: string;
  reason: 'InsufficientFunds' | 'InvalidCard' | 'ExpiredCard' | 'CardDeclined' | 'NetworkError' | 'ProcessingError' | 'FraudDetected' | 'LimitExceeded' | 'Unknown';
  errorCode: string;
  errorMessage: string;
  amount: number;
  currency: string;
  paymentMethod: string;
  timestamp: string;
  requiresAction: boolean;
  actionUrl?: string;
  metadata: Record<string, any>;
}

export interface BillingThresholdNotification {
  thresholdId: string;
  type: 'Usage' | 'Cost' | 'MessageCount' | 'ApiCalls';
  currentAmount: number;
  thresholdAmount: number;
  percentageUsed: number;
  currency: string;
  service: string;
  periodStart: string;
  periodEnd: string;
  severity: 'Info' | 'Warning' | 'Critical' | 'Emergency';
  message: string;
  actionUrl?: string;
  metadata: Record<string, any>;
}

export interface NotificationHistory {
  id: string;
  tenantId: string;
  userId: string;
  type: string;
  title: string;
  message: string;
  status: 'Pending' | 'Sent' | 'Delivered' | 'Read' | 'Failed' | 'Expired';
  createdAt: string;
  readAt?: string;
  metadata: Record<string, any>;
}

export type NotificationCallback = (notification: NotificationData) => void;

class NotificationService {
  private connection: HubConnection | null = null;
  private callbacks: Map<string, NotificationCallback[]> = new Map();
  private isConnected = false;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private reconnectDelay = 1000;

  constructor() {
    this.setupConnection();
  }

  private setupConnection() {
    const token = localStorage.getItem('authToken');
    const apiUrl = process.env.REACT_APP_API_URL || 'https://localhost:7001';
    
    this.connection = new HubConnectionBuilder()
      .withUrl(`${apiUrl}/notificationHub`, {
        accessTokenFactory: () => token || '',
        withCredentials: true
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          if (retryContext.previousRetryCount < this.maxReconnectAttempts) {
            return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
          }
          return null;
        }
      })
      .configureLogging(LogLevel.Information)
      .build();

    this.setupEventHandlers();
  }

  private setupEventHandlers() {
    if (!this.connection) return;

    this.connection.onclose((error) => {
      console.log('SignalR connection closed:', error);
      this.isConnected = false;
      this.notifyCallbacks('connectionClosed', { error });
    });

    this.connection.onreconnecting((error) => {
      console.log('SignalR reconnecting:', error);
      this.isConnected = false;
      this.notifyCallbacks('reconnecting', { error });
    });

    this.connection.onreconnected((connectionId) => {
      console.log('SignalR reconnected:', connectionId);
      this.isConnected = true;
      this.reconnectAttempts = 0;
      this.notifyCallbacks('reconnected', { connectionId });
    });

    this.connection.on('Connected', (data) => {
      console.log('Connected to notification hub:', data);
      this.isConnected = true;
      this.notifyCallbacks('connected', data);
    });

    this.connection.on('PaymentStatusUpdate', (notification: NotificationData) => {
      console.log('Payment status update:', notification);
      this.notifyCallbacks('paymentStatus', notification);
    });

    this.connection.on('SmsDeliveryUpdate', (notification: NotificationData) => {
      console.log('SMS delivery update:', notification);
      this.notifyCallbacks('smsDelivery', notification);
    });

    this.connection.on('PaymentFailureAlert', (notification: NotificationData) => {
      console.log('Payment failure alert:', notification);
      this.notifyCallbacks('paymentFailure', notification);
    });

    this.connection.on('BillingThresholdAlert', (notification: NotificationData) => {
      console.log('Billing threshold alert:', notification);
      this.notifyCallbacks('billingThreshold', notification);
    });

    this.connection.on('MultiChannelNotification', (notification: NotificationData) => {
      console.log('Multi-channel notification:', notification);
      this.notifyCallbacks('multiChannel', notification);
    });

    this.connection.on('NotificationHistory', (history: NotificationHistory[]) => {
      console.log('Notification history:', history);
      this.notifyCallbacks('notificationHistory', { history });
    });

    this.connection.on('NotificationMarkedAsRead', (data: { notificationId: string }) => {
      console.log('Notification marked as read:', data);
      this.notifyCallbacks('notificationRead', data);
    });
  }

  async connect(): Promise<void> {
    if (!this.connection || this.isConnected) return;

    try {
      await this.connection.start();
      console.log('SignalR connection established');
      this.isConnected = true;
      this.reconnectAttempts = 0;
    } catch (error) {
      console.error('Failed to connect to SignalR hub:', error);
      this.reconnectAttempts++;
      
      if (this.reconnectAttempts < this.maxReconnectAttempts) {
        setTimeout(() => this.connect(), this.reconnectDelay * this.reconnectAttempts);
      }
      
      throw error;
    }
  }

  async disconnect(): Promise<void> {
    if (!this.connection) return;

    try {
      await this.connection.stop();
      console.log('SignalR connection stopped');
      this.isConnected = false;
    } catch (error) {
      console.error('Error stopping SignalR connection:', error);
      throw error;
    }
  }

  subscribe(eventType: string, callback: NotificationCallback): void {
    if (!this.callbacks.has(eventType)) {
      this.callbacks.set(eventType, []);
    }
    this.callbacks.get(eventType)!.push(callback);
  }

  unsubscribe(eventType: string, callback: NotificationCallback): void {
    const callbacks = this.callbacks.get(eventType);
    if (callbacks) {
      const index = callbacks.indexOf(callback);
      if (index > -1) {
        callbacks.splice(index, 1);
      }
    }
  }

  private notifyCallbacks(eventType: string, data: any): void {
    const callbacks = this.callbacks.get(eventType);
    if (callbacks) {
      callbacks.forEach(callback => {
        try {
          callback({ type: eventType, data, timestamp: new Date().toISOString() });
        } catch (error) {
          console.error('Error in notification callback:', error);
        }
      });
    }
  }

  async markNotificationAsRead(notificationId: string): Promise<void> {
    if (!this.connection || !this.isConnected) {
      throw new Error('Not connected to notification hub');
    }

    try {
      await this.connection.invoke('MarkNotificationAsRead', notificationId);
    } catch (error) {
      console.error('Failed to mark notification as read:', error);
      throw error;
    }
  }

  async getNotificationHistory(pageSize: number = 50, pageNumber: number = 1): Promise<void> {
    if (!this.connection || !this.isConnected) {
      throw new Error('Not connected to notification hub');
    }

    try {
      await this.connection.invoke('GetNotificationHistory', pageSize, pageNumber);
    } catch (error) {
      console.error('Failed to get notification history:', error);
      throw error;
    }
  }

  async joinTenantGroup(tenantId: string): Promise<void> {
    if (!this.connection || !this.isConnected) {
      throw new Error('Not connected to notification hub');
    }

    try {
      await this.connection.invoke('JoinTenantGroup', tenantId);
    } catch (error) {
      console.error('Failed to join tenant group:', error);
      throw error;
    }
  }

  async leaveTenantGroup(tenantId: string): Promise<void> {
    if (!this.connection || !this.isConnected) {
      throw new Error('Not connected to notification hub');
    }

    try {
      await this.connection.invoke('LeaveTenantGroup', tenantId);
    } catch (error) {
      console.error('Failed to leave tenant group:', error);
      throw error;
    }
  }

  getConnectionState(): string {
    if (!this.connection) return 'Disconnected';
    return this.connection.state;
  }

  isConnectionActive(): boolean {
    return this.isConnected && this.connection?.state === 'Connected';
  }
}

export const notificationService = new NotificationService();
export default NotificationService;
