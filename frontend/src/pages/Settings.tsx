import {
  Save as SaveIcon,
  Security as SecurityIcon,
  Notifications as NotificationsIcon,
  AccountCircle as AccountIcon,
  Business as BusinessIcon,
} from '@mui/icons-material';
import {
  Box,
  Paper,
  Typography,
  TextField,
  Button,
  Switch,
  FormControlLabel,
  Divider,
  Alert,
  Grid,
  Card,
  CardContent,
  CardActions,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Tabs,
  Tab,
} from '@mui/material';
import React, { useState } from 'react';

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
      id={`settings-tabpanel-${index}`}
      aria-labelledby={`settings-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

const Settings: React.FC = () => {
  const [activeTab, setActiveTab] = useState(0);
  const [success, setSuccess] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const [profileSettings, setProfileSettings] = useState({
    firstName: 'John',
    lastName: 'Doe',
    email: 'john.doe@example.com',
    phone: '+1234567890',
    timezone: 'UTC',
    language: 'en',
  });

  const [securitySettings, setSecuritySettings] = useState({
    twoFactorEnabled: true,
    sessionTimeout: 30,
    passwordExpiry: 90,
    loginNotifications: true,
  });

  const [notificationSettings, setNotificationSettings] = useState({
    emailNotifications: true,
    smsNotifications: false,
    pushNotifications: true,
    marketingEmails: false,
    transactionAlerts: true,
    securityAlerts: true,
  });

  const [businessSettings, setBusinessSettings] = useState({
    companyName: 'Nexora Corp',
    businessType: 'fintech',
    country: 'US',
    currency: 'USD',
    taxId: '123-45-6789',
    website: 'https://nexora.com',
  });

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  const handleSaveProfile = async () => {
    try {
      console.log('Saving profile settings:', profileSettings);
      setSuccess('Profile settings saved successfully');
      setTimeout(() => setSuccess(null), 3000);
    } catch (err) {
      setError('Failed to save profile settings');
      setTimeout(() => setError(null), 3000);
    }
  };

  const handleSaveSecurity = async () => {
    try {
      console.log('Saving security settings:', securitySettings);
      setSuccess('Security settings saved successfully');
      setTimeout(() => setSuccess(null), 3000);
    } catch (err) {
      setError('Failed to save security settings');
      setTimeout(() => setError(null), 3000);
    }
  };

  const handleSaveNotifications = async () => {
    try {
      console.log('Saving notification settings:', notificationSettings);
      setSuccess('Notification settings saved successfully');
      setTimeout(() => setSuccess(null), 3000);
    } catch (err) {
      setError('Failed to save notification settings');
      setTimeout(() => setError(null), 3000);
    }
  };

  const handleSaveBusiness = async () => {
    try {
      console.log('Saving business settings:', businessSettings);
      setSuccess('Business settings saved successfully');
      setTimeout(() => setSuccess(null), 3000);
    } catch (err) {
      setError('Failed to save business settings');
      setTimeout(() => setError(null), 3000);
    }
  };

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h4" component="h1" gutterBottom>
        Settings
      </Typography>

      {success && (
        <Alert severity="success" sx={{ mb: 2 }}>
          {success}
        </Alert>
      )}

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      <Paper sx={{ width: '100%' }}>
        <Tabs
          value={activeTab}
          onChange={handleTabChange}
          aria-label="settings tabs"
          sx={{ borderBottom: 1, borderColor: 'divider' }}
        >
          <Tab icon={<AccountIcon />} label="Profile" />
          <Tab icon={<SecurityIcon />} label="Security" />
          <Tab icon={<NotificationsIcon />} label="Notifications" />
          <Tab icon={<BusinessIcon />} label="Business" />
        </Tabs>

        <TabPanel value={activeTab} index={0}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Profile Information
              </Typography>
              <Grid container spacing={2}>
                <Grid item xs={12} sm={6}>
                  <TextField
                    fullWidth
                    label="First Name"
                    value={profileSettings.firstName}
                    onChange={e =>
                      setProfileSettings({ ...profileSettings, firstName: e.target.value })
                    }
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <TextField
                    fullWidth
                    label="Last Name"
                    value={profileSettings.lastName}
                    onChange={e =>
                      setProfileSettings({ ...profileSettings, lastName: e.target.value })
                    }
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <TextField
                    fullWidth
                    label="Email"
                    type="email"
                    value={profileSettings.email}
                    onChange={e =>
                      setProfileSettings({ ...profileSettings, email: e.target.value })
                    }
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <TextField
                    fullWidth
                    label="Phone"
                    value={profileSettings.phone}
                    onChange={e =>
                      setProfileSettings({ ...profileSettings, phone: e.target.value })
                    }
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <FormControl fullWidth>
                    <InputLabel>Timezone</InputLabel>
                    <Select
                      value={profileSettings.timezone}
                      onChange={e =>
                        setProfileSettings({ ...profileSettings, timezone: e.target.value })
                      }
                      label="Timezone"
                    >
                      <MenuItem value="UTC">UTC</MenuItem>
                      <MenuItem value="EST">Eastern Time</MenuItem>
                      <MenuItem value="PST">Pacific Time</MenuItem>
                      <MenuItem value="GMT">Greenwich Mean Time</MenuItem>
                    </Select>
                  </FormControl>
                </Grid>
                <Grid item xs={12} sm={6}>
                  <FormControl fullWidth>
                    <InputLabel>Language</InputLabel>
                    <Select
                      value={profileSettings.language}
                      onChange={e =>
                        setProfileSettings({ ...profileSettings, language: e.target.value })
                      }
                      label="Language"
                    >
                      <MenuItem value="en">English</MenuItem>
                      <MenuItem value="es">Spanish</MenuItem>
                      <MenuItem value="fr">French</MenuItem>
                      <MenuItem value="de">German</MenuItem>
                    </Select>
                  </FormControl>
                </Grid>
              </Grid>
            </CardContent>
            <CardActions>
              <Button variant="contained" startIcon={<SaveIcon />} onClick={handleSaveProfile}>
                Save Profile
              </Button>
            </CardActions>
          </Card>
        </TabPanel>

        <TabPanel value={activeTab} index={1}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Security Settings
              </Typography>
              <Box sx={{ mt: 2 }}>
                <FormControlLabel
                  control={
                    <Switch
                      checked={securitySettings.twoFactorEnabled}
                      onChange={e =>
                        setSecuritySettings({
                          ...securitySettings,
                          twoFactorEnabled: e.target.checked,
                        })
                      }
                    />
                  }
                  label="Enable Two-Factor Authentication"
                />
              </Box>
              <Box sx={{ mt: 2 }}>
                <TextField
                  fullWidth
                  label="Session Timeout (minutes)"
                  type="number"
                  value={securitySettings.sessionTimeout}
                  onChange={e =>
                    setSecuritySettings({
                      ...securitySettings,
                      sessionTimeout: parseInt(e.target.value),
                    })
                  }
                  sx={{ maxWidth: 300 }}
                />
              </Box>
              <Box sx={{ mt: 2 }}>
                <TextField
                  fullWidth
                  label="Password Expiry (days)"
                  type="number"
                  value={securitySettings.passwordExpiry}
                  onChange={e =>
                    setSecuritySettings({
                      ...securitySettings,
                      passwordExpiry: parseInt(e.target.value),
                    })
                  }
                  sx={{ maxWidth: 300 }}
                />
              </Box>
              <Box sx={{ mt: 2 }}>
                <FormControlLabel
                  control={
                    <Switch
                      checked={securitySettings.loginNotifications}
                      onChange={e =>
                        setSecuritySettings({
                          ...securitySettings,
                          loginNotifications: e.target.checked,
                        })
                      }
                    />
                  }
                  label="Login Notifications"
                />
              </Box>
            </CardContent>
            <CardActions>
              <Button variant="contained" startIcon={<SaveIcon />} onClick={handleSaveSecurity}>
                Save Security Settings
              </Button>
            </CardActions>
          </Card>
        </TabPanel>

        <TabPanel value={activeTab} index={2}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Notification Preferences
              </Typography>
              <Box sx={{ mt: 2 }}>
                <FormControlLabel
                  control={
                    <Switch
                      checked={notificationSettings.emailNotifications}
                      onChange={e =>
                        setNotificationSettings({
                          ...notificationSettings,
                          emailNotifications: e.target.checked,
                        })
                      }
                    />
                  }
                  label="Email Notifications"
                />
              </Box>
              <Box sx={{ mt: 1 }}>
                <FormControlLabel
                  control={
                    <Switch
                      checked={notificationSettings.smsNotifications}
                      onChange={e =>
                        setNotificationSettings({
                          ...notificationSettings,
                          smsNotifications: e.target.checked,
                        })
                      }
                    />
                  }
                  label="SMS Notifications"
                />
              </Box>
              <Box sx={{ mt: 1 }}>
                <FormControlLabel
                  control={
                    <Switch
                      checked={notificationSettings.pushNotifications}
                      onChange={e =>
                        setNotificationSettings({
                          ...notificationSettings,
                          pushNotifications: e.target.checked,
                        })
                      }
                    />
                  }
                  label="Push Notifications"
                />
              </Box>
              <Divider sx={{ my: 2 }} />
              <Typography variant="subtitle2" gutterBottom>
                Alert Types
              </Typography>
              <Box sx={{ mt: 1 }}>
                <FormControlLabel
                  control={
                    <Switch
                      checked={notificationSettings.transactionAlerts}
                      onChange={e =>
                        setNotificationSettings({
                          ...notificationSettings,
                          transactionAlerts: e.target.checked,
                        })
                      }
                    />
                  }
                  label="Transaction Alerts"
                />
              </Box>
              <Box sx={{ mt: 1 }}>
                <FormControlLabel
                  control={
                    <Switch
                      checked={notificationSettings.securityAlerts}
                      onChange={e =>
                        setNotificationSettings({
                          ...notificationSettings,
                          securityAlerts: e.target.checked,
                        })
                      }
                    />
                  }
                  label="Security Alerts"
                />
              </Box>
              <Box sx={{ mt: 1 }}>
                <FormControlLabel
                  control={
                    <Switch
                      checked={notificationSettings.marketingEmails}
                      onChange={e =>
                        setNotificationSettings({
                          ...notificationSettings,
                          marketingEmails: e.target.checked,
                        })
                      }
                    />
                  }
                  label="Marketing Emails"
                />
              </Box>
            </CardContent>
            <CardActions>
              <Button
                variant="contained"
                startIcon={<SaveIcon />}
                onClick={handleSaveNotifications}
              >
                Save Notification Settings
              </Button>
            </CardActions>
          </Card>
        </TabPanel>

        <TabPanel value={activeTab} index={3}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Business Information
              </Typography>
              <Grid container spacing={2}>
                <Grid item xs={12} sm={6}>
                  <TextField
                    fullWidth
                    label="Company Name"
                    value={businessSettings.companyName}
                    onChange={e =>
                      setBusinessSettings({ ...businessSettings, companyName: e.target.value })
                    }
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <FormControl fullWidth>
                    <InputLabel>Business Type</InputLabel>
                    <Select
                      value={businessSettings.businessType}
                      onChange={e =>
                        setBusinessSettings({ ...businessSettings, businessType: e.target.value })
                      }
                      label="Business Type"
                    >
                      <MenuItem value="fintech">Fintech</MenuItem>
                      <MenuItem value="ecommerce">E-commerce</MenuItem>
                      <MenuItem value="saas">SaaS</MenuItem>
                      <MenuItem value="marketplace">Marketplace</MenuItem>
                    </Select>
                  </FormControl>
                </Grid>
                <Grid item xs={12} sm={6}>
                  <FormControl fullWidth>
                    <InputLabel>Country</InputLabel>
                    <Select
                      value={businessSettings.country}
                      onChange={e =>
                        setBusinessSettings({ ...businessSettings, country: e.target.value })
                      }
                      label="Country"
                    >
                      <MenuItem value="US">United States</MenuItem>
                      <MenuItem value="CA">Canada</MenuItem>
                      <MenuItem value="UK">United Kingdom</MenuItem>
                      <MenuItem value="DE">Germany</MenuItem>
                    </Select>
                  </FormControl>
                </Grid>
                <Grid item xs={12} sm={6}>
                  <FormControl fullWidth>
                    <InputLabel>Currency</InputLabel>
                    <Select
                      value={businessSettings.currency}
                      onChange={e =>
                        setBusinessSettings({ ...businessSettings, currency: e.target.value })
                      }
                      label="Currency"
                    >
                      <MenuItem value="USD">USD</MenuItem>
                      <MenuItem value="EUR">EUR</MenuItem>
                      <MenuItem value="GBP">GBP</MenuItem>
                      <MenuItem value="CAD">CAD</MenuItem>
                    </Select>
                  </FormControl>
                </Grid>
                <Grid item xs={12} sm={6}>
                  <TextField
                    fullWidth
                    label="Tax ID"
                    value={businessSettings.taxId}
                    onChange={e =>
                      setBusinessSettings({ ...businessSettings, taxId: e.target.value })
                    }
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <TextField
                    fullWidth
                    label="Website"
                    value={businessSettings.website}
                    onChange={e =>
                      setBusinessSettings({ ...businessSettings, website: e.target.value })
                    }
                  />
                </Grid>
              </Grid>
            </CardContent>
            <CardActions>
              <Button variant="contained" startIcon={<SaveIcon />} onClick={handleSaveBusiness}>
                Save Business Settings
              </Button>
            </CardActions>
          </Card>
        </TabPanel>
      </Paper>
    </Box>
  );
};

export default Settings;
