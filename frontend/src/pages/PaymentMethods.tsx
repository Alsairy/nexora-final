import React, { useState, useEffect } from 'react';
import {
  Container,
  Box,
  Typography,
  Card,
  CardContent,
  Grid,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Chip,
  IconButton,
  List,
  ListItem,
  ListItemText,
  ListItemSecondaryAction,
  Switch,
  FormControlLabel,
  Alert,
  Snackbar,
  Divider,
  Paper,
  Tab,
  Tabs,
  CircularProgress,
  Tooltip,
  Badge
} from '@mui/material';
import {
  Add as AddIcon,
  CreditCard as CreditCardIcon,
  Delete as DeleteIcon,
  Edit as EditIcon,
  Security as SecurityIcon,
  AccountBalance as BankIcon,
  Payment as PaymentIcon,
  Verified as VerifiedIcon,
  Warning as WarningIcon,
  Star as StarIcon,
  StarBorder as StarBorderIcon,
  Refresh as RefreshIcon,
  Lock as LockIcon,
  CheckCircle as CheckCircleIcon,
  Error as ErrorIcon
} from '@mui/icons-material';

interface PaymentMethod {
  id: string;
  type: 'credit_card' | 'debit_card' | 'bank_account' | 'digital_wallet';
  name: string;
  lastFour: string;
  expiryDate?: string;
  brand?: string;
  isDefault: boolean;
  isVerified: boolean;
  status: 'active' | 'expired' | 'blocked' | 'pending_verification';
  addedDate: string;
  lastUsed?: string;
  billingAddress: {
    street: string;
    city: string;
    state: string;
    zipCode: string;
    country: string;
  };
}

interface PaymentMethodForm {
  type: 'credit_card' | 'debit_card' | 'bank_account' | 'digital_wallet';
  cardNumber: string;
  expiryMonth: string;
  expiryYear: string;
  cvv: string;
  cardholderName: string;
  billingAddress: {
    street: string;
    city: string;
    state: string;
    zipCode: string;
    country: string;
  };
}

interface VerificationStatus {
  isVerifying: boolean;
  verificationMethod: 'micro_deposits' | 'instant' | 'manual';
  estimatedTime: string;
  instructions: string;
}

const PaymentMethods: React.FC = () => {
  const [paymentMethods, setPaymentMethods] = useState<PaymentMethod[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState(0);
  const [addMethodDialog, setAddMethodDialog] = useState(false);
  const [editMethodDialog, setEditMethodDialog] = useState(false);
  const [verificationDialog, setVerificationDialog] = useState(false);
  const [selectedMethod, setSelectedMethod] = useState<PaymentMethod | null>(null);
  const [verificationStatus, setVerificationStatus] = useState<VerificationStatus | null>(null);
  const [formData, setFormData] = useState<PaymentMethodForm>({
    type: 'credit_card',
    cardNumber: '',
    expiryMonth: '',
    expiryYear: '',
    cvv: '',
    cardholderName: '',
    billingAddress: {
      street: '',
      city: '',
      state: '',
      zipCode: '',
      country: 'SA'
    }
  });
  const [snackbar, setSnackbar] = useState({
    open: false,
    message: '',
    severity: 'success' as 'success' | 'error' | 'warning' | 'info'
  });

  const fetchPaymentMethods = async () => {
    setLoading(true);
    setTimeout(() => {
      const mockPaymentMethods: PaymentMethod[] = [
        {
          id: '1',
          type: 'credit_card',
          name: 'Visa Credit Card',
          lastFour: '4242',
          expiryDate: '12/25',
          brand: 'Visa',
          isDefault: true,
          isVerified: true,
          status: 'active',
          addedDate: '2024-01-15',
          lastUsed: '2024-06-18',
          billingAddress: {
            street: '123 King Fahd Road',
            city: 'Riyadh',
            state: 'Riyadh Province',
            zipCode: '11564',
            country: 'SA'
          }
        },
        {
          id: '2',
          type: 'credit_card',
          name: 'Mastercard Credit Card',
          lastFour: '5555',
          expiryDate: '08/26',
          brand: 'Mastercard',
          isDefault: false,
          isVerified: true,
          status: 'active',
          addedDate: '2024-02-20',
          lastUsed: '2024-06-10',
          billingAddress: {
            street: '456 Prince Sultan Street',
            city: 'Jeddah',
            state: 'Makkah Province',
            zipCode: '21589',
            country: 'SA'
          }
        },
        {
          id: '3',
          type: 'bank_account',
          name: 'Saudi National Bank',
          lastFour: '7890',
          isDefault: false,
          isVerified: false,
          status: 'pending_verification',
          addedDate: '2024-06-15',
          billingAddress: {
            street: '789 Al Olaya Street',
            city: 'Riyadh',
            state: 'Riyadh Province',
            zipCode: '11564',
            country: 'SA'
          }
        },
        {
          id: '4',
          type: 'digital_wallet',
          name: 'Apple Pay',
          lastFour: '1234',
          isDefault: false,
          isVerified: true,
          status: 'active',
          addedDate: '2024-03-10',
          lastUsed: '2024-06-17',
          billingAddress: {
            street: '321 Tahlia Street',
            city: 'Riyadh',
            state: 'Riyadh Province',
            zipCode: '11564',
            country: 'SA'
          }
        }
      ];
      setPaymentMethods(mockPaymentMethods);
      setLoading(false);
    }, 1000);
  };

  const handleAddPaymentMethod = async () => {
    setLoading(true);
    setTimeout(() => {
      const newMethod: PaymentMethod = {
        id: Date.now().toString(),
        type: formData.type,
        name: getPaymentMethodName(formData.type, formData.cardNumber),
        lastFour: formData.cardNumber.slice(-4),
        expiryDate: formData.type === 'credit_card' || formData.type === 'debit_card' 
          ? `${formData.expiryMonth}/${formData.expiryYear.slice(-2)}` 
          : undefined,
        brand: getBrandFromCardNumber(formData.cardNumber),
        isDefault: paymentMethods.length === 0,
        isVerified: formData.type === 'digital_wallet',
        status: formData.type === 'digital_wallet' ? 'active' : 'pending_verification',
        addedDate: new Date().toISOString().split('T')[0],
        billingAddress: formData.billingAddress
      };
      
      setPaymentMethods([...paymentMethods, newMethod]);
      setAddMethodDialog(false);
      resetForm();
      setSnackbar({
        open: true,
        message: 'Payment method added successfully',
        severity: 'success'
      });
      setLoading(false);
    }, 1500);
  };

  const handleSetDefault = async (methodId: string) => {
    const updatedMethods = paymentMethods.map(method => ({
      ...method,
      isDefault: method.id === methodId
    }));
    setPaymentMethods(updatedMethods);
    setSnackbar({
      open: true,
      message: 'Default payment method updated',
      severity: 'success'
    });
  };

  const handleDeleteMethod = async (methodId: string) => {
    const updatedMethods = paymentMethods.filter(method => method.id !== methodId);
    setPaymentMethods(updatedMethods);
    setSnackbar({
      open: true,
      message: 'Payment method removed',
      severity: 'success'
    });
  };

  const handleVerifyMethod = async (method: PaymentMethod) => {
    setSelectedMethod(method);
    setVerificationStatus({
      isVerifying: true,
      verificationMethod: method.type === 'bank_account' ? 'micro_deposits' : 'instant',
      estimatedTime: method.type === 'bank_account' ? '1-2 business days' : '2-3 minutes',
      instructions: method.type === 'bank_account' 
        ? 'We will send small deposits to your account. Please verify the amounts when they appear.'
        : 'Please confirm the verification code sent to your registered mobile number.'
    });
    setVerificationDialog(true);

    setTimeout(() => {
      const updatedMethods = paymentMethods.map(m => 
        m.id === method.id 
          ? { ...m, isVerified: true, status: 'active' as const }
          : m
      );
      setPaymentMethods(updatedMethods);
      setVerificationStatus(prev => prev ? { ...prev, isVerifying: false } : null);
      setSnackbar({
        open: true,
        message: 'Payment method verified successfully',
        severity: 'success'
      });
    }, 3000);
  };

  const getPaymentMethodName = (type: string, cardNumber: string): string => {
    const brand = getBrandFromCardNumber(cardNumber);
    switch (type) {
      case 'credit_card':
        return `${brand} Credit Card`;
      case 'debit_card':
        return `${brand} Debit Card`;
      case 'bank_account':
        return 'Bank Account';
      case 'digital_wallet':
        return 'Digital Wallet';
      default:
        return 'Payment Method';
    }
  };

  const getBrandFromCardNumber = (cardNumber: string): string => {
    const number = cardNumber.replace(/\s/g, '');
    if (number.startsWith('4')) return 'Visa';
    if (number.startsWith('5') || number.startsWith('2')) return 'Mastercard';
    if (number.startsWith('3')) return 'American Express';
    return 'Unknown';
  };

  const getPaymentMethodIcon = (type: string, brand?: string) => {
    switch (type) {
      case 'credit_card':
      case 'debit_card':
        return <CreditCardIcon />;
      case 'bank_account':
        return <BankIcon />;
      case 'digital_wallet':
        return <PaymentIcon />;
      default:
        return <PaymentIcon />;
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'active':
        return 'success';
      case 'pending_verification':
        return 'warning';
      case 'expired':
      case 'blocked':
        return 'error';
      default:
        return 'default';
    }
  };

  const getStatusText = (status: string) => {
    switch (status) {
      case 'active':
        return 'Active';
      case 'pending_verification':
        return 'Pending Verification';
      case 'expired':
        return 'Expired';
      case 'blocked':
        return 'Blocked';
      default:
        return status;
    }
  };

  const resetForm = () => {
    setFormData({
      type: 'credit_card',
      cardNumber: '',
      expiryMonth: '',
      expiryYear: '',
      cvv: '',
      cardholderName: '',
      billingAddress: {
        street: '',
        city: '',
        state: '',
        zipCode: '',
        country: 'SA'
      }
    });
  };

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  const formatCardNumber = (value: string) => {
    const v = value.replace(/\s+/g, '').replace(/[^0-9]/gi, '');
    const matches = v.match(/\d{4,16}/g);
    const match = matches && matches[0] || '';
    const parts = [];
    for (let i = 0, len = match.length; i < len; i += 4) {
      parts.push(match.substring(i, i + 4));
    }
    if (parts.length) {
      return parts.join(' ');
    } else {
      return v;
    }
  };

  const handleCardNumberChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const formatted = formatCardNumber(e.target.value);
    setFormData({ ...formData, cardNumber: formatted });
  };

  useEffect(() => {
    fetchPaymentMethods();
  }, []);

  const filteredMethods = paymentMethods.filter(method => {
    switch (activeTab) {
      case 0: return true; // All
      case 1: return method.type === 'credit_card' || method.type === 'debit_card';
      case 2: return method.type === 'bank_account';
      case 3: return method.type === 'digital_wallet';
      default: return true;
    }
  });

  return (
    <Container maxWidth="xl">
      <Box sx={{ mb: 4 }}>
        <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
          <Typography variant="h4" component="h1" sx={{ fontWeight: 700 }}>
            Payment Methods
          </Typography>
          <Box display="flex" gap={2}>
            <Button
              variant="outlined"
              startIcon={<RefreshIcon />}
              onClick={fetchPaymentMethods}
              disabled={loading}
            >
              Refresh
            </Button>
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={() => setAddMethodDialog(true)}
              sx={{
                background: 'linear-gradient(45deg, #2196F3 30%, #21CBF3 90%)',
                boxShadow: '0 3px 5px 2px rgba(33, 203, 243, .3)',
              }}
            >
              Add Payment Method
            </Button>
          </Box>
        </Box>

        <Tabs value={activeTab} onChange={handleTabChange} sx={{ mb: 3 }}>
          <Tab label={`All (${paymentMethods.length})`} />
          <Tab label={`Cards (${paymentMethods.filter(m => m.type === 'credit_card' || m.type === 'debit_card').length})`} />
          <Tab label={`Bank Accounts (${paymentMethods.filter(m => m.type === 'bank_account').length})`} />
          <Tab label={`Digital Wallets (${paymentMethods.filter(m => m.type === 'digital_wallet').length})`} />
        </Tabs>

        {loading ? (
          <Box display="flex" justifyContent="center" py={4}>
            <CircularProgress />
          </Box>
        ) : (
          <Grid container spacing={3}>
            {filteredMethods.map((method) => (
              <Grid item xs={12} md={6} lg={4} key={method.id}>
                <Card 
                  sx={{ 
                    height: '100%',
                    position: 'relative',
                    border: method.isDefault ? '2px solid #2196F3' : '1px solid #e0e0e0',
                    '&:hover': {
                      boxShadow: '0 4px 20px rgba(0,0,0,0.1)',
                      transform: 'translateY(-2px)',
                      transition: 'all 0.3s ease'
                    }
                  }}
                >
                  {method.isDefault && (
                    <Box
                      sx={{
                        position: 'absolute',
                        top: 8,
                        right: 8,
                        background: 'linear-gradient(45deg, #2196F3 30%, #21CBF3 90%)',
                        color: 'white',
                        px: 1,
                        py: 0.5,
                        borderRadius: 1,
                        fontSize: '0.75rem',
                        fontWeight: 600
                      }}
                    >
                      DEFAULT
                    </Box>
                  )}
                  
                  <CardContent>
                    <Box display="flex" alignItems="center" mb={2}>
                      {getPaymentMethodIcon(method.type, method.brand)}
                      <Box ml={2} flex={1}>
                        <Typography variant="h6" sx={{ fontWeight: 600 }}>
                          {method.name}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          •••• •••• •••• {method.lastFour}
                        </Typography>
                      </Box>
                      <Box display="flex" alignItems="center" gap={1}>
                        {method.isVerified ? (
                          <Tooltip title="Verified">
                            <VerifiedIcon color="success" fontSize="small" />
                          </Tooltip>
                        ) : (
                          <Tooltip title="Pending Verification">
                            <WarningIcon color="warning" fontSize="small" />
                          </Tooltip>
                        )}
                        <Chip
                          label={getStatusText(method.status)}
                          color={getStatusColor(method.status) as any}
                          size="small"
                        />
                      </Box>
                    </Box>

                    {method.expiryDate && (
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                        Expires: {method.expiryDate}
                      </Typography>
                    )}

                    <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                      Added: {new Date(method.addedDate).toLocaleDateString()}
                    </Typography>

                    {method.lastUsed && (
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        Last used: {new Date(method.lastUsed).toLocaleDateString()}
                      </Typography>
                    )}

                    <Divider sx={{ my: 2 }} />

                    <Box display="flex" justifyContent="space-between" alignItems="center">
                      <Box display="flex" gap={1}>
                        {!method.isDefault && method.isVerified && (
                          <Tooltip title="Set as Default">
                            <IconButton
                              size="small"
                              onClick={() => handleSetDefault(method.id)}
                              sx={{ color: '#2196F3' }}
                            >
                              <StarBorderIcon fontSize="small" />
                            </IconButton>
                          </Tooltip>
                        )}
                        
                        {!method.isVerified && (
                          <Button
                            size="small"
                            variant="outlined"
                            startIcon={<SecurityIcon />}
                            onClick={() => handleVerifyMethod(method)}
                            sx={{ fontSize: '0.75rem' }}
                          >
                            Verify
                          </Button>
                        )}
                      </Box>

                      <Box display="flex" gap={1}>
                        <Tooltip title="Edit">
                          <IconButton
                            size="small"
                            onClick={() => {
                              setSelectedMethod(method);
                              setEditMethodDialog(true);
                            }}
                          >
                            <EditIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                        
                        <Tooltip title="Delete">
                          <IconButton
                            size="small"
                            onClick={() => handleDeleteMethod(method.id)}
                            sx={{ color: '#f44336' }}
                          >
                            <DeleteIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      </Box>
                    </Box>
                  </CardContent>
                </Card>
              </Grid>
            ))}

            {filteredMethods.length === 0 && (
              <Grid item xs={12}>
                <Paper sx={{ p: 4, textAlign: 'center' }}>
                  <PaymentIcon sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
                  <Typography variant="h6" color="text.secondary" gutterBottom>
                    No payment methods found
                  </Typography>
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
                    Add your first payment method to get started
                  </Typography>
                  <Button
                    variant="contained"
                    startIcon={<AddIcon />}
                    onClick={() => setAddMethodDialog(true)}
                  >
                    Add Payment Method
                  </Button>
                </Paper>
              </Grid>
            )}
          </Grid>
        )}
      </Box>

      {/* Add Payment Method Dialog */}
      <Dialog 
        open={addMethodDialog} 
        onClose={() => setAddMethodDialog(false)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>
          <Box display="flex" alignItems="center" gap={2}>
            <AddIcon />
            Add New Payment Method
          </Box>
        </DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 2 }}>
            <FormControl fullWidth sx={{ mb: 3 }}>
              <InputLabel>Payment Method Type</InputLabel>
              <Select
                value={formData.type}
                label="Payment Method Type"
                onChange={(e) => setFormData({ ...formData, type: e.target.value as any })}
              >
                <MenuItem value="credit_card">Credit Card</MenuItem>
                <MenuItem value="debit_card">Debit Card</MenuItem>
                <MenuItem value="bank_account">Bank Account</MenuItem>
                <MenuItem value="digital_wallet">Digital Wallet</MenuItem>
              </Select>
            </FormControl>

            {(formData.type === 'credit_card' || formData.type === 'debit_card') && (
              <>
                <TextField
                  fullWidth
                  label="Card Number"
                  value={formData.cardNumber}
                  onChange={handleCardNumberChange}
                  placeholder="1234 5678 9012 3456"
                  inputProps={{ maxLength: 19 }}
                  sx={{ mb: 2 }}
                />
                
                <TextField
                  fullWidth
                  label="Cardholder Name"
                  value={formData.cardholderName}
                  onChange={(e) => setFormData({ ...formData, cardholderName: e.target.value })}
                  sx={{ mb: 2 }}
                />

                <Box display="flex" gap={2} sx={{ mb: 2 }}>
                  <FormControl sx={{ minWidth: 120 }}>
                    <InputLabel>Month</InputLabel>
                    <Select
                      value={formData.expiryMonth}
                      label="Month"
                      onChange={(e) => setFormData({ ...formData, expiryMonth: e.target.value })}
                    >
                      {Array.from({ length: 12 }, (_, i) => (
                        <MenuItem key={i + 1} value={String(i + 1).padStart(2, '0')}>
                          {String(i + 1).padStart(2, '0')}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>

                  <FormControl sx={{ minWidth: 120 }}>
                    <InputLabel>Year</InputLabel>
                    <Select
                      value={formData.expiryYear}
                      label="Year"
                      onChange={(e) => setFormData({ ...formData, expiryYear: e.target.value })}
                    >
                      {Array.from({ length: 10 }, (_, i) => {
                        const year = new Date().getFullYear() + i;
                        return (
                          <MenuItem key={year} value={String(year)}>
                            {year}
                          </MenuItem>
                        );
                      })}
                    </Select>
                  </FormControl>

                  <TextField
                    label="CVV"
                    value={formData.cvv}
                    onChange={(e) => setFormData({ ...formData, cvv: e.target.value })}
                    inputProps={{ maxLength: 4 }}
                    sx={{ width: 100 }}
                  />
                </Box>
              </>
            )}

            <Typography variant="h6" sx={{ mt: 3, mb: 2 }}>
              Billing Address
            </Typography>

            <TextField
              fullWidth
              label="Street Address"
              value={formData.billingAddress.street}
              onChange={(e) => setFormData({ 
                ...formData, 
                billingAddress: { ...formData.billingAddress, street: e.target.value }
              })}
              sx={{ mb: 2 }}
            />

            <Box display="flex" gap={2} sx={{ mb: 2 }}>
              <TextField
                label="City"
                value={formData.billingAddress.city}
                onChange={(e) => setFormData({ 
                  ...formData, 
                  billingAddress: { ...formData.billingAddress, city: e.target.value }
                })}
                sx={{ flex: 1 }}
              />
              
              <TextField
                label="State/Province"
                value={formData.billingAddress.state}
                onChange={(e) => setFormData({ 
                  ...formData, 
                  billingAddress: { ...formData.billingAddress, state: e.target.value }
                })}
                sx={{ flex: 1 }}
              />
            </Box>

            <Box display="flex" gap={2}>
              <TextField
                label="ZIP/Postal Code"
                value={formData.billingAddress.zipCode}
                onChange={(e) => setFormData({ 
                  ...formData, 
                  billingAddress: { ...formData.billingAddress, zipCode: e.target.value }
                })}
                sx={{ flex: 1 }}
              />
              
              <FormControl sx={{ flex: 1 }}>
                <InputLabel>Country</InputLabel>
                <Select
                  value={formData.billingAddress.country}
                  label="Country"
                  onChange={(e) => setFormData({ 
                    ...formData, 
                    billingAddress: { ...formData.billingAddress, country: e.target.value }
                  })}
                >
                  <MenuItem value="SA">Saudi Arabia</MenuItem>
                  <MenuItem value="AE">United Arab Emirates</MenuItem>
                  <MenuItem value="KW">Kuwait</MenuItem>
                  <MenuItem value="BH">Bahrain</MenuItem>
                  <MenuItem value="QA">Qatar</MenuItem>
                </Select>
              </FormControl>
            </Box>

            <Alert severity="info" sx={{ mt: 3 }}>
              <Box display="flex" alignItems="center" gap={1}>
                <LockIcon fontSize="small" />
                Your payment information is encrypted and stored securely. We never store your full card number or CVV.
              </Box>
            </Alert>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAddMethodDialog(false)}>
            Cancel
          </Button>
          <Button 
            variant="contained" 
            onClick={handleAddPaymentMethod}
            disabled={loading}
          >
            {loading ? <CircularProgress size={20} /> : 'Add Payment Method'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Verification Dialog */}
      <Dialog 
        open={verificationDialog} 
        onClose={() => setVerificationDialog(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>
          <Box display="flex" alignItems="center" gap={2}>
            <SecurityIcon />
            Verify Payment Method
          </Box>
        </DialogTitle>
        <DialogContent>
          {verificationStatus && (
            <Box sx={{ pt: 2 }}>
              {verificationStatus.isVerifying ? (
                <Box textAlign="center" py={3}>
                  <CircularProgress size={60} sx={{ mb: 2 }} />
                  <Typography variant="h6" gutterBottom>
                    Verifying Payment Method
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Estimated time: {verificationStatus.estimatedTime}
                  </Typography>
                </Box>
              ) : (
                <Box textAlign="center" py={3}>
                  <CheckCircleIcon color="success" sx={{ fontSize: 60, mb: 2 }} />
                  <Typography variant="h6" gutterBottom>
                    Verification Complete
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Your payment method has been successfully verified.
                  </Typography>
                </Box>
              )}
              
              <Alert severity="info" sx={{ mt: 2 }}>
                {verificationStatus.instructions}
              </Alert>
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setVerificationDialog(false)}>
            Close
          </Button>
        </DialogActions>
      </Dialog>

      {/* Snackbar */}
      <Snackbar
        open={snackbar.open}
        autoHideDuration={6000}
        onClose={() => setSnackbar({ ...snackbar, open: false })}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
      >
        <Alert onClose={() => setSnackbar({ ...snackbar, open: false })} severity={snackbar.severity} sx={{ width: '100%' }}>
          {snackbar.message}
        </Alert>
      </Snackbar>
    </Container>
  );
};

export default PaymentMethods;
