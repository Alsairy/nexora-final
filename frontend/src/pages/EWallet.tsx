import React, { useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  Container,
  Grid,
  Typography,
  Button,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
} from '@mui/material';
import {
  AccountBalanceWallet,
  Add,
  Remove,
  SwapHoriz,
  TrendingUp,
  CheckCircle,
  Error,
  Pending,
} from '@mui/icons-material';

interface WalletTransaction {
  id: string;
  type: 'deposit' | 'withdrawal' | 'transfer';
  amount: number;
  currency: string;
  status: 'completed' | 'pending' | 'failed';
  description: string;
  timestamp: string;
  balance: number;
}

const EWallet: React.FC = () => {
  const [openDialog, setOpenDialog] = useState(false);
  const [transactionType, setTransactionType] = useState<'deposit' | 'withdrawal' | 'transfer'>('deposit');
  const [amount, setAmount] = useState('');
  const [description, setDescription] = useState('');

  const transactions: WalletTransaction[] = [
    {
      id: 'WAL-001',
      type: 'deposit',
      amount: 1000.00,
      currency: 'USD',
      status: 'completed',
      description: 'Bank transfer deposit',
      timestamp: '2024-06-18 10:30:00',
      balance: 5250.00,
    },
    {
      id: 'WAL-002',
      type: 'withdrawal',
      amount: 500.00,
      currency: 'USD',
      status: 'completed',
      description: 'ATM withdrawal',
      timestamp: '2024-06-18 09:15:00',
      balance: 4250.00,
    },
    {
      id: 'WAL-003',
      type: 'transfer',
      amount: 250.00,
      currency: 'USD',
      status: 'pending',
      description: 'Transfer to John Doe',
      timestamp: '2024-06-18 08:45:00',
      balance: 4750.00,
    },
  ];

  const getTransactionIcon = (type: string) => {
    switch (type) {
      case 'deposit':
        return <Add color="success" />;
      case 'withdrawal':
        return <Remove color="error" />;
      case 'transfer':
        return <SwapHoriz color="info" />;
      default:
        return <SwapHoriz />;
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'completed':
        return <CheckCircle color="success" />;
      case 'pending':
        return <Pending color="warning" />;
      case 'failed':
        return <Error color="error" />;
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
      default:
        return 'default';
    }
  };

  const handleTransaction = () => {
    console.log('Processing transaction:', { transactionType, amount, description });
    setOpenDialog(false);
    setAmount('');
    setDescription('');
  };

  const currentBalance = 5250.00;
  const totalDeposits = transactions.filter(t => t.type === 'deposit').reduce((sum, t) => sum + t.amount, 0);
  const totalWithdrawals = transactions.filter(t => t.type === 'withdrawal').reduce((sum, t) => sum + t.amount, 0);
  const pendingTransactions = transactions.filter(t => t.status === 'pending').length;

  return (
    <Container maxWidth="xl">
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" component="h1" gutterBottom>
          E-Wallet
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Manage your digital wallet and track transactions.
        </Typography>
      </Box>

      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12} md={6}>
          <Card sx={{ background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)', color: 'white' }}>
            <CardContent>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography variant="h6" gutterBottom>
                    Current Balance
                  </Typography>
                  <Typography variant="h3" component="h2">
                    ${currentBalance.toLocaleString()}
                  </Typography>
                  <Typography variant="body2" sx={{ opacity: 0.8, mt: 1 }}>
                    USD
                  </Typography>
                </Box>
                <AccountBalanceWallet sx={{ fontSize: 60, opacity: 0.8 }} />
              </Box>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} md={6}>
          <Grid container spacing={2}>
            <Grid item xs={6}>
              <Card>
                <CardContent>
                  <Box display="flex" alignItems="center" justifyContent="space-between">
                    <Box>
                      <Typography color="textSecondary" gutterBottom variant="body2">
                        Total Deposits
                      </Typography>
                      <Typography variant="h5">
                        ${totalDeposits.toLocaleString()}
                      </Typography>
                    </Box>
                    <Add color="success" sx={{ fontSize: 30 }} />
                  </Box>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={6}>
              <Card>
                <CardContent>
                  <Box display="flex" alignItems="center" justifyContent="space-between">
                    <Box>
                      <Typography color="textSecondary" gutterBottom variant="body2">
                        Total Withdrawals
                      </Typography>
                      <Typography variant="h5">
                        ${totalWithdrawals.toLocaleString()}
                      </Typography>
                    </Box>
                    <Remove color="error" sx={{ fontSize: 30 }} />
                  </Box>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={6}>
              <Card>
                <CardContent>
                  <Box display="flex" alignItems="center" justifyContent="space-between">
                    <Box>
                      <Typography color="textSecondary" gutterBottom variant="body2">
                        Pending
                      </Typography>
                      <Typography variant="h5">
                        {pendingTransactions}
                      </Typography>
                    </Box>
                    <Pending color="warning" sx={{ fontSize: 30 }} />
                  </Box>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={6}>
              <Card>
                <CardContent>
                  <Box display="flex" alignItems="center" justifyContent="space-between">
                    <Box>
                      <Typography color="textSecondary" gutterBottom variant="body2">
                        Growth
                      </Typography>
                      <Typography variant="h5">
                        +12%
                      </Typography>
                    </Box>
                    <TrendingUp color="info" sx={{ fontSize: 30 }} />
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        </Grid>
      </Grid>

      <Grid container spacing={3}>
        <Grid item xs={12} md={8}>
          <Paper sx={{ p: 3 }}>
            <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 3 }}>
              <Typography variant="h6">
                Transaction History
              </Typography>
              <Box display="flex" gap={1}>
                <Button
                  variant="contained"
                  startIcon={<Add />}
                  onClick={() => {
                    setTransactionType('deposit');
                    setOpenDialog(true);
                  }}
                >
                  Deposit
                </Button>
                <Button
                  variant="outlined"
                  startIcon={<Remove />}
                  onClick={() => {
                    setTransactionType('withdrawal');
                    setOpenDialog(true);
                  }}
                >
                  Withdraw
                </Button>
                <Button
                  variant="outlined"
                  startIcon={<SwapHoriz />}
                  onClick={() => {
                    setTransactionType('transfer');
                    setOpenDialog(true);
                  }}
                >
                  Transfer
                </Button>
              </Box>
            </Box>
            
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Transaction ID</TableCell>
                    <TableCell>Type</TableCell>
                    <TableCell>Amount</TableCell>
                    <TableCell>Description</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Date</TableCell>
                    <TableCell>Balance</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {transactions.map((transaction) => (
                    <TableRow key={transaction.id}>
                      <TableCell>{transaction.id}</TableCell>
                      <TableCell>
                        <Chip
                          icon={getTransactionIcon(transaction.type)}
                          label={transaction.type.toUpperCase()}
                          size="small"
                          variant="outlined"
                        />
                      </TableCell>
                      <TableCell>
                        <Typography
                          color={
                            transaction.type === 'deposit' ? 'success.main' :
                            transaction.type === 'withdrawal' ? 'error.main' : 'info.main'
                          }
                        >
                          {transaction.type === 'deposit' ? '+' : '-'}${transaction.amount.toLocaleString()}
                        </Typography>
                      </TableCell>
                      <TableCell>{transaction.description}</TableCell>
                      <TableCell>
                        <Chip
                          icon={getStatusIcon(transaction.status)}
                          label={transaction.status.toUpperCase()}
                          color={getStatusColor(transaction.status) as any}
                          size="small"
                        />
                      </TableCell>
                      <TableCell>{new Date(transaction.timestamp).toLocaleString()}</TableCell>
                      <TableCell>${transaction.balance.toLocaleString()}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </Paper>
        </Grid>
        
        <Grid item xs={12} md={4}>
          <Paper sx={{ p: 3, mb: 3 }}>
            <Typography variant="h6" gutterBottom>
              Quick Actions
            </Typography>
            <Box display="flex" flexDirection="column" gap={2}>
              <Button variant="outlined" fullWidth startIcon={<Add />}>
                Add Funds
              </Button>
              <Button variant="outlined" fullWidth startIcon={<SwapHoriz />}>
                Send Money
              </Button>
              <Button variant="outlined" fullWidth startIcon={<TrendingUp />}>
                View Analytics
              </Button>
            </Box>
          </Paper>
          
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              Security Settings
            </Typography>
            <Box display="flex" flexDirection="column" gap={2}>
              <Button variant="text" fullWidth>
                Enable 2FA
              </Button>
              <Button variant="text" fullWidth>
                Transaction Limits
              </Button>
              <Button variant="text" fullWidth>
                Freeze Account
              </Button>
            </Box>
          </Paper>
        </Grid>
      </Grid>

      <Dialog open={openDialog} onClose={() => setOpenDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>
          {transactionType === 'deposit' ? 'Deposit Funds' :
           transactionType === 'withdrawal' ? 'Withdraw Funds' : 'Transfer Funds'}
        </DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 2 }}>
            <Grid container spacing={2}>
              <Grid item xs={12}>
                <FormControl fullWidth>
                  <InputLabel>Transaction Type</InputLabel>
                  <Select
                    value={transactionType}
                    label="Transaction Type"
                    onChange={(e) => setTransactionType(e.target.value as any)}
                  >
                    <MenuItem value="deposit">Deposit</MenuItem>
                    <MenuItem value="withdrawal">Withdrawal</MenuItem>
                    <MenuItem value="transfer">Transfer</MenuItem>
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={12}>
                <TextField
                  label="Amount"
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                  fullWidth
                  type="number"
                  InputProps={{
                    startAdornment: <Typography sx={{ mr: 1 }}>$</Typography>,
                  }}
                />
              </Grid>
              <Grid item xs={12}>
                <TextField
                  label="Description"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  fullWidth
                  multiline
                  rows={3}
                  placeholder="Enter transaction description..."
                />
              </Grid>
            </Grid>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenDialog(false)}>Cancel</Button>
          <Button onClick={handleTransaction} variant="contained">
            Process Transaction
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
};

export default EWallet;
