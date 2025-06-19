import {
  AdminPanelSettings,
  Security,
  Settings,
  Storage,
  CloudSync,
  Warning,
  CheckCircle,
  Error,
  Info,
  Refresh,
  Download,
  Upload,
  Delete,
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
  Switch,
  FormControlLabel,
  Tabs,
  Tab,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Alert,
} from '@mui/material';
import React, { useState } from 'react';

interface SystemHealth {
  service: string;
  status: 'healthy' | 'warning' | 'error';
  uptime: string;
  lastCheck: string;
  details: string;
}

interface AuditLog {
  id: string;
  timestamp: string;
  user: string;
  action: string;
  resource: string;
  status: 'success' | 'failure';
  ipAddress: string;
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
      id={`admin-tabpanel-${index}`}
      aria-labelledby={`admin-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

const AdminConsole: React.FC = () => {
  const [tabValue, setTabValue] = useState(0);
  const [openDialog, setOpenDialog] = useState(false);
  const [dialogType, setDialogType] = useState<'backup' | 'maintenance'>('backup');

  const systemHealth: SystemHealth[] = [
    {
      service: 'API Gateway',
      status: 'healthy',
      uptime: '99.9%',
      lastCheck: '2024-06-18 10:30:00',
      details: 'All endpoints responding normally',
    },
    {
      service: 'Database',
      status: 'healthy',
      uptime: '99.8%',
      lastCheck: '2024-06-18 10:29:00',
      details: 'Connection pool: 85% utilized',
    },
    {
      service: 'Redis Cache',
      status: 'warning',
      uptime: '98.5%',
      lastCheck: '2024-06-18 10:28:00',
      details: 'Memory usage: 92% - consider scaling',
    },
    {
      service: 'Payment Gateway',
      status: 'healthy',
      uptime: '99.7%',
      lastCheck: '2024-06-18 10:30:00',
      details: 'Processing transactions normally',
    },
    {
      service: 'SMS Gateway',
      status: 'error',
      uptime: '95.2%',
      lastCheck: '2024-06-18 10:25:00',
      details: 'Rate limit exceeded - service degraded',
    },
  ];

  const auditLogs: AuditLog[] = [
    {
      id: 'AUDIT-001',
      timestamp: '2024-06-18 10:30:00',
      user: 'admin@nexora.com',
      action: 'USER_CREATED',
      resource: 'users/john.doe',
      status: 'success',
      ipAddress: '192.168.1.100',
    },
    {
      id: 'AUDIT-002',
      timestamp: '2024-06-18 10:25:00',
      user: 'manager@nexora.com',
      action: 'PAYMENT_PROCESSED',
      resource: 'payments/TXN-12345',
      status: 'success',
      ipAddress: '192.168.1.101',
    },
    {
      id: 'AUDIT-003',
      timestamp: '2024-06-18 10:20:00',
      user: 'user@nexora.com',
      action: 'LOGIN_FAILED',
      resource: 'auth/login',
      status: 'failure',
      ipAddress: '192.168.1.102',
    },
  ];

  const getHealthIcon = (status: string) => {
    switch (status) {
      case 'healthy':
        return <CheckCircle color="success" />;
      case 'warning':
        return <Warning color="warning" />;
      case 'error':
        return <Error color="error" />;
      default:
        return <Info />;
    }
  };

  const getHealthColor = (status: string) => {
    switch (status) {
      case 'healthy':
        return 'success';
      case 'warning':
        return 'warning';
      case 'error':
        return 'error';
      default:
        return 'default';
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'success':
        return <CheckCircle color="success" />;
      case 'failure':
        return <Error color="error" />;
      default:
        return <Info />;
    }
  };

  const handleMaintenanceAction = () => {
    console.log('Performing maintenance action:', dialogType);
    setOpenDialog(false);
  };

  const healthyServices = systemHealth.filter(s => s.status === 'healthy').length;
  const warningServices = systemHealth.filter(s => s.status === 'warning').length;
  const errorServices = systemHealth.filter(s => s.status === 'error').length;
  const totalServices = systemHealth.length;

  return (
    <Container maxWidth="xl">
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" component="h1" gutterBottom>
          Admin Console
        </Typography>
        <Typography variant="body1" color="text.secondary">
          System administration and monitoring dashboard.
        </Typography>
      </Box>

      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography color="textSecondary" gutterBottom variant="h6">
                    Total Services
                  </Typography>
                  <Typography variant="h4">{totalServices}</Typography>
                </Box>
                <AdminPanelSettings color="primary" sx={{ fontSize: 40 }} />
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
                    Healthy
                  </Typography>
                  <Typography variant="h4">{healthyServices}</Typography>
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
                    Warnings
                  </Typography>
                  <Typography variant="h4">{warningServices}</Typography>
                </Box>
                <Warning color="warning" sx={{ fontSize: 40 }} />
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
                    Errors
                  </Typography>
                  <Typography variant="h4">{errorServices}</Typography>
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
            <Tab label="System Health" />
            <Tab label="Audit Logs" />
            <Tab label="Configuration" />
            <Tab label="Maintenance" />
          </Tabs>
        </Box>

        <TabPanel value={tabValue} index={0}>
          <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 3 }}>
            <Typography variant="h6">System Health Status</Typography>
            <Button variant="outlined" startIcon={<Refresh />}>
              Refresh Status
            </Button>
          </Box>

          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Service</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Uptime</TableCell>
                  <TableCell>Last Check</TableCell>
                  <TableCell>Details</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {systemHealth.map(service => (
                  <TableRow key={service.service}>
                    <TableCell>{service.service}</TableCell>
                    <TableCell>
                      <Chip
                        icon={getHealthIcon(service.status)}
                        label={service.status.toUpperCase()}
                        color={getHealthColor(service.status) as any}
                        size="small"
                      />
                    </TableCell>
                    <TableCell>{service.uptime}</TableCell>
                    <TableCell>{new Date(service.lastCheck).toLocaleString()}</TableCell>
                    <TableCell>{service.details}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </TabPanel>

        <TabPanel value={tabValue} index={1}>
          <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 3 }}>
            <Typography variant="h6">Audit Logs</Typography>
            <Button variant="outlined" startIcon={<Download />}>
              Export Logs
            </Button>
          </Box>

          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Timestamp</TableCell>
                  <TableCell>User</TableCell>
                  <TableCell>Action</TableCell>
                  <TableCell>Resource</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>IP Address</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {auditLogs.map(log => (
                  <TableRow key={log.id}>
                    <TableCell>{new Date(log.timestamp).toLocaleString()}</TableCell>
                    <TableCell>{log.user}</TableCell>
                    <TableCell>{log.action}</TableCell>
                    <TableCell>{log.resource}</TableCell>
                    <TableCell>
                      <Chip
                        icon={getStatusIcon(log.status)}
                        label={log.status.toUpperCase()}
                        color={log.status === 'success' ? 'success' : 'error'}
                        size="small"
                      />
                    </TableCell>
                    <TableCell>{log.ipAddress}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </TabPanel>

        <TabPanel value={tabValue} index={2}>
          <Typography variant="h6" gutterBottom>
            System Configuration
          </Typography>
          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              <Paper sx={{ p: 3 }}>
                <Typography variant="h6" gutterBottom>
                  Security Settings
                </Typography>
                <List>
                  <ListItem>
                    <ListItemIcon>
                      <Security />
                    </ListItemIcon>
                    <ListItemText
                      primary="Two-Factor Authentication"
                      secondary="Enforce 2FA for all admin users"
                    />
                    <Switch defaultChecked />
                  </ListItem>
                  <ListItem>
                    <ListItemIcon>
                      <Security />
                    </ListItemIcon>
                    <ListItemText
                      primary="Session Timeout"
                      secondary="Auto-logout after 30 minutes of inactivity"
                    />
                    <Switch defaultChecked />
                  </ListItem>
                  <ListItem>
                    <ListItemIcon>
                      <Security />
                    </ListItemIcon>
                    <ListItemText
                      primary="IP Whitelist"
                      secondary="Restrict admin access to specific IPs"
                    />
                    <Switch />
                  </ListItem>
                </List>
              </Paper>
            </Grid>
            <Grid item xs={12} md={6}>
              <Paper sx={{ p: 3 }}>
                <Typography variant="h6" gutterBottom>
                  System Settings
                </Typography>
                <List>
                  <ListItem>
                    <ListItemIcon>
                      <Settings />
                    </ListItemIcon>
                    <ListItemText
                      primary="Maintenance Mode"
                      secondary="Enable maintenance mode for system updates"
                    />
                    <Switch />
                  </ListItem>
                  <ListItem>
                    <ListItemIcon>
                      <CloudSync />
                    </ListItemIcon>
                    <ListItemText
                      primary="Auto Backup"
                      secondary="Daily automated backups at 2:00 AM"
                    />
                    <Switch defaultChecked />
                  </ListItem>
                  <ListItem>
                    <ListItemIcon>
                      <Storage />
                    </ListItemIcon>
                    <ListItemText
                      primary="Data Retention"
                      secondary="Keep audit logs for 90 days"
                    />
                    <Switch defaultChecked />
                  </ListItem>
                </List>
              </Paper>
            </Grid>
          </Grid>
        </TabPanel>

        <TabPanel value={tabValue} index={3}>
          <Typography variant="h6" gutterBottom>
            System Maintenance
          </Typography>
          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              <Paper sx={{ p: 3 }}>
                <Typography variant="h6" gutterBottom>
                  Database Operations
                </Typography>
                <Box display="flex" flexDirection="column" gap={2}>
                  <Button
                    variant="outlined"
                    startIcon={<Download />}
                    onClick={() => {
                      setDialogType('backup');
                      setOpenDialog(true);
                    }}
                  >
                    Create Backup
                  </Button>
                  <Button variant="outlined" startIcon={<Upload />}>
                    Restore Backup
                  </Button>
                  <Button variant="outlined" startIcon={<Refresh />}>
                    Optimize Database
                  </Button>
                  <Button variant="outlined" color="warning" startIcon={<Delete />}>
                    Clear Cache
                  </Button>
                </Box>
              </Paper>
            </Grid>
            <Grid item xs={12} md={6}>
              <Paper sx={{ p: 3 }}>
                <Typography variant="h6" gutterBottom>
                  System Status
                </Typography>
                <Box display="flex" flexDirection="column" gap={2}>
                  <Alert severity="info">Last backup: June 17, 2024 at 2:00 AM</Alert>
                  <Alert severity="success">System health check: All services operational</Alert>
                  <Alert severity="warning">Redis cache usage: 92% - consider scaling</Alert>
                </Box>
              </Paper>
            </Grid>
          </Grid>
        </TabPanel>
      </Paper>

      <Dialog open={openDialog} onClose={() => setOpenDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>
          {dialogType === 'backup' ? 'Create System Backup' : 'System Maintenance'}
        </DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 2 }}>
            {dialogType === 'backup' ? (
              <Grid container spacing={2}>
                <Grid item xs={12}>
                  <TextField
                    label="Backup Name"
                    fullWidth
                    defaultValue={`backup-${new Date().toISOString().split('T')[0]}`}
                  />
                </Grid>
                <Grid item xs={12}>
                  <FormControl fullWidth>
                    <InputLabel>Backup Type</InputLabel>
                    <Select defaultValue="full">
                      <MenuItem value="full">Full Backup</MenuItem>
                      <MenuItem value="incremental">Incremental Backup</MenuItem>
                      <MenuItem value="differential">Differential Backup</MenuItem>
                    </Select>
                  </FormControl>
                </Grid>
                <Grid item xs={12}>
                  <FormControlLabel control={<Switch defaultChecked />} label="Include user data" />
                </Grid>
                <Grid item xs={12}>
                  <FormControlLabel
                    control={<Switch defaultChecked />}
                    label="Include system configuration"
                  />
                </Grid>
              </Grid>
            ) : (
              <Typography>Maintenance operation configuration will be displayed here.</Typography>
            )}
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenDialog(false)}>Cancel</Button>
          <Button onClick={handleMaintenanceAction} variant="contained">
            {dialogType === 'backup' ? 'Create Backup' : 'Execute'}
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
};

export default AdminConsole;
