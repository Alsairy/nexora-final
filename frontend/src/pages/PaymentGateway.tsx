import React, { useState } from 'react';
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
} from '@mui/material';
import {
  Payment,
  Add,
  Visibility,
  GetApp,
  TrendingUp,
  CheckCircle,
  Error,
  Pending,
} from '@mui/icons-material';

interface Transaction {
  id: string;
  amount: number;
  currency: string;
  status: 'completed' | 'pending' | 'failed';
  merchant: string;
  date: string;
  reference: string;
}

const PaymentGateway: React.FC = () => {
  const [openDialog, setOpenDialog] = useState(false);
  const [selectedTransaction, setSelectedTransaction] = useState<Transaction | null>(null);

  const transactions: Transaction[] = [
    {
      id: 'TXN-001',
      amount: 1250.00,
      currency: 'USD',
      status: 'completed',
      merchant: 'E-Commerce Store',
      date: '2024-06-18 10:30:00',
      reference: 'REF-2024-001',
    },
    {
      id: 'TXN-002',
      amount: 750.50,
      currency: 'USD',
      status: 'pending',
      merchant: 'Online Services',
      date: '2024-06-18 09:15:00',
      reference: 'REF-2024-002',
    },
    {
      id: 'TXN-003',
      amount: 2100.00,
      currency: 'USD',
      status: 'failed',
      merchant: 'Digital Products',
      date: '2024-06-18 08:45:00',
      reference: 'REF-2024-003',
    },
  ];

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

  const handleViewTransaction = (transaction: Transaction) => {
    setSelectedTransaction(transaction);
    setOpenDialog(true);
  };

  const totalAmount = transactions.reduce((sum, txn) => sum + txn.amount, 0);
  const completedTransactions = transactions.filter(txn => txn.status === 'completed').length;
  const pendingTransactions = transactions.filter(txn => txn.status === 'pending').length;

  return (
    <Container maxWidth="xl">
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" component="h1" gutterBottom>
          Payment Gateway
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Manage and monitor payment transactions across your platform.
        </Typography>
      </Box>

      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography color="textSecondary" gutterBottom variant="h6">
                    Total Volume
                  </Typography>
                  <Typography variant="h4">
                    ${totalAmount.toLocaleString()}
                  </Typography>
                </Box>
                <Payment color="primary" sx={{ fontSize: 40 }} />
              </Box>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography color="textSecondary" gutterBottom variant="h6">
                    Completed
                  </Typography>
                  <Typography variant="h4">
                    {completedTransactions}
                  </Typography>
                </Box>
                <CheckCircle color="success" sx={{ fontSize: 40 }} />
              </Box>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography color="textSecondary" gutterBottom variant="h6">
                    Pending
                  </Typography>
                  <Typography variant="h4">
                    {pendingTransactions}
                  </Typography>
                </Box>
                <Pending color="warning" sx={{ fontSize: 40 }} />
              </Box>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography color="textSecondary" gutterBottom variant="h6">
                    Success Rate
                  </Typography>
                  <Typography variant="h4">
                    {Math.round((completedTransactions / transactions.length) * 100)}%
                  </Typography>
                </Box>
                <TrendingUp color="info" sx={{ fontSize: 40 }} />
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Grid container spacing={3}>
        <Grid item xs={12} md={8}>
          <Paper sx={{ p: 3 }}>
            <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 3 }}>
              <Typography variant="h6">
                Recent Transactions
              </Typography>
              <Button
                variant="contained"
                startIcon={<Add />}
                onClick={() => {}}
              >
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
                  {transactions.map((transaction) => (
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
                        <IconButton
                          size="small"
                          onClick={() => handleViewTransaction(transaction)}
                        >
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

      <Dialog open={openDialog} onClose={() => setOpenDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Transaction Details</DialogTitle>
        <DialogContent>
          {selectedTransaction && (
            <Box>
              <Grid container spacing={2}>
                <Grid item xs={6}>
                  <TextField
                    label="Transaction ID"
                    value={selectedTransaction.id}
                    fullWidth
                    disabled
                  />
                </Grid>
                <Grid item xs={6}>
                  <TextField
                    label="Reference"
                    value={selectedTransaction.reference}
                    fullWidth
                    disabled
                  />
                </Grid>
                <Grid item xs={6}>
                  <TextField
                    label="Amount"
                    value={`$${selectedTransaction.amount.toLocaleString()} ${selectedTransaction.currency}`}
                    fullWidth
                    disabled
                  />
                </Grid>
                <Grid item xs={6}>
                  <TextField
                    label="Status"
                    value={selectedTransaction.status.toUpperCase()}
                    fullWidth
                    disabled
                  />
                </Grid>
                <Grid item xs={12}>
                  <TextField
                    label="Merchant"
                    value={selectedTransaction.merchant}
                    fullWidth
                    disabled
                  />
                </Grid>
                <Grid item xs={12}>
                  <TextField
                    label="Date"
                    value={new Date(selectedTransaction.date).toLocaleString()}
                    fullWidth
                    disabled
                  />
                </Grid>
              </Grid>
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenDialog(false)}>Close</Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
};

export default PaymentGateway;
