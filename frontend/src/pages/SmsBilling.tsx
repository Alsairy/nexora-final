import {
  Sms,
  TrendingUp,
  TrendingDown,
  Warning,
  CheckCircle,
  Error,
  Download,
  Refresh,
  Settings,
  Analytics,
  Payment,
  Receipt,
  Notifications,
  AccountBalance,
  CreditCard,
  MonetizationOn,
  ExpandMore,
  Visibility,
  Add,
  Search,
  FilterList,
  DateRange,
  PieChart,
  BarChart,
  Timeline,
} from '@mui/icons-material';
import {
  Container,
  Typography,
  Box,
  Grid,
  Card,
  CardContent,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Chip,
  Button,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  LinearProgress,
  Alert,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Tabs,
  Tab,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Divider,
  Switch,
  FormControlLabel,
  IconButton,
  Tooltip,
  Badge,
  CircularProgress,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Snackbar,
} from '@mui/material';
import React, { useState, useEffect } from 'react';

interface SmsUsageData {
  totalMessages: number;
  totalCost: number;
  averageCostPerMessage: number;
  successRate: number;
  usageByCountry: Record<string, number>;
  usageByType: Record<string, number>;
  dailyUsage: Array<{
    date: string;
    messages: number;
    cost: number;
  }>;
}

interface BillingCycle {
  id: string;
  startDate: string;
  endDate: string;
  totalMessages: number;
  totalCost: number;
  status: 'active' | 'completed' | 'pending';
}

interface Invoice {
  id: string;
  invoiceNumber: string;
  billingCycleId: string;
  amount: number;
  currency: string;
  status: 'paid' | 'pending' | 'overdue';
  issueDate: string;
  dueDate: string;
  downloadUrl?: string;
}

interface UsageAlert {
  id: string;
  type: 'balance' | 'usage' | 'cost';
  threshold: number;
  currentValue: number;
  isActive: boolean;
  message: string;
  severity: 'info' | 'warning' | 'error';
}

interface PaymentMethod {
  id: string;
  type: 'credit_card' | 'bank_account' | 'wallet';
  last4: string;
  expiryDate?: string;
  isDefault: boolean;
  status: 'active' | 'expired' | 'pending';
}

const SmsBilling: React.FC = () => {
  const [activeTab, setActiveTab] = useState(0);
  const [loading, setLoading] = useState(false);
  const [selectedPeriod, setSelectedPeriod] = useState('current_month');
  const [smsUsage, setSmsUsage] = useState<SmsUsageData | null>(null);
  const [billingCycles, setBillingCycles] = useState<BillingCycle[]>([]);
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [usageAlerts, setUsageAlerts] = useState<UsageAlert[]>([]);
  const [paymentMethods, setPaymentMethods] = useState<PaymentMethod[]>([]);
  const [selectedInvoice, setSelectedInvoice] = useState<Invoice | null>(null);
  const [invoiceDialogOpen, setInvoiceDialogOpen] = useState(false);
  const [snackbar, setSnackbar] = useState({
    open: false,
    message: '',
    severity: 'success' as 'success' | 'error' | 'warning' | 'info',
  });

  const fetchSmsUsageData = async () => {
    setLoading(true);
    setTimeout(() => {
      const mockUsageData: SmsUsageData = {
        totalMessages: 15420,
        totalCost: 462.6,
        averageCostPerMessage: 0.03,
        successRate: 98.5,
        usageByCountry: {
          'Saudi Arabia': 8500,
          UAE: 3200,
          Kuwait: 2100,
          Bahrain: 980,
          Qatar: 640,
        },
        usageByType: {
          Transactional: 12500,
          Marketing: 2420,
          OTP: 500,
        },
        dailyUsage: Array.from({ length: 30 }, (_, i) => {
          const date = new Date(Date.now() - (29 - i) * 24 * 60 * 60 * 1000);
          const dateString = date.toISOString().split('T')[0];
          return {
            date: dateString!,
            messages: Math.floor(Math.random() * 800) + 200,
            cost: Math.floor(Math.random() * 25) + 5,
          };
        }),
      };
      setSmsUsage(mockUsageData);
      setLoading(false);
    }, 1000);
  };

  const fetchBillingCycles = async () => {
    const mockCycles: BillingCycle[] = [
      {
        id: '1',
        startDate: '2024-01-01',
        endDate: '2024-01-31',
        totalMessages: 15420,
        totalCost: 462.6,
        status: 'active',
      },
      {
        id: '2',
        startDate: '2023-12-01',
        endDate: '2023-12-31',
        totalMessages: 18200,
        totalCost: 546.0,
        status: 'completed',
      },
    ];
    setBillingCycles(mockCycles);
  };

  const fetchInvoices = async () => {
    const mockInvoices: Invoice[] = [
      {
        id: '1',
        invoiceNumber: 'INV-2024-001',
        billingCycleId: '1',
        amount: 462.6,
        currency: 'SAR',
        status: 'paid',
        issueDate: '2024-02-01',
        dueDate: '2024-02-15',
        downloadUrl: '/invoices/INV-2024-001.pdf',
      },
      {
        id: '2',
        invoiceNumber: 'INV-2023-012',
        billingCycleId: '2',
        amount: 546.0,
        currency: 'SAR',
        status: 'paid',
        issueDate: '2024-01-01',
        dueDate: '2024-01-15',
        downloadUrl: '/invoices/INV-2023-012.pdf',
      },
    ];
    setInvoices(mockInvoices);
  };

  const fetchUsageAlerts = async () => {
    const mockAlerts: UsageAlert[] = [
      {
        id: '1',
        type: 'balance',
        threshold: 100,
        currentValue: 85,
        isActive: true,
        message: 'Account balance is running low',
        severity: 'warning',
      },
      {
        id: '2',
        type: 'usage',
        threshold: 10000,
        currentValue: 8500,
        isActive: true,
        message: 'Monthly SMS usage approaching limit',
        severity: 'info',
      },
    ];
    setUsageAlerts(mockAlerts);
  };

  const fetchPaymentMethods = async () => {
    const mockPaymentMethods: PaymentMethod[] = [
      {
        id: '1',
        type: 'credit_card',
        last4: '4242',
        expiryDate: '12/25',
        isDefault: true,
        status: 'active',
      },
      {
        id: '2',
        type: 'bank_account',
        last4: '1234',
        isDefault: false,
        status: 'active',
      },
    ];
    setPaymentMethods(mockPaymentMethods);
  };

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  const handleViewInvoice = (invoice: Invoice) => {
    setSelectedInvoice(invoice);
    setInvoiceDialogOpen(true);
  };

  const handleDownloadInvoice = (invoice: Invoice) => {
    setSnackbar({
      open: true,
      message: `Downloading invoice ${invoice.invoiceNumber}...`,
      severity: 'info',
    });
  };

  const handleRefreshData = () => {
    fetchSmsUsageData();
    fetchBillingCycles();
    fetchInvoices();
    fetchUsageAlerts();
    fetchPaymentMethods();
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'active':
      case 'paid':
        return 'success';
      case 'pending':
        return 'warning';
      case 'overdue':
      case 'expired':
        return 'error';
      default:
        return 'default';
    }
  };

  const getAlertIcon = (severity: string) => {
    switch (severity) {
      case 'error':
        return <Error color="error" />;
      case 'warning':
        return <Warning color="warning" />;
      case 'info':
        return <CheckCircle color="info" />;
      default:
        return <CheckCircle color="success" />;
    }
  };

  const formatCurrency = (amount: number, currency = 'SAR') => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency,
    }).format(amount);
  };

  useEffect(() => {
    handleRefreshData();
  }, [selectedPeriod]);

  return (
    <Container maxWidth="xl">
      <Box sx={{ mb: 4 }}>
        <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
          <Typography variant="h4" component="h1" sx={{ fontWeight: 700 }}>
            SMS Billing & Usage
          </Typography>
          <Box display="flex" gap={2}>
            <FormControl size="small" sx={{ minWidth: 150 }}>
              <InputLabel>Period</InputLabel>
              <Select
                value={selectedPeriod}
                label="Period"
                onChange={e => setSelectedPeriod(e.target.value)}
              >
                <MenuItem value="current_month">Current Month</MenuItem>
                <MenuItem value="last_month">Last Month</MenuItem>
                <MenuItem value="last_3_months">Last 3 Months</MenuItem>
                <MenuItem value="last_6_months">Last 6 Months</MenuItem>
              </Select>
            </FormControl>
            <Button
              variant="outlined"
              startIcon={<Refresh />}
              onClick={handleRefreshData}
              disabled={loading}
            >
              Refresh
            </Button>
          </Box>
        </Box>

        <Tabs value={activeTab} onChange={handleTabChange} sx={{ mb: 3 }}>
          <Tab icon={<Analytics />} label="Usage Analytics" />
          <Tab icon={<Receipt />} label="Billing History" />
          <Tab icon={<Notifications />} label="Usage Alerts" />
          <Tab icon={<Payment />} label="Payment Methods" />
        </Tabs>
      </Box>

      {loading && <LinearProgress sx={{ mb: 2 }} />}

      {/* Usage Analytics Tab */}
      {activeTab === 0 && (
        <>
          {/* Usage Overview Cards */}
          <Grid container spacing={3} sx={{ mb: 4 }}>
            <Grid item xs={12} sm={6} md={3}>
              <Card
                sx={{
                  background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                  color: 'white',
                }}
              >
                <CardContent>
                  <Box display="flex" alignItems="center" justifyContent="space-between">
                    <Box>
                      <Typography color="inherit" gutterBottom variant="h6">
                        Total Messages
                      </Typography>
                      <Typography variant="h4">
                        {smsUsage?.totalMessages.toLocaleString() || '0'}
                      </Typography>
                    </Box>
                    <Sms sx={{ fontSize: 40, opacity: 0.8 }} />
                  </Box>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Card
                sx={{
                  background: 'linear-gradient(135deg, #f093fb 0%, #f5576c 100%)',
                  color: 'white',
                }}
              >
                <CardContent>
                  <Box display="flex" alignItems="center" justifyContent="space-between">
                    <Box>
                      <Typography color="inherit" gutterBottom variant="h6">
                        Total Cost
                      </Typography>
                      <Typography variant="h4">
                        {smsUsage ? formatCurrency(smsUsage.totalCost) : formatCurrency(0)}
                      </Typography>
                    </Box>
                    <MonetizationOn sx={{ fontSize: 40, opacity: 0.8 }} />
                  </Box>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Card
                sx={{
                  background: 'linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)',
                  color: 'white',
                }}
              >
                <CardContent>
                  <Box display="flex" alignItems="center" justifyContent="space-between">
                    <Box>
                      <Typography color="inherit" gutterBottom variant="h6">
                        Success Rate
                      </Typography>
                      <Typography variant="h4">{smsUsage?.successRate || 0}%</Typography>
                    </Box>
                    <TrendingUp sx={{ fontSize: 40, opacity: 0.8 }} />
                  </Box>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Card
                sx={{
                  background: 'linear-gradient(135deg, #43e97b 0%, #38f9d7 100%)',
                  color: 'white',
                }}
              >
                <CardContent>
                  <Box display="flex" alignItems="center" justifyContent="space-between">
                    <Box>
                      <Typography color="inherit" gutterBottom variant="h6">
                        Avg Cost/SMS
                      </Typography>
                      <Typography variant="h4">
                        {smsUsage
                          ? formatCurrency(smsUsage.averageCostPerMessage)
                          : formatCurrency(0)}
                      </Typography>
                    </Box>
                    <BarChart sx={{ fontSize: 40, opacity: 0.8 }} />
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          </Grid>

          {/* Usage Breakdown */}
          <Grid container spacing={3} sx={{ mb: 4 }}>
            <Grid item xs={12} md={6}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    Usage by Country
                  </Typography>
                  {smsUsage?.usageByCountry &&
                    Object.entries(smsUsage.usageByCountry).map(([country, count]) => (
                      <Box key={country} sx={{ mb: 2 }}>
                        <Box display="flex" justifyContent="space-between" alignItems="center">
                          <Typography variant="body2">{country}</Typography>
                          <Typography variant="body2" fontWeight="bold">
                            {count.toLocaleString()}
                          </Typography>
                        </Box>
                        <LinearProgress
                          variant="determinate"
                          value={(count / (smsUsage?.totalMessages || 1)) * 100}
                          sx={{ mt: 1 }}
                        />
                      </Box>
                    ))}
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={12} md={6}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    Usage by Message Type
                  </Typography>
                  {smsUsage?.usageByType &&
                    Object.entries(smsUsage.usageByType).map(([type, count]) => (
                      <Box key={type} sx={{ mb: 2 }}>
                        <Box display="flex" justifyContent="space-between" alignItems="center">
                          <Typography variant="body2">{type}</Typography>
                          <Typography variant="body2" fontWeight="bold">
                            {count.toLocaleString()}
                          </Typography>
                        </Box>
                        <LinearProgress
                          variant="determinate"
                          value={(count / (smsUsage?.totalMessages || 1)) * 100}
                          sx={{ mt: 1 }}
                          color="secondary"
                        />
                      </Box>
                    ))}
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        </>
      )}

      {/* Billing History Tab */}
      {activeTab === 1 && (
        <>
          {/* Current Billing Cycle */}
          <Card sx={{ mb: 4 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Current Billing Cycle
              </Typography>
              {billingCycles.length > 0 && billingCycles[0] && (
                <Grid container spacing={3}>
                  <Grid item xs={12} md={4}>
                    <Box textAlign="center">
                      <Typography variant="h3" color="primary">
                        {formatCurrency(billingCycles[0].totalCost)}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        Current Usage
                      </Typography>
                    </Box>
                  </Grid>
                  <Grid item xs={12} md={4}>
                    <Box textAlign="center">
                      <Typography variant="h3" color="info.main">
                        {billingCycles[0].totalMessages.toLocaleString()}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        Messages Sent
                      </Typography>
                    </Box>
                  </Grid>
                  <Grid item xs={12} md={4}>
                    <Box textAlign="center">
                      <Typography variant="h3" color="success.main">
                        {Math.max(
                          0,
                          new Date(billingCycles[0].endDate).getDate() - new Date().getDate(),
                        )}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        Days Remaining
                      </Typography>
                    </Box>
                  </Grid>
                </Grid>
              )}
            </CardContent>
          </Card>

          {/* Invoice History */}
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Invoice History
              </Typography>
              <TableContainer>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>Invoice Number</TableCell>
                      <TableCell>Amount</TableCell>
                      <TableCell>Status</TableCell>
                      <TableCell>Issue Date</TableCell>
                      <TableCell>Due Date</TableCell>
                      <TableCell>Actions</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {invoices.map(invoice => (
                      <TableRow key={invoice.id}>
                        <TableCell>{invoice.invoiceNumber}</TableCell>
                        <TableCell>{formatCurrency(invoice.amount, invoice.currency)}</TableCell>
                        <TableCell>
                          <Chip
                            label={invoice.status.toUpperCase()}
                            color={getStatusColor(invoice.status) as any}
                            size="small"
                          />
                        </TableCell>
                        <TableCell>{new Date(invoice.issueDate).toLocaleDateString()}</TableCell>
                        <TableCell>{new Date(invoice.dueDate).toLocaleDateString()}</TableCell>
                        <TableCell>
                          <IconButton
                            size="small"
                            onClick={() => handleViewInvoice(invoice)}
                            sx={{ mr: 1 }}
                          >
                            <Visibility />
                          </IconButton>
                          <IconButton size="small" onClick={() => handleDownloadInvoice(invoice)}>
                            <Download />
                          </IconButton>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            </CardContent>
          </Card>
        </>
      )}

      {/* Usage Alerts Tab */}
      {activeTab === 2 && (
        <Grid container spacing={3}>
          <Grid item xs={12} md={8}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Active Alerts
                </Typography>
                <List>
                  {usageAlerts.map(alert => (
                    <React.Fragment key={alert.id}>
                      <ListItem>
                        <ListItemIcon>{getAlertIcon(alert.severity)}</ListItemIcon>
                        <ListItemText
                          primary={alert.message}
                          secondary={`Threshold: ${alert.threshold} | Current: ${alert.currentValue}`}
                        />
                        <Switch
                          checked={alert.isActive}
                          onChange={() => {
                            setSnackbar({
                              open: true,
                              message: `Alert ${alert.isActive ? 'disabled' : 'enabled'}`,
                              severity: 'info',
                            });
                          }}
                        />
                      </ListItem>
                      <Divider />
                    </React.Fragment>
                  ))}
                </List>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} md={4}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Create New Alert
                </Typography>
                <Box component="form" sx={{ mt: 2 }}>
                  <FormControl fullWidth sx={{ mb: 2 }}>
                    <InputLabel>Alert Type</InputLabel>
                    <Select defaultValue="" label="Alert Type">
                      <MenuItem value="balance">Balance Alert</MenuItem>
                      <MenuItem value="usage">Usage Alert</MenuItem>
                      <MenuItem value="cost">Cost Alert</MenuItem>
                    </Select>
                  </FormControl>
                  <TextField fullWidth label="Threshold Value" type="number" sx={{ mb: 2 }} />
                  <Button variant="contained" fullWidth startIcon={<Add />}>
                    Create Alert
                  </Button>
                </Box>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      )}

      {/* Payment Methods Tab */}
      {activeTab === 3 && (
        <Grid container spacing={3}>
          <Grid item xs={12} md={8}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Payment Methods
                </Typography>
                <List>
                  {paymentMethods.map(method => (
                    <React.Fragment key={method.id}>
                      <ListItem>
                        <ListItemIcon>
                          {method.type === 'credit_card' ? <CreditCard /> : <AccountBalance />}
                        </ListItemIcon>
                        <ListItemText
                          primary={`${method.type === 'credit_card' ? 'Credit Card' : 'Bank Account'} ending in ${method.last4}`}
                          secondary={
                            <Box>
                              {method.expiryDate && `Expires: ${method.expiryDate}`}
                              {method.isDefault && (
                                <Chip label="Default" size="small" color="primary" sx={{ ml: 1 }} />
                              )}
                            </Box>
                          }
                        />
                        <Chip
                          label={method.status.toUpperCase()}
                          color={getStatusColor(method.status) as any}
                          size="small"
                        />
                      </ListItem>
                      <Divider />
                    </React.Fragment>
                  ))}
                </List>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} md={4}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Add Payment Method
                </Typography>
                <Box component="form" sx={{ mt: 2 }}>
                  <FormControl fullWidth sx={{ mb: 2 }}>
                    <InputLabel>Payment Type</InputLabel>
                    <Select defaultValue="" label="Payment Type">
                      <MenuItem value="credit_card">Credit Card</MenuItem>
                      <MenuItem value="bank_account">Bank Account</MenuItem>
                    </Select>
                  </FormControl>
                  <Button variant="contained" fullWidth startIcon={<Add />}>
                    Add Payment Method
                  </Button>
                </Box>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      )}

      {/* Invoice Details Dialog */}
      <Dialog
        open={invoiceDialogOpen}
        onClose={() => setInvoiceDialogOpen(false)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>Invoice Details - {selectedInvoice?.invoiceNumber}</DialogTitle>
        <DialogContent>
          {selectedInvoice && (
            <Grid container spacing={2}>
              <Grid item xs={6}>
                <Typography variant="body2" color="text.secondary">
                  Amount
                </Typography>
                <Typography variant="h6">
                  {formatCurrency(selectedInvoice.amount, selectedInvoice.currency)}
                </Typography>
              </Grid>
              <Grid item xs={6}>
                <Typography variant="body2" color="text.secondary">
                  Status
                </Typography>
                <Chip
                  label={selectedInvoice.status.toUpperCase()}
                  color={getStatusColor(selectedInvoice.status) as any}
                />
              </Grid>
              <Grid item xs={6}>
                <Typography variant="body2" color="text.secondary">
                  Issue Date
                </Typography>
                <Typography variant="body1">
                  {new Date(selectedInvoice.issueDate).toLocaleDateString()}
                </Typography>
              </Grid>
              <Grid item xs={6}>
                <Typography variant="body2" color="text.secondary">
                  Due Date
                </Typography>
                <Typography variant="body1">
                  {new Date(selectedInvoice.dueDate).toLocaleDateString()}
                </Typography>
              </Grid>
            </Grid>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setInvoiceDialogOpen(false)}>Close</Button>
          {selectedInvoice && (
            <Button
              variant="contained"
              startIcon={<Download />}
              onClick={() => handleDownloadInvoice(selectedInvoice)}
            >
              Download
            </Button>
          )}
        </DialogActions>
      </Dialog>

      {/* Snackbar for notifications */}
      <Snackbar
        open={snackbar.open}
        autoHideDuration={6000}
        onClose={() => setSnackbar({ ...snackbar, open: false })}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
      >
        <Alert
          onClose={() => setSnackbar({ ...snackbar, open: false })}
          severity={snackbar.severity}
          sx={{ width: '100%' }}
        >
          {snackbar.message}
        </Alert>
      </Snackbar>
    </Container>
  );
};

export default SmsBilling;
