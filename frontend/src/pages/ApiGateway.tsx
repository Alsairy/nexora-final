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
  Tabs,
  Tab,
  IconButton,
} from '@mui/material';
import {
  Api,
  Add,
  Code,
  Security,
  Speed,
  CheckCircle,
  Error,
  Warning,
  ContentCopy,
} from '@mui/icons-material';

interface ApiEndpoint {
  id: string;
  name: string;
  method: 'GET' | 'POST' | 'PUT' | 'DELETE';
  path: string;
  status: 'active' | 'inactive' | 'deprecated';
  requests: number;
  latency: number;
  errorRate: number;
}

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;

  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`api-tabpanel-${index}`}
      aria-labelledby={`api-tab-${index}`}
      {...other}
    >
      {value === index && (
        <Box sx={{ p: 3 }}>
          {children}
        </Box>
      )}
    </div>
  );
}

const ApiGateway: React.FC = () => {
  const [tabValue, setTabValue] = useState(0);
  const [openDialog, setOpenDialog] = useState(false);
  const [newEndpoint, setNewEndpoint] = useState({
    name: '',
    method: 'GET' as const,
    path: '',
    description: '',
  });

  const endpoints: ApiEndpoint[] = [
    {
      id: 'API-001',
      name: 'User Authentication',
      method: 'POST',
      path: '/api/auth/login',
      status: 'active',
      requests: 15420,
      latency: 120,
      errorRate: 0.5,
    },
    {
      id: 'API-002',
      name: 'Payment Processing',
      method: 'POST',
      path: '/api/payments/process',
      status: 'active',
      requests: 8750,
      latency: 250,
      errorRate: 1.2,
    },
    {
      id: 'API-003',
      name: 'User Profile',
      method: 'GET',
      path: '/api/users/profile',
      status: 'active',
      requests: 12300,
      latency: 80,
      errorRate: 0.3,
    },
    {
      id: 'API-004',
      name: 'Legacy Endpoint',
      method: 'GET',
      path: '/api/v1/legacy',
      status: 'deprecated',
      requests: 150,
      latency: 500,
      errorRate: 5.0,
    },
  ];

  const getMethodColor = (method: string) => {
    switch (method) {
      case 'GET':
        return 'success';
      case 'POST':
        return 'primary';
      case 'PUT':
        return 'warning';
      case 'DELETE':
        return 'error';
      default:
        return 'default';
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'active':
        return 'success';
      case 'inactive':
        return 'default';
      case 'deprecated':
        return 'warning';
      default:
        return 'default';
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'active':
        return <CheckCircle color="success" />;
      case 'inactive':
        return <Error color="disabled" />;
      case 'deprecated':
        return <Warning color="warning" />;
      default:
        return <CheckCircle />;
    }
  };

  const handleCreateEndpoint = () => {
    console.log('Creating endpoint:', newEndpoint);
    setOpenDialog(false);
    setNewEndpoint({ name: '', method: 'GET', path: '', description: '' });
  };

  const totalRequests = endpoints.reduce((sum, ep) => sum + ep.requests, 0);
  const avgLatency = Math.round(endpoints.reduce((sum, ep) => sum + ep.latency, 0) / endpoints.length);
  const activeEndpoints = endpoints.filter(ep => ep.status === 'active').length;
  const avgErrorRate = (endpoints.reduce((sum, ep) => sum + ep.errorRate, 0) / endpoints.length).toFixed(1);

  return (
    <Container maxWidth="xl">
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" component="h1" gutterBottom>
          API Gateway
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Manage and monitor your API endpoints and integrations.
        </Typography>
      </Box>

      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography color="textSecondary" gutterBottom variant="h6">
                    Total Requests
                  </Typography>
                  <Typography variant="h4">
                    {totalRequests.toLocaleString()}
                  </Typography>
                </Box>
                <Api color="primary" sx={{ fontSize: 40 }} />
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
                    Active Endpoints
                  </Typography>
                  <Typography variant="h4">
                    {activeEndpoints}
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
                    Avg Latency
                  </Typography>
                  <Typography variant="h4">
                    {avgLatency}ms
                  </Typography>
                </Box>
                <Speed color="info" sx={{ fontSize: 40 }} />
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
                    Error Rate
                  </Typography>
                  <Typography variant="h4">
                    {avgErrorRate}%
                  </Typography>
                </Box>
                <Error color="error" sx={{ fontSize: 40 }} />
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Paper sx={{ width: '100%' }}>
        <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
          <Tabs value={tabValue} onChange={(e, newValue) => setTabValue(newValue)}>
            <Tab label="Endpoints" />
            <Tab label="Documentation" />
            <Tab label="Security" />
            <Tab label="Analytics" />
          </Tabs>
        </Box>
        
        <TabPanel value={tabValue} index={0}>
          <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 3 }}>
            <Typography variant="h6">
              API Endpoints
            </Typography>
            <Button
              variant="contained"
              startIcon={<Add />}
              onClick={() => setOpenDialog(true)}
            >
              Add Endpoint
            </Button>
          </Box>
          
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Name</TableCell>
                  <TableCell>Method</TableCell>
                  <TableCell>Path</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Requests</TableCell>
                  <TableCell>Latency</TableCell>
                  <TableCell>Error Rate</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {endpoints.map((endpoint) => (
                  <TableRow key={endpoint.id}>
                    <TableCell>{endpoint.name}</TableCell>
                    <TableCell>
                      <Chip
                        label={endpoint.method}
                        color={getMethodColor(endpoint.method) as any}
                        size="small"
                      />
                    </TableCell>
                    <TableCell>
                      <Box display="flex" alignItems="center" gap={1}>
                        <Typography variant="body2" fontFamily="monospace">
                          {endpoint.path}
                        </Typography>
                        <IconButton size="small">
                          <ContentCopy fontSize="small" />
                        </IconButton>
                      </Box>
                    </TableCell>
                    <TableCell>
                      <Chip
                        icon={getStatusIcon(endpoint.status)}
                        label={endpoint.status.toUpperCase()}
                        color={getStatusColor(endpoint.status) as any}
                        size="small"
                      />
                    </TableCell>
                    <TableCell>{endpoint.requests.toLocaleString()}</TableCell>
                    <TableCell>{endpoint.latency}ms</TableCell>
                    <TableCell>
                      <Typography
                        color={endpoint.errorRate > 2 ? 'error.main' : 'text.primary'}
                      >
                        {endpoint.errorRate}%
                      </Typography>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </TabPanel>
        
        <TabPanel value={tabValue} index={1}>
          <Typography variant="h6" gutterBottom>
            API Documentation
          </Typography>
          <Paper sx={{ p: 3, mb: 3 }}>
            <Typography variant="h6" gutterBottom>
              Authentication
            </Typography>
            <Typography variant="body2" paragraph>
              All API requests require authentication using Bearer tokens. Include the token in the Authorization header:
            </Typography>
            <Paper sx={{ p: 2, bgcolor: 'grey.100', fontFamily: 'monospace' }}>
              Authorization: Bearer YOUR_API_TOKEN
            </Paper>
          </Paper>
          
          <Paper sx={{ p: 3, mb: 3 }}>
            <Typography variant="h6" gutterBottom>
              Rate Limiting
            </Typography>
            <Typography variant="body2" paragraph>
              API requests are limited to 1000 requests per hour per API key. Rate limit headers are included in responses:
            </Typography>
            <Paper sx={{ p: 2, bgcolor: 'grey.100', fontFamily: 'monospace' }}>
              X-RateLimit-Limit: 1000<br />
              X-RateLimit-Remaining: 999<br />
              X-RateLimit-Reset: 1640995200
            </Paper>
          </Paper>
        </TabPanel>
        
        <TabPanel value={tabValue} index={2}>
          <Typography variant="h6" gutterBottom>
            Security Settings
          </Typography>
          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              <Paper sx={{ p: 3 }}>
                <Typography variant="h6" gutterBottom>
                  API Keys
                </Typography>
                <Box display="flex" flexDirection="column" gap={2}>
                  <Button variant="outlined" startIcon={<Add />}>
                    Generate New API Key
                  </Button>
                  <Button variant="outlined" startIcon={<Security />}>
                    Rotate Keys
                  </Button>
                  <Button variant="outlined" color="error">
                    Revoke All Keys
                  </Button>
                </Box>
              </Paper>
            </Grid>
            <Grid item xs={12} md={6}>
              <Paper sx={{ p: 3 }}>
                <Typography variant="h6" gutterBottom>
                  Access Control
                </Typography>
                <Box display="flex" flexDirection="column" gap={2}>
                  <Button variant="outlined">
                    IP Whitelist
                  </Button>
                  <Button variant="outlined">
                    CORS Settings
                  </Button>
                  <Button variant="outlined">
                    Webhook Security
                  </Button>
                </Box>
              </Paper>
            </Grid>
          </Grid>
        </TabPanel>
        
        <TabPanel value={tabValue} index={3}>
          <Typography variant="h6" gutterBottom>
            API Analytics
          </Typography>
          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              <Paper sx={{ p: 3 }}>
                <Typography variant="h6" gutterBottom>
                  Request Volume
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  API request trends over time
                </Typography>
              </Paper>
            </Grid>
            <Grid item xs={12} md={6}>
              <Paper sx={{ p: 3 }}>
                <Typography variant="h6" gutterBottom>
                  Response Times
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Average response time metrics
                </Typography>
              </Paper>
            </Grid>
          </Grid>
        </TabPanel>
      </Paper>

      <Dialog open={openDialog} onClose={() => setOpenDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Create New API Endpoint</DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 2 }}>
            <Grid container spacing={2}>
              <Grid item xs={12}>
                <TextField
                  label="Endpoint Name"
                  value={newEndpoint.name}
                  onChange={(e) => setNewEndpoint({ ...newEndpoint, name: e.target.value })}
                  fullWidth
                  placeholder="User Authentication"
                />
              </Grid>
              <Grid item xs={6}>
                <FormControl fullWidth>
                  <InputLabel>HTTP Method</InputLabel>
                  <Select
                    value={newEndpoint.method}
                    label="HTTP Method"
                    onChange={(e) => setNewEndpoint({ ...newEndpoint, method: e.target.value as any })}
                  >
                    <MenuItem value="GET">GET</MenuItem>
                    <MenuItem value="POST">POST</MenuItem>
                    <MenuItem value="PUT">PUT</MenuItem>
                    <MenuItem value="DELETE">DELETE</MenuItem>
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={6}>
                <TextField
                  label="API Path"
                  value={newEndpoint.path}
                  onChange={(e) => setNewEndpoint({ ...newEndpoint, path: e.target.value })}
                  fullWidth
                  placeholder="/api/users"
                />
              </Grid>
              <Grid item xs={12}>
                <TextField
                  label="Description"
                  value={newEndpoint.description}
                  onChange={(e) => setNewEndpoint({ ...newEndpoint, description: e.target.value })}
                  fullWidth
                  multiline
                  rows={3}
                  placeholder="Describe what this endpoint does..."
                />
              </Grid>
            </Grid>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenDialog(false)}>Cancel</Button>
          <Button onClick={handleCreateEndpoint} variant="contained">
            Create Endpoint
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
};

export default ApiGateway;
