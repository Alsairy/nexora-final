import {
  AccountBalance,
  Add,
  TrendingUp,
  CheckCircle,
  Error,
  Pending,
  Person,
  AttachMoney,
} from '@mui/icons-material';
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
  LinearProgress,
  Rating,
} from '@mui/material';
import React, { useState } from 'react';

interface LoanApplication {
  id: string;
  applicantName: string;
  loanAmount: number;
  purpose: string;
  status: 'pending' | 'approved' | 'rejected' | 'funded';
  creditScore: number;
  interestRate: number;
  term: number;
  applicationDate: string;
  riskLevel: 'low' | 'medium' | 'high';
}

interface LoanOffer {
  id: string;
  lenderName: string;
  amount: number;
  interestRate: number;
  term: number;
  rating: number;
  requirements: string[];
}

const LoanMarketplace: React.FC = () => {
  const [openDialog, setOpenDialog] = useState(false);
  const [dialogType, setDialogType] = useState<'application' | 'offer'>('application');
  const [newApplication, setNewApplication] = useState({
    amount: '',
    purpose: '',
    term: '12',
    income: '',
  });

  const applications: LoanApplication[] = [
    {
      id: 'LOAN-001',
      applicantName: 'John Doe',
      loanAmount: 50000,
      purpose: 'Business Expansion',
      status: 'approved',
      creditScore: 750,
      interestRate: 8.5,
      term: 24,
      applicationDate: '2024-06-15',
      riskLevel: 'low',
    },
    {
      id: 'LOAN-002',
      applicantName: 'Jane Smith',
      loanAmount: 25000,
      purpose: 'Equipment Purchase',
      status: 'pending',
      creditScore: 680,
      interestRate: 12.0,
      term: 18,
      applicationDate: '2024-06-16',
      riskLevel: 'medium',
    },
    {
      id: 'LOAN-003',
      applicantName: 'Mike Johnson',
      loanAmount: 75000,
      purpose: 'Real Estate',
      status: 'funded',
      creditScore: 800,
      interestRate: 6.5,
      term: 36,
      applicationDate: '2024-06-14',
      riskLevel: 'low',
    },
  ];

  const offers: LoanOffer[] = [
    {
      id: 'OFFER-001',
      lenderName: 'Capital Bank',
      amount: 100000,
      interestRate: 7.5,
      term: 24,
      rating: 4.5,
      requirements: ['Credit Score 700+', 'Annual Revenue $500K+', 'Business Age 2+ years'],
    },
    {
      id: 'OFFER-002',
      lenderName: 'Growth Finance',
      amount: 50000,
      interestRate: 9.2,
      term: 18,
      rating: 4.2,
      requirements: ['Credit Score 650+', 'Annual Revenue $250K+', 'Collateral Required'],
    },
    {
      id: 'OFFER-003',
      lenderName: 'Quick Loans Inc',
      amount: 25000,
      interestRate: 15.0,
      term: 12,
      rating: 3.8,
      requirements: ['Credit Score 600+', 'Proof of Income', 'Fast Approval'],
    },
  ];

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'approved':
        return <CheckCircle color="success" />;
      case 'funded':
        return <AttachMoney color="info" />;
      case 'pending':
        return <Pending color="warning" />;
      case 'rejected':
        return <Error color="error" />;
      default:
        return <Pending />;
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'approved':
        return 'success';
      case 'funded':
        return 'info';
      case 'pending':
        return 'warning';
      case 'rejected':
        return 'error';
      default:
        return 'default';
    }
  };

  const getRiskColor = (risk: string) => {
    switch (risk) {
      case 'low':
        return 'success';
      case 'medium':
        return 'warning';
      case 'high':
        return 'error';
      default:
        return 'default';
    }
  };

  const handleSubmitApplication = () => {
    console.log('Submitting application:', newApplication);
    setOpenDialog(false);
    setNewApplication({ amount: '', purpose: '', term: '12', income: '' });
  };

  const totalApplications = applications.length;
  const approvedApplications = applications.filter(app => app.status === 'approved').length;
  const totalLoanValue = applications.reduce((sum, app) => sum + app.loanAmount, 0);
  const avgInterestRate = (
    applications.reduce((sum, app) => sum + app.interestRate, 0) / applications.length
  ).toFixed(1);

  return (
    <Container maxWidth="xl">
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" component="h1" gutterBottom>
          Loan Marketplace
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Connect borrowers with lenders and manage loan applications.
        </Typography>
      </Box>

      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography color="textSecondary" gutterBottom variant="h6">
                    Total Applications
                  </Typography>
                  <Typography variant="h4">{totalApplications}</Typography>
                </Box>
                <AccountBalance color="primary" sx={{ fontSize: 40 }} />
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
                    Approved
                  </Typography>
                  <Typography variant="h4">{approvedApplications}</Typography>
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
                    Total Value
                  </Typography>
                  <Typography variant="h4">${totalLoanValue.toLocaleString()}</Typography>
                </Box>
                <AttachMoney color="info" sx={{ fontSize: 40 }} />
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
                    Avg Interest Rate
                  </Typography>
                  <Typography variant="h4">{avgInterestRate}%</Typography>
                </Box>
                <TrendingUp color="warning" sx={{ fontSize: 40 }} />
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Grid container spacing={3}>
        <Grid item xs={12} md={8}>
          <Paper sx={{ p: 3, mb: 3 }}>
            <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 3 }}>
              <Typography variant="h6">Loan Applications</Typography>
              <Button
                variant="contained"
                startIcon={<Add />}
                onClick={() => {
                  setDialogType('application');
                  setOpenDialog(true);
                }}
              >
                New Application
              </Button>
            </Box>

            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Applicant</TableCell>
                    <TableCell>Amount</TableCell>
                    <TableCell>Purpose</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Credit Score</TableCell>
                    <TableCell>Risk Level</TableCell>
                    <TableCell>Interest Rate</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {applications.map(application => (
                    <TableRow key={application.id}>
                      <TableCell>{application.applicantName}</TableCell>
                      <TableCell>${application.loanAmount.toLocaleString()}</TableCell>
                      <TableCell>{application.purpose}</TableCell>
                      <TableCell>
                        <Chip
                          icon={getStatusIcon(application.status)}
                          label={application.status.toUpperCase()}
                          color={getStatusColor(application.status) as any}
                          size="small"
                        />
                      </TableCell>
                      <TableCell>
                        <Box display="flex" alignItems="center" gap={1}>
                          <Typography variant="body2">{application.creditScore}</Typography>
                          <LinearProgress
                            variant="determinate"
                            value={(application.creditScore / 850) * 100}
                            sx={{ width: 50, height: 4 }}
                            color={
                              application.creditScore > 700
                                ? 'success'
                                : application.creditScore > 600
                                  ? 'warning'
                                  : 'error'
                            }
                          />
                        </Box>
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={application.riskLevel.toUpperCase()}
                          color={getRiskColor(application.riskLevel) as any}
                          size="small"
                          variant="outlined"
                        />
                      </TableCell>
                      <TableCell>{application.interestRate}%</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </Paper>

          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              Available Loan Offers
            </Typography>
            <Grid container spacing={2}>
              {offers.map(offer => (
                <Grid item xs={12} md={6} key={offer.id}>
                  <Card variant="outlined">
                    <CardContent>
                      <Box
                        display="flex"
                        justifyContent="space-between"
                        alignItems="start"
                        sx={{ mb: 2 }}
                      >
                        <Typography variant="h6">{offer.lenderName}</Typography>
                        <Rating value={offer.rating} precision={0.1} size="small" readOnly />
                      </Box>

                      <Typography variant="h4" color="primary" gutterBottom>
                        ${offer.amount.toLocaleString()}
                      </Typography>

                      <Box display="flex" justifyContent="space-between" sx={{ mb: 2 }}>
                        <Typography variant="body2" color="text.secondary">
                          Interest Rate: {offer.interestRate}%
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          Term: {offer.term} months
                        </Typography>
                      </Box>

                      <Typography variant="subtitle2" gutterBottom>
                        Requirements:
                      </Typography>
                      <Box sx={{ mb: 2 }}>
                        {offer.requirements.map((req, index) => (
                          <Chip
                            key={index}
                            label={req}
                            size="small"
                            variant="outlined"
                            sx={{ mr: 1, mb: 1 }}
                          />
                        ))}
                      </Box>

                      <Button variant="outlined" fullWidth>
                        Apply Now
                      </Button>
                    </CardContent>
                  </Card>
                </Grid>
              ))}
            </Grid>
          </Paper>
        </Grid>

        <Grid item xs={12} md={4}>
          <Paper sx={{ p: 3, mb: 3 }}>
            <Typography variant="h6" gutterBottom>
              Loan Calculator
            </Typography>
            <Box display="flex" flexDirection="column" gap={2}>
              <TextField
                label="Loan Amount"
                type="number"
                fullWidth
                InputProps={{
                  startAdornment: <Typography sx={{ mr: 1 }}>$</Typography>,
                }}
              />
              <TextField
                label="Interest Rate"
                type="number"
                fullWidth
                InputProps={{
                  endAdornment: <Typography sx={{ ml: 1 }}>%</Typography>,
                }}
              />
              <FormControl fullWidth>
                <InputLabel>Loan Term</InputLabel>
                <Select defaultValue="12">
                  <MenuItem value="6">6 months</MenuItem>
                  <MenuItem value="12">12 months</MenuItem>
                  <MenuItem value="18">18 months</MenuItem>
                  <MenuItem value="24">24 months</MenuItem>
                  <MenuItem value="36">36 months</MenuItem>
                </Select>
              </FormControl>
              <Button variant="contained" fullWidth>
                Calculate Payment
              </Button>
            </Box>
          </Paper>

          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              Market Insights
            </Typography>
            <Box display="flex" flexDirection="column" gap={2}>
              <Box>
                <Typography variant="body2" color="text.secondary">
                  Average Interest Rate
                </Typography>
                <Typography variant="h5" color="primary">
                  {avgInterestRate}%
                </Typography>
              </Box>
              <Box>
                <Typography variant="body2" color="text.secondary">
                  Approval Rate
                </Typography>
                <Typography variant="h5" color="success.main">
                  78%
                </Typography>
              </Box>
              <Box>
                <Typography variant="body2" color="text.secondary">
                  Average Loan Amount
                </Typography>
                <Typography variant="h5" color="info.main">
                  ${Math.round(totalLoanValue / totalApplications).toLocaleString()}
                </Typography>
              </Box>
            </Box>
          </Paper>
        </Grid>
      </Grid>

      <Dialog open={openDialog} onClose={() => setOpenDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>
          {dialogType === 'application' ? 'New Loan Application' : 'Create Loan Offer'}
        </DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 2 }}>
            <Grid container spacing={2}>
              <Grid item xs={12}>
                <TextField
                  label="Loan Amount"
                  value={newApplication.amount}
                  onChange={e => setNewApplication({ ...newApplication, amount: e.target.value })}
                  fullWidth
                  type="number"
                  InputProps={{
                    startAdornment: <Typography sx={{ mr: 1 }}>$</Typography>,
                  }}
                />
              </Grid>
              <Grid item xs={12}>
                <FormControl fullWidth>
                  <InputLabel>Loan Purpose</InputLabel>
                  <Select
                    value={newApplication.purpose}
                    label="Loan Purpose"
                    onChange={e =>
                      setNewApplication({ ...newApplication, purpose: e.target.value })
                    }
                  >
                    <MenuItem value="Business Expansion">Business Expansion</MenuItem>
                    <MenuItem value="Equipment Purchase">Equipment Purchase</MenuItem>
                    <MenuItem value="Working Capital">Working Capital</MenuItem>
                    <MenuItem value="Real Estate">Real Estate</MenuItem>
                    <MenuItem value="Debt Consolidation">Debt Consolidation</MenuItem>
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={6}>
                <FormControl fullWidth>
                  <InputLabel>Loan Term</InputLabel>
                  <Select
                    value={newApplication.term}
                    label="Loan Term"
                    onChange={e => setNewApplication({ ...newApplication, term: e.target.value })}
                  >
                    <MenuItem value="6">6 months</MenuItem>
                    <MenuItem value="12">12 months</MenuItem>
                    <MenuItem value="18">18 months</MenuItem>
                    <MenuItem value="24">24 months</MenuItem>
                    <MenuItem value="36">36 months</MenuItem>
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={6}>
                <TextField
                  label="Annual Income"
                  value={newApplication.income}
                  onChange={e => setNewApplication({ ...newApplication, income: e.target.value })}
                  fullWidth
                  type="number"
                  InputProps={{
                    startAdornment: <Typography sx={{ mr: 1 }}>$</Typography>,
                  }}
                />
              </Grid>
            </Grid>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenDialog(false)}>Cancel</Button>
          <Button onClick={handleSubmitApplication} variant="contained">
            Submit Application
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
};

export default LoanMarketplace;
