import {
  Payment,
  Add,
  Visibility,
  GetApp,
  TrendingUp,
  CheckCircle,
  Error,
  Pending,
  AccountBalance,
  CreditCard,
  Smartphone,
  Analytics,
  Refresh,
  FilterList,
  Search,
  Download,
  Settings,
  Notifications,
  AttachMoney,
  Euro,
  CurrencyPound,
  Schedule,
  ExpandMore,
  Dashboard,
  Receipt,
  SwapHoriz,
  Security,
  Speed,
  TrendingDown,
  Warning,
  Info,
  MonetizationOn,
  Sms,
  SubscriptionsOutlined,
  CurrencyExchange,
  Sync,
} from '@mui/icons-material';
import {
  Box,
  Card,
  CardContent,
  Container,
  Grid,
  Typography,
  Button,
  TextField,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Chip,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Tabs,
  Tab,
  LinearProgress,
  Alert,
  Snackbar,
  Divider,
  Avatar,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Switch,
  FormControlLabel,
  Tooltip,
  Badge,
  CircularProgress,
  Accordion,
  AccordionSummary,
  AccordionDetails,
} from '@mui/material';
import React, { useState, useEffect, useCallback } from 'react';

interface Transaction {
  id: string;
  amount: number;
  currency: string;
  status: 'completed' | 'pending' | 'failed' | 'refunded';
  merchant: string;
  date: string;
  reference: string;
  paymentMethod: string;
  customerEmail?: string;
  fees?: number;
  netAmount?: number;
  country?: string;
  riskScore?: number;
  processingTime?: number;
}

interface PaymentMetrics {
  totalTransactions: number;
  totalRevenue: number;
  successfulPayments: number;
  failedPayments: number;
  refundedPayments: number;
  successRate: number;
  averageTransactionValue: number;
  totalRefundAmount: number;
  netRevenue: number;
  currency: string;
}

interface RevenueAnalytics {
  totalRevenue: number;
  recurringRevenue: number;
  oneTimeRevenue: number;
  smsRevenue: number;
  refundAmount: number;
  netRevenue: number;
  growthRate: number;
  dailyBreakdown: DailyRevenue[];
  revenueBySource: { [key: string]: number };
  revenueByCurrency: { [key: string]: number };
}

interface DailyRevenue {
  date: string;
  revenue: number;
  transactionCount: number;
  averageValue: number;
}

interface SmsUsageData {
  totalMessages: number;
  totalCost: number;
  averageCostPerMessage: number;
  usageByCountry: { [key: string]: any };
  dailyUsage: any[];
}

interface Subscription {
  id: string;
  planName: string;
  amount: number;
  currency: string;
  interval: string;
  status: 'active' | 'cancelled' | 'past_due';
  nextBillingDate: string;
  customerEmail: string;
}

interface CurrencyRate {
  from: string;
  to: string;
  rate: number;
  lastUpdated: string;
}

interface PaymentAlert {
  id: string;
  type: 'high_volume' | 'failed_payment' | 'fraud_detected' | 'low_balance';
  message: string;
  severity: 'low' | 'medium' | 'high';
  timestamp: string;
  isRead: boolean;
}

const PaymentGateway: React.FC = () => {
  const [openDialog, setOpenDialog] = useState(false);
  const [selectedTransaction, setSelectedTransaction] = useState<Transaction | null>(null);
  const [activeTab, setActiveTab] = useState(0);
  const [loading, setLoading] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [dateRange, setDateRange] = useState('7d');
  const [selectedCurrency, setSelectedCurrency] = useState('USD');
  const [paymentMetrics, setPaymentMetrics] = useState<PaymentMetrics | null>(null);
  const [revenueAnalytics, setRevenueAnalytics] = useState<RevenueAnalytics | null>(null);
  const [smsUsage, setSmsUsage] = useState<SmsUsageData | null>(null);
  const [subscriptions, setSubscriptions] = useState<Subscription[]>([]);
  const [currencyRates, setCurrencyRates] = useState<CurrencyRate[]>([]);
  const [alerts, setAlerts] = useState<PaymentAlert[]>([]);
  const [realTimeEnabled, setRealTimeEnabled] = useState(true);
  const [snackbar, setSnackbar] = useState({
    open: false,
    message: '',
    severity: 'info' as 'success' | 'error' | 'warning' | 'info',
  });
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [paymentMethodFilter, setPaymentMethodFilter] = useState('all');

  const [transactions, setTransactions] = useState<Transaction[]>([]);

  const mockTransactions: Transaction[] = [
    {
      id: 'TXN-001',
      amount: 1250.0,
      currency: 'USD',
      status: 'completed',
      merchant: 'E-Commerce Store',
      date: '2024-06-18 10:30:00',
      reference: 'REF-2024-001',
      paymentMethod: 'Credit Card',
      customerEmail: 'customer@example.com',
      fees: 36.25,
      netAmount: 1213.75,
      country: 'US',
      riskScore: 15,
      processingTime: 2.3,
    },
    {
      id: 'TXN-002',
      amount: 750.5,
      currency: 'EUR',
      status: 'pending',
      merchant: 'Online Services',
      date: '2024-06-18 09:15:00',
      reference: 'REF-2024-002',
      paymentMethod: 'Bank Transfer',
      customerEmail: 'user@domain.com',
      fees: 15.01,
      netAmount: 735.49,
      country: 'DE',
      riskScore: 8,
      processingTime: 0,
    },
    {
      id: 'TXN-003',
      amount: 2100.0,
      currency: 'SAR',
      status: 'failed',
      merchant: 'Digital Products',
      date: '2024-06-18 08:45:00',
      reference: 'REF-2024-003',
      paymentMethod: 'Digital Wallet',
      customerEmail: 'buyer@company.sa',
      fees: 0,
      netAmount: 0,
      country: 'SA',
      riskScore: 85,
      processingTime: 1.8,
    },
    {
      id: 'TXN-004',
      amount: 450.75,
      currency: 'GBP',
      status: 'refunded',
      merchant: 'Subscription Service',
      date: '2024-06-18 07:20:00',
      reference: 'REF-2024-004',
      paymentMethod: 'Credit Card',
      customerEmail: 'client@uk.co',
      fees: 13.52,
      netAmount: -450.75,
      country: 'GB',
      riskScore: 12,
      processingTime: 3.1,
    },
  ];

  const mockSubscriptions: Subscription[] = [
    {
      id: 'SUB-001',
      planName: 'Premium SMS Plan',
      amount: 99.99,
      currency: 'USD',
      interval: 'monthly',
      status: 'active',
      nextBillingDate: '2024-07-18',
      customerEmail: 'enterprise@company.com',
    },
    {
      id: 'SUB-002',
      planName: 'Basic Communication Package',
      amount: 29.99,
      currency: 'USD',
      interval: 'monthly',
      status: 'active',
      nextBillingDate: '2024-07-15',
      customerEmail: 'startup@business.com',
    },
  ];

  const mockAlerts: PaymentAlert[] = [
    {
      id: 'ALERT-001',
      type: 'high_volume',
      message: 'Transaction volume 25% above normal for the past hour',
      severity: 'medium',
      timestamp: '2024-06-18 11:45:00',
      isRead: false,
    },
    {
      id: 'ALERT-002',
      type: 'fraud_detected',
      message: 'Suspicious payment pattern detected from IP 192.168.1.100',
      severity: 'high',
      timestamp: '2024-06-18 11:30:00',
      isRead: false,
    },
  ];

  const fetchPaymentMetrics = useCallback(async () => {
    try {
      setLoading(true);
      const endDate = new Date();
      const startDate = new Date();

      switch (dateRange) {
        case '7d':
          startDate.setDate(endDate.getDate() - 7);
          break;
        case '30d':
          startDate.setDate(endDate.getDate() - 30);
          break;
        case '90d':
          startDate.setDate(endDate.getDate() - 90);
          break;
        default:
          startDate.setDate(endDate.getDate() - 7);
      }

      const mockMetrics: PaymentMetrics = {
        totalTransactions: mockTransactions.length,
        totalRevenue: mockTransactions
          .filter(t => t.status === 'completed')
          .reduce((sum, t) => sum + t.amount, 0),
        successfulPayments: mockTransactions.filter(t => t.status === 'completed').length,
        failedPayments: mockTransactions.filter(t => t.status === 'failed').length,
        refundedPayments: mockTransactions.filter(t => t.status === 'refunded').length,
        successRate: 75.5,
        averageTransactionValue: 1137.81,
        totalRefundAmount: 450.75,
        netRevenue: 2949.49,
        currency: selectedCurrency,
      };

      setPaymentMetrics(mockMetrics);
      setTransactions(mockTransactions);
      setSubscriptions(mockSubscriptions);
      setAlerts(mockAlerts);

      setSmsUsage({
        totalMessages: 15420,
        totalCost: 462.6,
        averageCostPerMessage: 0.03,
        usageByCountry: { SA: 8500, US: 4200, GB: 2720 },
        dailyUsage: [],
      });

      setCurrencyRates([
        { from: 'USD', to: 'EUR', rate: 0.85, lastUpdated: '2024-06-18 12:00:00' },
        { from: 'USD', to: 'SAR', rate: 3.75, lastUpdated: '2024-06-18 12:00:00' },
        { from: 'USD', to: 'GBP', rate: 0.79, lastUpdated: '2024-06-18 12:00:00' },
      ]);
    } catch (error) {
      console.error('Error fetching payment metrics:', error);
      setSnackbar({ open: true, message: 'Failed to load payment data', severity: 'error' });
    } finally {
      setLoading(false);
    }
  }, [dateRange, selectedCurrency]);

  const refreshData = async () => {
    setRefreshing(true);
    await fetchPaymentMetrics();
    setRefreshing(false);
    setSnackbar({ open: true, message: 'Data refreshed successfully', severity: 'success' });
  };

  const processPayment = async (paymentData: any) => {
    try {
      setLoading(true);
      await new Promise(resolve => setTimeout(resolve, 2000));
      setSnackbar({ open: true, message: 'Payment processed successfully', severity: 'success' });
      await fetchPaymentMetrics();
    } catch (error) {
      setSnackbar({ open: true, message: 'Payment processing failed', severity: 'error' });
    } finally {
      setLoading(false);
    }
  };

  const exportTransactions = async () => {
    try {
      setLoading(true);
      await new Promise(resolve => setTimeout(resolve, 1500));
      setSnackbar({
        open: true,
        message: 'Transactions exported successfully',
        severity: 'success',
      });
    } catch (error) {
      setSnackbar({ open: true, message: 'Export failed', severity: 'error' });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchPaymentMetrics();
  }, [fetchPaymentMetrics]);

  useEffect(() => {
    if (!realTimeEnabled) return;

    const interval = setInterval(() => {
      if (Math.random() > 0.8) {
        const newAlert: PaymentAlert = {
          id: `ALERT-${Date.now()}`,
          type: 'high_volume',
          message: 'New transaction processed',
          severity: 'low',
          timestamp: new Date().toISOString(),
          isRead: false,
        };
        setAlerts(prev => [newAlert, ...prev.slice(0, 9)]);
      }
    }, 30000);

    return () => clearInterval(interval);
  }, [realTimeEnabled]);

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'completed':
        return <CheckCircle color="success" />;
      case 'pending':
        return <Pending color="warning" />;
      case 'failed':
        return <Error color="error" />;
      case 'refunded':
        return <SwapHoriz color="info" />;
      default:
        return <Pending />;
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'completed':
        return 'success';
      case 'pending':
        return 'warning';
      case 'failed':
        return 'error';
      case 'refunded':
        return 'info';
      default:
        return 'default';
    }
  };

  const getCurrencyIcon = (currency: string) => {
    switch (currency) {
      case 'USD':
        return <AttachMoney />;
      case 'EUR':
        return <Euro />;
      case 'GBP':
        return <CurrencyPound />;
      case 'SAR':
        return <MonetizationOn />;
      default:
        return <CurrencyExchange />;
    }
  };

  const getRiskColor = (riskScore: number) => {
    if (riskScore < 30) return 'success';
    if (riskScore < 70) return 'warning';
    return 'error';
  };

  const filteredTransactions = transactions.filter(transaction => {
    const matchesSearch =
      transaction.id.toLowerCase().includes(searchTerm.toLowerCase()) ||
      transaction.merchant.toLowerCase().includes(searchTerm.toLowerCase()) ||
      transaction.customerEmail?.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesStatus = statusFilter === 'all' || transaction.status === statusFilter;
    const matchesPaymentMethod =
      paymentMethodFilter === 'all' || transaction.paymentMethod === paymentMethodFilter;

    return matchesSearch && matchesStatus && matchesPaymentMethod;
  });

  const handleViewTransaction = (transaction: Transaction) => {
    setSelectedTransaction(transaction);
    setOpenDialog(true);
  };

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  const handleCloseSnackbar = () => {
    setSnackbar({ ...snackbar, open: false });
  };

  const unreadAlertsCount = alerts.filter(alert => !alert.isRead).length;

  const totalAmount = paymentMetrics?.totalRevenue || 0;
  const completedTransactions = paymentMetrics?.successfulPayments || 0;
  const pendingTransactions = transactions.filter(txn => txn.status === 'pending').length;
  const failedTransactions = paymentMetrics?.failedPayments || 0;

  return (
    <Container maxWidth="xl">
      <Box sx={{ mb: 4 }}>
        <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
          <Box>
            <Typography
              variant="h4"
              component="h1"
              gutterBottom
              sx={{ fontWeight: 600, color: 'primary.main' }}
            >
              Payment Gateway
            </Typography>
            <Typography variant="body1" color="text.secondary">
              World-class fintech platform for payment processing and analytics
            </Typography>
          </Box>
          <Box display="flex" gap={2} alignItems="center">
            <FormControlLabel
              control={
                <Switch
                  checked={realTimeEnabled}
                  onChange={e => setRealTimeEnabled(e.target.checked)}
                  color="primary"
                />
              }
              label={
                <Box display="flex" alignItems="center" gap={1}>
                  <Sync color={realTimeEnabled ? 'primary' : 'disabled'} />
                  Real-time
                </Box>
              }
            />
            <FormControl size="small" sx={{ minWidth: 120 }}>
              <InputLabel>Period</InputLabel>
              <Select value={dateRange} label="Period" onChange={e => setDateRange(e.target.value)}>
                <MenuItem value="7d">Last 7 days</MenuItem>
                <MenuItem value="30d">Last 30 days</MenuItem>
                <MenuItem value="90d">Last 90 days</MenuItem>
              </Select>
            </FormControl>
            <FormControl size="small" sx={{ minWidth: 100 }}>
              <InputLabel>Currency</InputLabel>
              <Select
                value={selectedCurrency}
                label="Currency"
                onChange={e => setSelectedCurrency(e.target.value)}
              >
                <MenuItem value="USD">USD</MenuItem>
                <MenuItem value="EUR">EUR</MenuItem>
                <MenuItem value="SAR">SAR</MenuItem>
                <MenuItem value="GBP">GBP</MenuItem>
              </Select>
            </FormControl>
            <Tooltip title="Refresh Data">
              <IconButton
                onClick={refreshData}
                disabled={refreshing}
                sx={{
                  bgcolor: 'primary.main',
                  color: 'white',
                  '&:hover': { bgcolor: 'primary.dark' },
                }}
              >
                {refreshing ? <CircularProgress size={20} color="inherit" /> : <Refresh />}
              </IconButton>
            </Tooltip>
            <Badge badgeContent={unreadAlertsCount} color="error">
              <IconButton sx={{ bgcolor: 'warning.light', color: 'warning.contrastText' }}>
                <Notifications />
              </IconButton>
            </Badge>
          </Box>
        </Box>

        <Tabs value={activeTab} onChange={handleTabChange} sx={{ mb: 3 }}>
          <Tab icon={<Dashboard />} label="Overview" />
          <Tab icon={<Analytics />} label="Analytics" />
          <Tab icon={<SubscriptionsOutlined />} label="Subscriptions" />
          <Tab icon={<Sms />} label="SMS Billing" />
          <Tab icon={<CurrencyExchange />} label="Multi-Currency" />
          <Tab icon={<Settings />} label="Settings" />
        </Tabs>
      </Box>

      {loading && <LinearProgress sx={{ mb: 2 }} />}

      {/* Enhanced Metrics Cards */}
      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12} sm={6} md={3}>
          <Card
            sx={{ background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)', color: 'white' }}
          >
            <CardContent>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography color="inherit" gutterBottom variant="body2" sx={{ opacity: 0.8 }}>
                    Total Revenue
                  </Typography>
                  <Typography variant="h4" sx={{ fontWeight: 700 }}>
                    {getCurrencyIcon(selectedCurrency)}
                    {paymentMetrics?.totalRevenue.toLocaleString() || totalAmount.toLocaleString()}
                  </Typography>
                  <Typography variant="body2" sx={{ opacity: 0.8, mt: 1 }}>
                    Net: {getCurrencyIcon(selectedCurrency)}
                    {paymentMetrics?.netRevenue.toLocaleString() || '0'}
                  </Typography>
                </Box>
                <MonetizationOn sx={{ fontSize: 48, opacity: 0.8 }} />
              </Box>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Card
            sx={{ background: 'linear-gradient(135deg, #f093fb 0%, #f5576c 100%)', color: 'white' }}
          >
            <CardContent>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography color="inherit" gutterBottom variant="body2" sx={{ opacity: 0.8 }}>
                    Success Rate
                  </Typography>
                  <Typography variant="h4" sx={{ fontWeight: 700 }}>
                    {paymentMetrics?.successRate.toFixed(1) ||
                      Math.round((completedTransactions / Math.max(transactions.length, 1)) * 100)}
                    %
                  </Typography>
                  <Box display="flex" alignItems="center" sx={{ mt: 1 }}>
                    <TrendingUp sx={{ fontSize: 16, mr: 0.5 }} />
                    <Typography variant="body2" sx={{ opacity: 0.8 }}>
                      +2.5% vs last period
                    </Typography>
                  </Box>
                </Box>
                <CheckCircle sx={{ fontSize: 48, opacity: 0.8 }} />
              </Box>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Card
            sx={{ background: 'linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)', color: 'white' }}
          >
            <CardContent>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography color="inherit" gutterBottom variant="body2" sx={{ opacity: 0.8 }}>
                    Transactions
                  </Typography>
                  <Typography variant="h4" sx={{ fontWeight: 700 }}>
                    {paymentMetrics?.totalTransactions.toLocaleString() || transactions.length}
                  </Typography>
                  <Typography variant="body2" sx={{ opacity: 0.8, mt: 1 }}>
                    Avg: {getCurrencyIcon(selectedCurrency)}
                    {paymentMetrics?.averageTransactionValue.toFixed(2) || '0'}
                  </Typography>
                </Box>
                <Receipt sx={{ fontSize: 48, opacity: 0.8 }} />
              </Box>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Card
            sx={{ background: 'linear-gradient(135deg, #fa709a 0%, #fee140 100%)', color: 'white' }}
          >
            <CardContent>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography color="inherit" gutterBottom variant="body2" sx={{ opacity: 0.8 }}>
                    SMS Revenue
                  </Typography>
                  <Typography variant="h4" sx={{ fontWeight: 700 }}>
                    ${smsUsage?.totalCost.toFixed(2) || '462.60'}
                  </Typography>
                  <Typography variant="body2" sx={{ opacity: 0.8, mt: 1 }}>
                    {smsUsage?.totalMessages.toLocaleString() || '15,420'} messages
                  </Typography>
                </Box>
                <Sms sx={{ fontSize: 48, opacity: 0.8 }} />
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Real-time Alerts */}
      {alerts.length > 0 && (
        <Card sx={{ mb: 3, border: '1px solid', borderColor: 'warning.main' }}>
          <CardContent>
            <Box display="flex" alignItems="center" justifyContent="between" sx={{ mb: 2 }}>
              <Typography variant="h6" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Warning color="warning" />
                Real-time Alerts
                <Badge badgeContent={unreadAlertsCount} color="error" />
              </Typography>
            </Box>
            <List dense>
              {alerts.slice(0, 3).map(alert => (
                <ListItem
                  key={alert.id}
                  sx={{
                    bgcolor: alert.isRead ? 'transparent' : 'action.hover',
                    borderRadius: 1,
                    mb: 1,
                  }}
                >
                  <ListItemIcon>
                    {alert.type === 'fraud_detected' && <Security color="error" />}
                    {alert.type === 'high_volume' && <TrendingUp color="warning" />}
                    {alert.type === 'failed_payment' && <Error color="error" />}
                    {alert.type === 'low_balance' && <Warning color="warning" />}
                  </ListItemIcon>
                  <ListItemText
                    primary={alert.message}
                    secondary={`${alert.severity.toUpperCase()} • ${new Date(alert.timestamp).toLocaleString()}`}
                  />
                  <Chip
                    label={alert.severity}
                    size="small"
                    color={
                      alert.severity === 'high'
                        ? 'error'
                        : alert.severity === 'medium'
                          ? 'warning'
                          : 'default'
                    }
                  />
                </ListItem>
              ))}
            </List>
          </CardContent>
        </Card>
      )}

      <Grid container spacing={3}>
        <Grid item xs={12} md={8}>
          <Paper sx={{ p: 3 }}>
            <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 3 }}>
              <Typography variant="h6">Recent Transactions</Typography>
              <Button variant="contained" startIcon={<Add />} onClick={() => {}}>
                New Transaction
              </Button>
            </Box>

            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Transaction ID</TableCell>
                    <TableCell>Amount</TableCell>
                    <TableCell>Merchant</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Date</TableCell>
                    <TableCell>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {transactions.map(transaction => (
                    <TableRow key={transaction.id}>
                      <TableCell>{transaction.id}</TableCell>
                      <TableCell>
                        ${transaction.amount.toLocaleString()} {transaction.currency}
                      </TableCell>
                      <TableCell>{transaction.merchant}</TableCell>
                      <TableCell>
                        <Chip
                          icon={getStatusIcon(transaction.status)}
                          label={transaction.status.toUpperCase()}
                          color={getStatusColor(transaction.status) as any}
                          size="small"
                        />
                      </TableCell>
                      <TableCell>{new Date(transaction.date).toLocaleString()}</TableCell>
                      <TableCell>
                        <IconButton size="small" onClick={() => handleViewTransaction(transaction)}>
                          <Visibility />
                        </IconButton>
                        <IconButton size="small">
                          <GetApp />
                        </IconButton>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </Paper>
        </Grid>

        <Grid item xs={12} md={4}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              Quick Actions
            </Typography>
            <Box display="flex" flexDirection="column" gap={2}>
              <Button variant="outlined" fullWidth startIcon={<Payment />}>
                Process Payment
              </Button>
              <Button variant="outlined" fullWidth startIcon={<GetApp />}>
                Export Transactions
              </Button>
              <Button variant="outlined" fullWidth startIcon={<TrendingUp />}>
                View Analytics
              </Button>
            </Box>
          </Paper>
        </Grid>
      </Grid>

      {/* Enhanced Transaction Details Dialog */}
      <Dialog open={openDialog} onClose={() => setOpenDialog(false)} maxWidth="md" fullWidth>
        <DialogTitle>
          <Box display="flex" alignItems="center" justifyContent="space-between">
            <Typography variant="h6">Transaction Details</Typography>
            {selectedTransaction && (
              <Chip
                icon={getStatusIcon(selectedTransaction.status)}
                label={selectedTransaction.status.toUpperCase()}
                color={getStatusColor(selectedTransaction.status) as any}
              />
            )}
          </Box>
        </DialogTitle>
        <DialogContent>
          {selectedTransaction && (
            <Box>
              <Grid container spacing={3}>
                <Grid item xs={12} md={6}>
                  <TextField
                    label="Transaction ID"
                    value={selectedTransaction.id}
                    fullWidth
                    disabled
                    sx={{ mb: 2 }}
                  />
                  <TextField
                    label="Reference"
                    value={selectedTransaction.reference}
                    fullWidth
                    disabled
                    sx={{ mb: 2 }}
                  />
                  <TextField
                    label="Amount"
                    value={`${selectedTransaction.amount.toLocaleString()} ${selectedTransaction.currency}`}
                    fullWidth
                    disabled
                    sx={{ mb: 2 }}
                  />
                  <TextField
                    label="Payment Method"
                    value={selectedTransaction.paymentMethod}
                    fullWidth
                    disabled
                    sx={{ mb: 2 }}
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    label="Customer Email"
                    value={selectedTransaction.customerEmail || 'N/A'}
                    fullWidth
                    disabled
                    sx={{ mb: 2 }}
                  />
                  <TextField
                    label="Country"
                    value={selectedTransaction.country || 'N/A'}
                    fullWidth
                    disabled
                    sx={{ mb: 2 }}
                  />
                  <TextField
                    label="Processing Time"
                    value={
                      selectedTransaction.processingTime
                        ? `${selectedTransaction.processingTime}s`
                        : 'N/A'
                    }
                    fullWidth
                    disabled
                    sx={{ mb: 2 }}
                  />
                  <TextField
                    label="Risk Score"
                    value={
                      selectedTransaction.riskScore ? `${selectedTransaction.riskScore}%` : 'N/A'
                    }
                    fullWidth
                    disabled
                    sx={{ mb: 2 }}
                  />
                </Grid>
                <Grid item xs={12}>
                  <TextField
                    label="Merchant"
                    value={selectedTransaction.merchant}
                    fullWidth
                    disabled
                    sx={{ mb: 2 }}
                  />
                  <TextField
                    label="Transaction Date"
                    value={new Date(selectedTransaction.date).toLocaleString()}
                    fullWidth
                    disabled
                  />
                </Grid>
                {selectedTransaction.fees && (
                  <Grid item xs={12}>
                    <Divider sx={{ my: 2 }} />
                    <Typography variant="h6" gutterBottom>
                      Financial Breakdown
                    </Typography>
                    <Box display="flex" justifyContent="space-between" sx={{ mb: 1 }}>
                      <Typography>Gross Amount:</Typography>
                      <Typography fontWeight="bold">
                        {selectedTransaction.amount.toLocaleString()} {selectedTransaction.currency}
                      </Typography>
                    </Box>
                    <Box display="flex" justifyContent="space-between" sx={{ mb: 1 }}>
                      <Typography>Processing Fees:</Typography>
                      <Typography color="error.main">
                        -{selectedTransaction.fees.toFixed(2)} {selectedTransaction.currency}
                      </Typography>
                    </Box>
                    <Box display="flex" justifyContent="space-between" sx={{ mb: 1 }}>
                      <Typography variant="h6">Net Amount:</Typography>
                      <Typography variant="h6" color="success.main">
                        {selectedTransaction.netAmount?.toFixed(2)} {selectedTransaction.currency}
                      </Typography>
                    </Box>
                  </Grid>
                )}
              </Grid>
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenDialog(false)}>Close</Button>
          {selectedTransaction?.status === 'completed' && (
            <Button variant="contained" startIcon={<Receipt />}>
              Download Receipt
            </Button>
          )}
        </DialogActions>
      </Dialog>

      {/* Snackbar for notifications */}
      <Snackbar
        open={snackbar.open}
        autoHideDuration={6000}
        onClose={handleCloseSnackbar}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
      >
        <Alert onClose={handleCloseSnackbar} severity={snackbar.severity} sx={{ width: '100%' }}>
          {snackbar.message}
        </Alert>
      </Snackbar>
    </Container>
  );
};

export default PaymentGateway;
