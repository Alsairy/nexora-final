import React, { useState, useEffect } from 'react';
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
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Tabs,
  Tab,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Divider,
  Alert,
  LinearProgress,
  Switch,
  FormControlLabel,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Badge,
  IconButton,
  Tooltip,
  Stepper,
  Step,
  StepLabel,
  StepContent,
  Avatar,
  AvatarGroup,
  Drawer,
  AppBar,
  Toolbar,
  Menu,
  MenuList,
  MenuItem as MenuItemComponent,
  ListItemButton,
  Collapse,
} from '@mui/material';
import {
  Sms,
  Send,
  CheckCircle,
  Error,
  Pending,
  TrendingUp,
  Campaign,
  Analytics,
  Security,
  ContactPhone,
  Schedule,
  WhatsApp,
  Email,
  Phone,
  ExpandMore,
  Add,
  Edit,
  Delete,
  Visibility,
  Download,
  Upload,
  FilterList,
  Search,
  Refresh,
  Settings,
  Warning,
  Info,
  PlayArrow,
  Pause,
  Stop,
  BarChart,
  PieChart,
  Timeline,
  Group,
  PersonAdd,
  Segment,
  Approval,
  Notifications,
  Shield,
  Language,
  AccessTime,
  LocationOn,
  Business,
  VerifiedUser,
  DragIndicator,
  ExpandLess,
  ExpandMore as ExpandMoreIcon,
} from '@mui/icons-material';

interface SmsMessage {
  id: string;
  recipient: string;
  message: string;
  status: 'sent' | 'pending' | 'failed' | 'delivered' | 'cancelled' | 'scheduled';
  timestamp: string;
  cost: number;
  senderId: string;
  messageType: string;
  provider: string;
  segments: number;
  deliveredAt?: string;
  errorCode?: string;
  errorMessage?: string;
  campaignId?: string;
  templateId?: string;
}

interface Campaign {
  id: string;
  name: string;
  description: string;
  status: 'draft' | 'scheduled' | 'running' | 'paused' | 'completed' | 'cancelled';
  channels: string[];
  targetAudience: string;
  scheduledTime?: string;
  createdAt: string;
  totalRecipients: number;
  sentMessages: number;
  deliveredMessages: number;
  failedMessages: number;
  estimatedCost: number;
  actualCost: number;
}

interface Template {
  id: string;
  name: string;
  content: string;
  type: 'transactional' | 'promotional' | 'otp' | 'notification';
  status: 'draft' | 'pending_approval' | 'approved' | 'rejected';
  language: string;
  variables: string[];
  approvedBy?: string;
  approvedAt?: string;
  rejectionReason?: string;
  usageCount: number;
}

interface Contact {
  id: string;
  phoneNumber: string;
  firstName?: string;
  lastName?: string;
  email?: string;
  tags: string[];
  segments: string[];
  isOptedOut: boolean;
  isDnd: boolean;
  createdAt: string;
  lastMessageAt?: string;
}

interface ComplianceAlert {
  id: string;
  type: 'dnd_violation' | 'time_window' | 'content_filter' | 'sender_id' | 'rate_limit';
  severity: 'low' | 'medium' | 'high' | 'critical';
  message: string;
  timestamp: string;
  resolved: boolean;
  affectedMessages: number;
}

interface AnalyticsData {
  totalMessages: number;
  deliveredMessages: number;
  failedMessages: number;
  deliveryRate: number;
  totalCost: number;
  averageCost: number;
  topProviders: Array<{ name: string; count: number; rate: number }>;
  messagesByHour: Array<{ hour: string; count: number }>;
  messagesByType: Array<{ type: string; count: number; percentage: number }>;
  complianceScore: number;
}

const SmsGateway: React.FC = () => {
  const [activeTab, setActiveTab] = useState(0);
  const [openDialog, setOpenDialog] = useState(false);
  const [dialogType, setDialogType] = useState<'message' | 'campaign' | 'template' | 'contact'>('message');
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [selectedCampaign, setSelectedCampaign] = useState<Campaign | null>(null);
  const [complianceAlerts, setComplianceAlerts] = useState<ComplianceAlert[]>([]);
  const [realTimeData, setRealTimeData] = useState<AnalyticsData | null>(null);
  
  const [newMessage, setNewMessage] = useState({
    recipient: '',
    message: '',
    type: 'single',
    senderId: '',
    messageType: 'transactional',
    scheduledTime: '',
    templateId: '',
    channels: ['sms'],
  });

  const [newCampaign, setNewCampaign] = useState({
    name: '',
    description: '',
    channels: ['sms'],
    targetAudience: '',
    scheduledTime: '',
    templateId: '',
    messageContent: '',
    senderId: '',
  });

  const [newTemplate, setNewTemplate] = useState({
    name: '',
    content: '',
    type: 'transactional',
    language: 'en',
    variables: [] as string[],
  });

  const [newContact, setNewContact] = useState({
    phoneNumber: '',
    firstName: '',
    lastName: '',
    email: '',
    tags: [] as string[],
    segments: [] as string[],
  });

  const [filters, setFilters] = useState({
    status: 'all',
    dateRange: '7d',
    messageType: 'all',
    provider: 'all',
    campaign: 'all',
  });

  const messages: SmsMessage[] = [
    {
      id: 'SMS-001',
      recipient: '+966501234567',
      message: 'Your verification code is 123456',
      status: 'delivered',
      timestamp: '2024-06-18 10:30:00',
      deliveredAt: '2024-06-18 10:30:15',
      cost: 0.15,
      senderId: 'NEXORA',
      messageType: 'otp',
      provider: 'STC',
      segments: 1,
      templateId: 'TPL-001',
    },
    {
      id: 'SMS-002',
      recipient: '+966502345678',
      message: 'Payment confirmation: 1,250.00 SAR received for transaction #TX123456',
      status: 'delivered',
      timestamp: '2024-06-18 09:15:00',
      deliveredAt: '2024-06-18 09:15:08',
      cost: 0.30,
      senderId: 'NEXORA',
      messageType: 'transactional',
      provider: 'Mobily',
      segments: 2,
      campaignId: 'CMP-001',
    },
    {
      id: 'SMS-003',
      recipient: '+966503456789',
      message: 'Welcome to Nexora platform! Start your fintech journey today.',
      status: 'scheduled',
      timestamp: '2024-06-18 08:45:00',
      cost: 0.15,
      senderId: 'NEXORA',
      messageType: 'promotional',
      provider: 'Zain',
      segments: 1,
      campaignId: 'CMP-002',
    },
    {
      id: 'SMS-004',
      recipient: '+966504567890',
      message: 'Your loan application has been approved. Amount: 50,000 SAR',
      status: 'failed',
      timestamp: '2024-06-18 08:00:00',
      cost: 0.00,
      senderId: 'NEXORA',
      messageType: 'notification',
      provider: 'STC',
      segments: 1,
      errorCode: 'DND_VIOLATION',
      errorMessage: 'Recipient is on Do Not Disturb list',
    },
  ];

  const campaigns: Campaign[] = [
    {
      id: 'CMP-001',
      name: 'Payment Confirmations Q2',
      description: 'Automated payment confirmation messages for Q2 transactions',
      status: 'running',
      channels: ['sms', 'email'],
      targetAudience: 'Active Users',
      createdAt: '2024-06-01 09:00:00',
      totalRecipients: 15420,
      sentMessages: 12350,
      deliveredMessages: 11890,
      failedMessages: 460,
      estimatedCost: 1850.50,
      actualCost: 1642.30,
    },
    {
      id: 'CMP-002',
      name: 'New User Onboarding',
      description: 'Welcome series for new platform users',
      status: 'scheduled',
      channels: ['sms', 'whatsapp', 'email'],
      targetAudience: 'New Registrations',
      scheduledTime: '2024-06-20 10:00:00',
      createdAt: '2024-06-15 14:30:00',
      totalRecipients: 2500,
      sentMessages: 0,
      deliveredMessages: 0,
      failedMessages: 0,
      estimatedCost: 625.00,
      actualCost: 0.00,
    },
    {
      id: 'CMP-003',
      name: 'Ramadan Promotion',
      description: 'Special offers during Ramadan period',
      status: 'completed',
      channels: ['sms', 'whatsapp'],
      targetAudience: 'Premium Users',
      createdAt: '2024-03-15 08:00:00',
      totalRecipients: 8750,
      sentMessages: 8750,
      deliveredMessages: 8234,
      failedMessages: 516,
      estimatedCost: 1312.50,
      actualCost: 1235.10,
    },
  ];

  const templates: Template[] = [
    {
      id: 'TPL-001',
      name: 'OTP Verification',
      content: 'Your verification code is {{code}}. Valid for 5 minutes. Do not share with anyone.',
      type: 'otp',
      status: 'approved',
      language: 'en',
      variables: ['code'],
      approvedBy: 'Compliance Team',
      approvedAt: '2024-06-01 10:00:00',
      usageCount: 15420,
    },
    {
      id: 'TPL-002',
      name: 'Payment Confirmation',
      content: 'Payment confirmed: {{amount}} SAR received for transaction #{{transactionId}}. Thank you!',
      type: 'transactional',
      status: 'approved',
      language: 'en',
      variables: ['amount', 'transactionId'],
      approvedBy: 'Compliance Team',
      approvedAt: '2024-06-01 10:15:00',
      usageCount: 8750,
    },
    {
      id: 'TPL-003',
      name: 'Welcome Message Arabic',
      content: 'مرحباً بك في منصة نكسورا! ابدأ رحلتك المالية اليوم. {{firstName}}',
      type: 'promotional',
      status: 'pending_approval',
      language: 'ar',
      variables: ['firstName'],
      usageCount: 0,
    },
    {
      id: 'TPL-004',
      name: 'Loan Approval',
      content: 'Congratulations! Your loan of {{amount}} SAR has been approved. Reference: {{refNumber}}',
      type: 'notification',
      status: 'rejected',
      language: 'en',
      variables: ['amount', 'refNumber'],
      rejectionReason: 'Missing compliance disclaimer',
      usageCount: 0,
    },
  ];

  const contacts: Contact[] = [
    {
      id: 'CNT-001',
      phoneNumber: '+966501234567',
      firstName: 'Ahmed',
      lastName: 'Al-Rashid',
      email: 'ahmed.rashid@email.com',
      tags: ['premium', 'active'],
      segments: ['High Value', 'Saudi Arabia'],
      isOptedOut: false,
      isDnd: false,
      createdAt: '2024-01-15 09:30:00',
      lastMessageAt: '2024-06-18 10:30:00',
    },
    {
      id: 'CNT-002',
      phoneNumber: '+966502345678',
      firstName: 'Fatima',
      lastName: 'Al-Zahra',
      email: 'fatima.zahra@email.com',
      tags: ['new_user', 'verified'],
      segments: ['New Users', 'Riyadh'],
      isOptedOut: false,
      isDnd: false,
      createdAt: '2024-06-10 14:20:00',
      lastMessageAt: '2024-06-18 09:15:00',
    },
    {
      id: 'CNT-003',
      phoneNumber: '+966503456789',
      firstName: 'Mohammed',
      lastName: 'Al-Saud',
      email: 'mohammed.saud@email.com',
      tags: ['business', 'enterprise'],
      segments: ['Business Users', 'Jeddah'],
      isOptedOut: true,
      isDnd: false,
      createdAt: '2024-03-20 11:45:00',
    },
  ];

  useEffect(() => {
    setComplianceAlerts([
      {
        id: 'ALT-001',
        type: 'dnd_violation',
        severity: 'high',
        message: '15 messages blocked due to DND violations in the last hour',
        timestamp: '2024-06-18 10:45:00',
        resolved: false,
        affectedMessages: 15,
      },
      {
        id: 'ALT-002',
        type: 'time_window',
        severity: 'medium',
        message: 'Promotional messages scheduled outside allowed time window',
        timestamp: '2024-06-18 09:30:00',
        resolved: true,
        affectedMessages: 8,
      },
      {
        id: 'ALT-003',
        type: 'sender_id',
        severity: 'critical',
        message: 'Sender ID "PROMO" not approved for promotional messages',
        timestamp: '2024-06-18 08:15:00',
        resolved: false,
        affectedMessages: 25,
      },
    ]);

    setRealTimeData({
      totalMessages: 45230,
      deliveredMessages: 42150,
      failedMessages: 3080,
      deliveryRate: 93.2,
      totalCost: 6784.50,
      averageCost: 0.15,
      topProviders: [
        { name: 'STC', count: 18500, rate: 94.5 },
        { name: 'Mobily', count: 15200, rate: 92.8 },
        { name: 'Zain', count: 11530, rate: 91.2 },
      ],
      messagesByHour: [
        { hour: '00:00', count: 120 },
        { hour: '01:00', count: 85 },
        { hour: '02:00', count: 45 },
        { hour: '03:00', count: 30 },
        { hour: '04:00', count: 25 },
        { hour: '05:00', count: 40 },
        { hour: '06:00', count: 180 },
        { hour: '07:00', count: 350 },
        { hour: '08:00', count: 520 },
        { hour: '09:00', count: 680 },
        { hour: '10:00', count: 750 },
        { hour: '11:00', count: 620 },
      ],
      messagesByType: [
        { type: 'OTP', count: 18500, percentage: 40.9 },
        { type: 'Transactional', count: 15200, percentage: 33.6 },
        { type: 'Promotional', count: 8230, percentage: 18.2 },
        { type: 'Notification', count: 3300, percentage: 7.3 },
      ],
      complianceScore: 96.8,
    });
  }, []);

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'sent':
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
      case 'sent':
        return 'success';
      case 'pending':
        return 'warning';
      case 'failed':
        return 'error';
      default:
        return 'default';
    }
  };

  const handleSendMessage = () => {
    console.log('Sending message:', newMessage);
    setOpenDialog(false);
    setNewMessage({ 
      recipient: '', 
      message: '', 
      type: 'single',
      senderId: '',
      messageType: 'transactional',
      scheduledTime: '',
      templateId: '',
      channels: ['sms'],
    });
  };

  const handleCreateCampaign = () => {
    console.log('Creating campaign:', newCampaign);
    setOpenDialog(false);
    setNewCampaign({
      name: '',
      description: '',
      channels: ['sms'],
      targetAudience: '',
      scheduledTime: '',
      templateId: '',
      messageContent: '',
      senderId: '',
    });
  };

  const handleCreateTemplate = () => {
    console.log('Creating template:', newTemplate);
    setOpenDialog(false);
    setNewTemplate({
      name: '',
      content: '',
      type: 'transactional',
      language: 'en',
      variables: [],
    });
  };

  const handleAddContact = () => {
    console.log('Adding contact:', newContact);
    setOpenDialog(false);
    setNewContact({
      phoneNumber: '',
      firstName: '',
      lastName: '',
      email: '',
      tags: [],
      segments: [],
    });
  };

  const openCreateDialog = (type: 'message' | 'campaign' | 'template' | 'contact') => {
    setDialogType(type);
    setOpenDialog(true);
  };

  const totalMessages = realTimeData?.totalMessages || messages.length;
  const deliveredMessages = realTimeData?.deliveredMessages || messages.filter(msg => msg.status === 'delivered').length;
  const totalCost = realTimeData?.totalCost || messages.reduce((sum, msg) => sum + msg.cost, 0);
  const deliveryRate = realTimeData?.deliveryRate || Math.round((deliveredMessages / totalMessages) * 100);
  const complianceScore = realTimeData?.complianceScore || 96.8;

  const unreadAlerts = complianceAlerts.filter(alert => !alert.resolved).length;

  const renderCampaigns = () => (
    <Grid container spacing={3}>
      <Grid item xs={12}>
        <Paper sx={{ p: 3 }}>
          <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 3 }}>
            <Typography variant="h6">Campaign Management</Typography>
            <Button
              variant="contained"
              startIcon={<Add />}
              onClick={() => openCreateDialog('campaign')}
            >
              Create Campaign
            </Button>
          </Box>
          
          <Grid container spacing={2}>
            {campaigns.map((campaign) => (
              <Grid item xs={12} md={6} lg={4} key={campaign.id}>
                <Card>
                  <CardContent>
                    <Box display="flex" justifyContent="space-between" alignItems="flex-start" mb={2}>
                      <Typography variant="h6" component="div">
                        {campaign.name}
                      </Typography>
                      <Chip
                        label={campaign.status.toUpperCase()}
                        color={
                          campaign.status === 'running' ? 'success' :
                          campaign.status === 'scheduled' ? 'info' :
                          campaign.status === 'completed' ? 'default' : 'warning'
                        }
                        size="small"
                      />
                    </Box>
                    
                    <Typography variant="body2" color="text.secondary" mb={2}>
                      {campaign.description}
                    </Typography>
                    
                    <Box mb={2}>
                      <Typography variant="body2" gutterBottom>
                        Channels: {campaign.channels.join(', ')}
                      </Typography>
                      <Typography variant="body2" gutterBottom>
                        Target: {campaign.targetAudience}
                      </Typography>
                      <Typography variant="body2">
                        Recipients: {campaign.totalRecipients.toLocaleString()}
                      </Typography>
                    </Box>
                    
                    <Box mb={2}>
                      <Box display="flex" justifyContent="space-between" mb={1}>
                        <Typography variant="body2">Progress</Typography>
                        <Typography variant="body2">
                          {Math.round((campaign.sentMessages / campaign.totalRecipients) * 100)}%
                        </Typography>
                      </Box>
                      <LinearProgress
                        variant="determinate"
                        value={(campaign.sentMessages / campaign.totalRecipients) * 100}
                        sx={{ height: 6, borderRadius: 3 }}
                      />
                    </Box>
                    
                    <Box display="flex" justifyContent="space-between" alignItems="center">
                      <Typography variant="body2" color="text.secondary">
                        Cost: {campaign.actualCost.toFixed(2)} SAR
                      </Typography>
                      <Box>
                        <IconButton size="small">
                          <Visibility />
                        </IconButton>
                        <IconButton size="small">
                          <Edit />
                        </IconButton>
                        {campaign.status === 'running' && (
                          <IconButton size="small" color="warning">
                            <Pause />
                          </IconButton>
                        )}
                      </Box>
                    </Box>
                  </CardContent>
                </Card>
              </Grid>
            ))}
          </Grid>
        </Paper>
      </Grid>
    </Grid>
  );

  const renderTemplates = () => (
    <Grid container spacing={3}>
      <Grid item xs={12}>
        <Paper sx={{ p: 3 }}>
          <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 3 }}>
            <Typography variant="h6">Message Templates</Typography>
            <Button
              variant="contained"
              startIcon={<Add />}
              onClick={() => openCreateDialog('template')}
            >
              Create Template
            </Button>
          </Box>
          
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Template Name</TableCell>
                  <TableCell>Type</TableCell>
                  <TableCell>Language</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Usage Count</TableCell>
                  <TableCell>Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {templates.map((template) => (
                  <TableRow key={template.id}>
                    <TableCell>
                      <Typography variant="body2" fontWeight="medium">
                        {template.name}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {template.content.substring(0, 50)}...
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={template.type.toUpperCase()}
                        size="small"
                        color={template.type === 'otp' ? 'primary' : 'default'}
                      />
                    </TableCell>
                    <TableCell>{template.language.toUpperCase()}</TableCell>
                    <TableCell>
                      <Chip
                        label={template.status.replace('_', ' ').toUpperCase()}
                        color={
                          template.status === 'approved' ? 'success' :
                          template.status === 'pending_approval' ? 'warning' :
                          template.status === 'rejected' ? 'error' : 'default'
                        }
                        size="small"
                      />
                    </TableCell>
                    <TableCell>{template.usageCount.toLocaleString()}</TableCell>
                    <TableCell>
                      <IconButton size="small">
                        <Visibility />
                      </IconButton>
                      <IconButton size="small">
                        <Edit />
                      </IconButton>
                      {template.status === 'pending_approval' && (
                        <IconButton size="small" color="primary">
                          <Approval />
                        </IconButton>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Paper>
      </Grid>
    </Grid>
  );

  const renderContacts = () => (
    <Grid container spacing={3}>
      <Grid item xs={12}>
        <Paper sx={{ p: 3 }}>
          <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 3 }}>
            <Typography variant="h6">Contact Management</Typography>
            <Box display="flex" gap={1}>
              <Button variant="outlined" startIcon={<Upload />}>
                Import Contacts
              </Button>
              <Button
                variant="contained"
                startIcon={<PersonAdd />}
                onClick={() => openCreateDialog('contact')}
              >
                Add Contact
              </Button>
            </Box>
          </Box>
          
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Contact Info</TableCell>
                  <TableCell>Phone Number</TableCell>
                  <TableCell>Segments</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Last Message</TableCell>
                  <TableCell>Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {contacts.map((contact) => (
                  <TableRow key={contact.id}>
                    <TableCell>
                      <Box display="flex" alignItems="center">
                        <Avatar sx={{ mr: 2 }}>
                          {contact.firstName?.[0] || contact.phoneNumber[0]}
                        </Avatar>
                        <Box>
                          <Typography variant="body2" fontWeight="medium">
                            {contact.firstName} {contact.lastName}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {contact.email}
                          </Typography>
                        </Box>
                      </Box>
                    </TableCell>
                    <TableCell>{contact.phoneNumber}</TableCell>
                    <TableCell>
                      <Box display="flex" gap={0.5} flexWrap="wrap">
                        {contact.segments.map((segment) => (
                          <Chip key={segment} label={segment} size="small" />
                        ))}
                      </Box>
                    </TableCell>
                    <TableCell>
                      <Box display="flex" gap={1}>
                        {contact.isOptedOut && (
                          <Chip label="Opted Out" color="error" size="small" />
                        )}
                        {contact.isDnd && (
                          <Chip label="DND" color="warning" size="small" />
                        )}
                        {!contact.isOptedOut && !contact.isDnd && (
                          <Chip label="Active" color="success" size="small" />
                        )}
                      </Box>
                    </TableCell>
                    <TableCell>
                      {contact.lastMessageAt ? 
                        new Date(contact.lastMessageAt).toLocaleDateString() : 
                        'Never'
                      }
                    </TableCell>
                    <TableCell>
                      <IconButton size="small">
                        <Visibility />
                      </IconButton>
                      <IconButton size="small">
                        <Edit />
                      </IconButton>
                      <IconButton size="small">
                        <Send />
                      </IconButton>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Paper>
      </Grid>
    </Grid>
  );

  const renderAnalytics = () => (
    <Grid container spacing={3}>
      <Grid item xs={12} md={8}>
        <Paper sx={{ p: 3, mb: 3 }}>
          <Typography variant="h6" gutterBottom>
            Message Volume Trends
          </Typography>
          <Box height={300} display="flex" alignItems="center" justifyContent="center">
            <Typography color="text.secondary">
              Chart visualization would be implemented here with a charting library
            </Typography>
          </Box>
        </Paper>
        
        <Paper sx={{ p: 3 }}>
          <Typography variant="h6" gutterBottom>
            Provider Performance Comparison
          </Typography>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Provider</TableCell>
                  <TableCell>Messages Sent</TableCell>
                  <TableCell>Delivery Rate</TableCell>
                  <TableCell>Avg Cost</TableCell>
                  <TableCell>Avg Response Time</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {realTimeData?.topProviders.map((provider) => (
                  <TableRow key={provider.name}>
                    <TableCell>{provider.name}</TableCell>
                    <TableCell>{provider.count.toLocaleString()}</TableCell>
                    <TableCell>
                      <Box display="flex" alignItems="center">
                        <Typography variant="body2" sx={{ mr: 1 }}>
                          {provider.rate}%
                        </Typography>
                        <LinearProgress
                          variant="determinate"
                          value={provider.rate}
                          sx={{ width: 60, height: 4 }}
                          color={provider.rate > 95 ? 'success' : provider.rate > 90 ? 'warning' : 'error'}
                        />
                      </Box>
                    </TableCell>
                    <TableCell>0.15 SAR</TableCell>
                    <TableCell>2.3s</TableCell>
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
            Message Types Distribution
          </Typography>
          <Box>
            {realTimeData?.messagesByType.map((type, index) => (
              <Box key={type.type} mb={2}>
                <Box display="flex" justifyContent="space-between" mb={1}>
                  <Typography variant="body2">{type.type}</Typography>
                  <Typography variant="body2" fontWeight="medium">
                    {type.count.toLocaleString()} ({type.percentage}%)
                  </Typography>
                </Box>
                <LinearProgress
                  variant="determinate"
                  value={type.percentage}
                  sx={{ height: 8, borderRadius: 4 }}
                  color={index === 0 ? 'primary' : index === 1 ? 'secondary' : 'info'}
                />
              </Box>
            ))}
          </Box>
        </Paper>
        
        <Paper sx={{ p: 3 }}>
          <Typography variant="h6" gutterBottom>
            Real-time Metrics
          </Typography>
          <List>
            <ListItem>
              <ListItemText
                primary="Messages Today"
                secondary={realTimeData?.totalMessages.toLocaleString()}
              />
            </ListItem>
            <ListItem>
              <ListItemText
                primary="Current Delivery Rate"
                secondary={`${realTimeData?.deliveryRate}%`}
              />
            </ListItem>
            <ListItem>
              <ListItemText
                primary="Total Cost Today"
                secondary={`${realTimeData?.totalCost.toFixed(2)} SAR`}
              />
            </ListItem>
            <ListItem>
              <ListItemText
                primary="Compliance Score"
                secondary={`${realTimeData?.complianceScore}%`}
              />
            </ListItem>
          </List>
        </Paper>
      </Grid>
    </Grid>
  );

  const renderCompliance = () => (
    <Grid container spacing={3}>
      <Grid item xs={12}>
        <Alert severity="info" sx={{ mb: 3 }}>
          <Typography variant="body2">
            KSA Compliance Dashboard - Monitor your messaging compliance with Saudi Arabian regulations
          </Typography>
        </Alert>
      </Grid>
      
      <Grid item xs={12} md={8}>
        <Paper sx={{ p: 3 }}>
          <Typography variant="h6" gutterBottom>
            Compliance Alerts
          </Typography>
          <List>
            {complianceAlerts.map((alert) => (
              <ListItem key={alert.id} divider>
                <ListItemIcon>
                  <Warning 
                    color={
                      alert.severity === 'critical' ? 'error' :
                      alert.severity === 'high' ? 'warning' :
                      alert.severity === 'medium' ? 'info' : 'inherit'
                    }
                  />
                </ListItemIcon>
                <ListItemText
                  primary={alert.message}
                  secondary={
                    <Box>
                      <Typography variant="caption" display="block">
                        {alert.timestamp} • {alert.affectedMessages} messages affected
                      </Typography>
                      <Chip
                        label={alert.severity.toUpperCase()}
                        size="small"
                        color={
                          alert.severity === 'critical' ? 'error' :
                          alert.severity === 'high' ? 'warning' :
                          alert.severity === 'medium' ? 'info' : 'default'
                        }
                        sx={{ mt: 1 }}
                      />
                    </Box>
                  }
                />
                <Box>
                  {!alert.resolved && (
                    <Button size="small" variant="outlined">
                      Resolve
                    </Button>
                  )}
                </Box>
              </ListItem>
            ))}
          </List>
        </Paper>
      </Grid>
      
      <Grid item xs={12} md={4}>
        <Paper sx={{ p: 3, mb: 3 }}>
          <Typography variant="h6" gutterBottom>
            Compliance Score
          </Typography>
          <Box textAlign="center" mb={2}>
            <Typography variant="h2" color="success.main">
              {complianceScore}%
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Overall Compliance Rating
            </Typography>
          </Box>
          <LinearProgress
            variant="determinate"
            value={complianceScore}
            sx={{ height: 8, borderRadius: 4 }}
            color="success"
          />
        </Paper>
        
        <Paper sx={{ p: 3 }}>
          <Typography variant="h6" gutterBottom>
            Compliance Checklist
          </Typography>
          <List dense>
            <ListItem>
              <ListItemIcon>
                <CheckCircle color="success" />
              </ListItemIcon>
              <ListItemText primary="DND List Updated" />
            </ListItem>
            <ListItem>
              <ListItemIcon>
                <CheckCircle color="success" />
              </ListItemIcon>
              <ListItemText primary="Sender IDs Approved" />
            </ListItem>
            <ListItem>
              <ListItemIcon>
                <CheckCircle color="success" />
              </ListItemIcon>
              <ListItemText primary="Time Window Compliance" />
            </ListItem>
            <ListItem>
              <ListItemIcon>
                <Warning color="warning" />
              </ListItemIcon>
              <ListItemText primary="Content Filter Review" />
            </ListItem>
          </List>
        </Paper>
      </Grid>
    </Grid>
  );

  const renderTabContent = () => {
    switch (activeTab) {
      case 0: // Dashboard
        return renderDashboard();
      case 1: // Campaigns
        return renderCampaigns();
      case 2: // Templates
        return renderTemplates();
      case 3: // Contacts
        return renderContacts();
      case 4: // Analytics
        return renderAnalytics();
      case 5: // Compliance
        return renderCompliance();
      default:
        return renderDashboard();
    }
  };

  const renderDashboard = () => (
    <>
      {/* Real-time Alerts */}
      {unreadAlerts > 0 && (
        <Alert 
          severity="warning" 
          sx={{ mb: 3 }}
          action={
            <Button color="inherit" size="small" onClick={() => setActiveTab(5)}>
              View Details
            </Button>
          }
        >
          {unreadAlerts} compliance alert{unreadAlerts > 1 ? 's' : ''} require attention
        </Alert>
      )}

      {/* KPI Cards */}
      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography color="textSecondary" gutterBottom variant="h6">
                    Total Messages
                  </Typography>
                  <Typography variant="h4">
                    {totalMessages.toLocaleString()}
                  </Typography>
                  <Typography variant="body2" color="success.main">
                    +12.5% from last month
                  </Typography>
                </Box>
                <Sms color="primary" sx={{ fontSize: 40 }} />
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
                    Delivery Rate
                  </Typography>
                  <Typography variant="h4">
                    {deliveryRate}%
                  </Typography>
                  <LinearProgress 
                    variant="determinate" 
                    value={deliveryRate} 
                    sx={{ mt: 1, height: 6, borderRadius: 3 }}
                    color={deliveryRate > 95 ? 'success' : deliveryRate > 90 ? 'warning' : 'error'}
                  />
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
                    Total Cost
                  </Typography>
                  <Typography variant="h4">
                    {totalCost.toLocaleString()} SAR
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Avg: {(totalCost / totalMessages).toFixed(3)} SAR/msg
                  </Typography>
                </Box>
                <TrendingUp color="info" sx={{ fontSize: 40 }} />
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
                    Compliance Score
                  </Typography>
                  <Typography variant="h4" color={complianceScore > 95 ? 'success.main' : 'warning.main'}>
                    {complianceScore}%
                  </Typography>
                  <Box display="flex" alignItems="center" mt={1}>
                    <Shield color={complianceScore > 95 ? 'success' : 'warning'} sx={{ mr: 0.5, fontSize: 16 }} />
                    <Typography variant="body2" color="text.secondary">
                      KSA Compliant
                    </Typography>
                  </Box>
                </Box>
                <Security color={complianceScore > 95 ? 'success' : 'warning'} sx={{ fontSize: 40 }} />
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Quick Actions */}
      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12} md={8}>
          <Paper sx={{ p: 3 }}>
            <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 3 }}>
              <Typography variant="h6">
                Recent Messages
              </Typography>
              <Box display="flex" gap={1}>
                <Button
                  variant="outlined"
                  startIcon={<Campaign />}
                  onClick={() => openCreateDialog('campaign')}
                >
                  Create Campaign
                </Button>
                <Button
                  variant="contained"
                  startIcon={<Send />}
                  onClick={() => openCreateDialog('message')}
                >
                  Send Message
                </Button>
              </Box>
            </Box>
            
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Message ID</TableCell>
                    <TableCell>Recipient</TableCell>
                    <TableCell>Content</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Provider</TableCell>
                    <TableCell>Cost</TableCell>
                    <TableCell>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {messages.slice(0, 5).map((message) => (
                    <TableRow key={message.id}>
                      <TableCell>
                        <Typography variant="body2" fontWeight="medium">
                          {message.id}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {message.messageType}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2">
                          {message.recipient}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {message.senderId}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2">
                          {message.message.length > 40 
                            ? `${message.message.substring(0, 40)}...` 
                            : message.message}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {message.segments} segment{message.segments > 1 ? 's' : ''}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Chip
                          icon={getStatusIcon(message.status)}
                          label={message.status.toUpperCase()}
                          color={getStatusColor(message.status) as any}
                          size="small"
                        />
                        {message.errorMessage && (
                          <Tooltip title={message.errorMessage}>
                            <Warning color="error" sx={{ ml: 1, fontSize: 16 }} />
                          </Tooltip>
                        )}
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2">
                          {message.provider}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2" fontWeight="medium">
                          {message.cost.toFixed(2)} SAR
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <IconButton size="small">
                          <Visibility />
                        </IconButton>
                        {message.status === 'failed' && (
                          <IconButton size="small" color="primary">
                            <Refresh />
                          </IconButton>
                        )}
                      </TableCell>
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
              Active Campaigns
            </Typography>
            <List>
              {campaigns.filter(c => c.status === 'running').map((campaign) => (
                <ListItem key={campaign.id} disablePadding>
                  <ListItemButton onClick={() => setSelectedCampaign(campaign)}>
                    <ListItemIcon>
                      <Campaign color="primary" />
                    </ListItemIcon>
                    <ListItemText
                      primary={campaign.name}
                      secondary={`${campaign.sentMessages}/${campaign.totalRecipients} sent`}
                    />
                    <Box>
                      <LinearProgress 
                        variant="determinate" 
                        value={(campaign.sentMessages / campaign.totalRecipients) * 100}
                        sx={{ width: 60, height: 4 }}
                      />
                    </Box>
                  </ListItemButton>
                </ListItem>
              ))}
            </List>
          </Paper>
          
          <Paper sx={{ p: 3, mb: 3 }}>
            <Typography variant="h6" gutterBottom>
              Channel Distribution
            </Typography>
            <Box>
              {realTimeData?.messagesByType.map((type, index) => (
                <Box key={type.type} display="flex" alignItems="center" justifyContent="space-between" mb={1}>
                  <Typography variant="body2">{type.type}</Typography>
                  <Box display="flex" alignItems="center">
                    <Typography variant="body2" sx={{ mr: 1 }}>
                      {type.percentage}%
                    </Typography>
                    <LinearProgress 
                      variant="determinate" 
                      value={type.percentage}
                      sx={{ width: 60, height: 4 }}
                      color={index === 0 ? 'primary' : index === 1 ? 'secondary' : 'info'}
                    />
                  </Box>
                </Box>
              ))}
            </Box>
          </Paper>

          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              Provider Performance
            </Typography>
            <List>
              {realTimeData?.topProviders.map((provider) => (
                <ListItem key={provider.name} disablePadding>
                  <ListItemText
                    primary={provider.name}
                    secondary={`${provider.count.toLocaleString()} messages`}
                  />
                  <Box textAlign="right">
                    <Typography variant="body2" fontWeight="medium">
                      {provider.rate}%
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      delivery rate
                    </Typography>
                  </Box>
                </ListItem>
              ))}
            </List>
          </Paper>
        </Grid>
      </Grid>
    </>
  );

  return (
    <Container maxWidth="xl">
      <Box sx={{ mb: 4 }}>
        <Box display="flex" justifyContent="space-between" alignItems="center">
          <Box>
            <Typography variant="h4" component="h1" gutterBottom>
              CPaaS SMS Gateway
            </Typography>
            <Typography variant="body1" color="text.secondary">
              Enterprise messaging platform for the Saudi Arabian market
            </Typography>
          </Box>
          <Box display="flex" alignItems="center" gap={2}>
            <Badge badgeContent={unreadAlerts} color="error">
              <IconButton onClick={() => setActiveTab(5)}>
                <Notifications />
              </IconButton>
            </Badge>
            <IconButton onClick={() => setDrawerOpen(true)}>
              <Settings />
            </IconButton>
          </Box>
        </Box>
      </Box>

      {/* Navigation Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value={activeTab} onChange={(e, newValue) => setActiveTab(newValue)}>
          <Tab icon={<Analytics />} label="Dashboard" />
          <Tab icon={<Campaign />} label="Campaigns" />
          <Tab icon={<Schedule />} label="Templates" />
          <Tab icon={<ContactPhone />} label="Contacts" />
          <Tab icon={<BarChart />} label="Analytics" />
          <Tab 
            icon={
              <Badge badgeContent={unreadAlerts} color="error">
                <Security />
              </Badge>
            } 
            label="Compliance" 
          />
        </Tabs>
      </Box>

      {/* Tab Content */}
      {renderTabContent()}

      {/* Enhanced Dialog System */}
      <Dialog open={openDialog} onClose={() => setOpenDialog(false)} maxWidth="md" fullWidth>
        <DialogTitle>
          {dialogType === 'message' && 'Send SMS Message'}
          {dialogType === 'campaign' && 'Create Campaign'}
          {dialogType === 'template' && 'Create Template'}
          {dialogType === 'contact' && 'Add Contact'}
        </DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 2 }}>
            {dialogType === 'message' && (
              <Grid container spacing={2}>
                <Grid item xs={12} md={6}>
                  <FormControl fullWidth>
                    <InputLabel>Message Type</InputLabel>
                    <Select
                      value={newMessage.messageType}
                      label="Message Type"
                      onChange={(e) => setNewMessage({ ...newMessage, messageType: e.target.value })}
                    >
                      <MenuItem value="transactional">Transactional</MenuItem>
                      <MenuItem value="promotional">Promotional</MenuItem>
                      <MenuItem value="otp">OTP</MenuItem>
                      <MenuItem value="notification">Notification</MenuItem>
                    </Select>
                  </FormControl>
                </Grid>
                <Grid item xs={12} md={6}>
                  <FormControl fullWidth>
                    <InputLabel>Sender ID</InputLabel>
                    <Select
                      value={newMessage.senderId}
                      label="Sender ID"
                      onChange={(e) => setNewMessage({ ...newMessage, senderId: e.target.value })}
                    >
                      <MenuItem value="NEXORA">NEXORA</MenuItem>
                      <MenuItem value="NEXORAPAY">NEXORAPAY</MenuItem>
                      <MenuItem value="NEXORALOAN">NEXORALOAN</MenuItem>
                    </Select>
                  </FormControl>
                </Grid>
                <Grid item xs={12}>
                  <TextField
                    label="Recipient Phone Number"
                    value={newMessage.recipient}
                    onChange={(e) => setNewMessage({ ...newMessage, recipient: e.target.value })}
                    fullWidth
                    placeholder="+966501234567"
                    helperText="Enter Saudi Arabian phone number with country code"
                  />
                </Grid>
                <Grid item xs={12}>
                  <TextField
                    label="Message Content"
                    value={newMessage.message}
                    onChange={(e) => setNewMessage({ ...newMessage, message: e.target.value })}
                    fullWidth
                    multiline
                    rows={4}
                    placeholder="Enter your message here..."
                    helperText={`${newMessage.message.length}/160 characters • ${Math.ceil(newMessage.message.length / 160)} segment(s)`}
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    label="Scheduled Time (Optional)"
                    type="datetime-local"
                    value={newMessage.scheduledTime}
                    onChange={(e) => setNewMessage({ ...newMessage, scheduledTime: e.target.value })}
                    fullWidth
                    InputLabelProps={{ shrink: true }}
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <FormControl fullWidth>
                    <InputLabel>Template (Optional)</InputLabel>
                    <Select
                      value={newMessage.templateId}
                      label="Template (Optional)"
                      onChange={(e) => setNewMessage({ ...newMessage, templateId: e.target.value })}
                    >
                      <MenuItem value="">None</MenuItem>
                      {templates.filter(t => t.status === 'approved').map(template => (
                        <MenuItem key={template.id} value={template.id}>
                          {template.name}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                </Grid>
              </Grid>
            )}

            {dialogType === 'campaign' && (
              <Grid container spacing={2}>
                <Grid item xs={12}>
                  <TextField
                    label="Campaign Name"
                    value={newCampaign.name}
                    onChange={(e) => setNewCampaign({ ...newCampaign, name: e.target.value })}
                    fullWidth
                    required
                  />
                </Grid>
                <Grid item xs={12}>
                  <TextField
                    label="Description"
                    value={newCampaign.description}
                    onChange={(e) => setNewCampaign({ ...newCampaign, description: e.target.value })}
                    fullWidth
                    multiline
                    rows={2}
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <FormControl fullWidth>
                    <InputLabel>Target Audience</InputLabel>
                    <Select
                      value={newCampaign.targetAudience}
                      label="Target Audience"
                      onChange={(e) => setNewCampaign({ ...newCampaign, targetAudience: e.target.value })}
                    >
                      <MenuItem value="All Users">All Users</MenuItem>
                      <MenuItem value="Premium Users">Premium Users</MenuItem>
                      <MenuItem value="New Users">New Users</MenuItem>
                      <MenuItem value="Active Users">Active Users</MenuItem>
                    </Select>
                  </FormControl>
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    label="Scheduled Time"
                    type="datetime-local"
                    value={newCampaign.scheduledTime}
                    onChange={(e) => setNewCampaign({ ...newCampaign, scheduledTime: e.target.value })}
                    fullWidth
                    InputLabelProps={{ shrink: true }}
                  />
                </Grid>
              </Grid>
            )}

            {dialogType === 'template' && (
              <Grid container spacing={2}>
                <Grid item xs={12} md={6}>
                  <TextField
                    label="Template Name"
                    value={newTemplate.name}
                    onChange={(e) => setNewTemplate({ ...newTemplate, name: e.target.value })}
                    fullWidth
                    required
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <FormControl fullWidth>
                    <InputLabel>Template Type</InputLabel>
                    <Select
                      value={newTemplate.type}
                      label="Template Type"
                      onChange={(e) => setNewTemplate({ ...newTemplate, type: e.target.value })}
                    >
                      <MenuItem value="transactional">Transactional</MenuItem>
                      <MenuItem value="promotional">Promotional</MenuItem>
                      <MenuItem value="otp">OTP</MenuItem>
                      <MenuItem value="notification">Notification</MenuItem>
                    </Select>
                  </FormControl>
                </Grid>
                <Grid item xs={12}>
                  <TextField
                    label="Template Content"
                    value={newTemplate.content}
                    onChange={(e) => setNewTemplate({ ...newTemplate, content: e.target.value })}
                    fullWidth
                    multiline
                    rows={4}
                    placeholder="Use {{variable}} for dynamic content"
                    helperText="Templates require approval before use"
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <FormControl fullWidth>
                    <InputLabel>Language</InputLabel>
                    <Select
                      value={newTemplate.language}
                      label="Language"
                      onChange={(e) => setNewTemplate({ ...newTemplate, language: e.target.value })}
                    >
                      <MenuItem value="en">English</MenuItem>
                      <MenuItem value="ar">Arabic</MenuItem>
                    </Select>
                  </FormControl>
                </Grid>
              </Grid>
            )}

            {dialogType === 'contact' && (
              <Grid container spacing={2}>
                <Grid item xs={12}>
                  <TextField
                    label="Phone Number"
                    value={newContact.phoneNumber}
                    onChange={(e) => setNewContact({ ...newContact, phoneNumber: e.target.value })}
                    fullWidth
                    required
                    placeholder="+966501234567"
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    label="First Name"
                    value={newContact.firstName}
                    onChange={(e) => setNewContact({ ...newContact, firstName: e.target.value })}
                    fullWidth
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    label="Last Name"
                    value={newContact.lastName}
                    onChange={(e) => setNewContact({ ...newContact, lastName: e.target.value })}
                    fullWidth
                  />
                </Grid>
                <Grid item xs={12}>
                  <TextField
                    label="Email"
                    type="email"
                    value={newContact.email}
                    onChange={(e) => setNewContact({ ...newContact, email: e.target.value })}
                    fullWidth
                  />
                </Grid>
              </Grid>
            )}
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenDialog(false)}>Cancel</Button>
          <Button 
            onClick={
              dialogType === 'message' ? handleSendMessage :
              dialogType === 'campaign' ? handleCreateCampaign :
              dialogType === 'template' ? handleCreateTemplate :
              handleAddContact
            } 
            variant="contained"
          >
            {dialogType === 'message' && 'Send Message'}
            {dialogType === 'campaign' && 'Create Campaign'}
            {dialogType === 'template' && 'Create Template'}
            {dialogType === 'contact' && 'Add Contact'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Settings Drawer */}
      <Drawer
        anchor="right"
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        sx={{ '& .MuiDrawer-paper': { width: 400, p: 3 } }}
      >
        <Typography variant="h6" gutterBottom>
          Gateway Settings
        </Typography>
        <Divider sx={{ mb: 3 }} />
        
        <Typography variant="subtitle1" gutterBottom>
          Default Configuration
        </Typography>
        <FormControl fullWidth sx={{ mb: 2 }}>
          <InputLabel>Default Sender ID</InputLabel>
          <Select value="NEXORA" label="Default Sender ID">
            <MenuItem value="NEXORA">NEXORA</MenuItem>
            <MenuItem value="NEXORAPAY">NEXORAPAY</MenuItem>
            <MenuItem value="NEXORALOAN">NEXORALOAN</MenuItem>
          </Select>
        </FormControl>
        
        <FormControl fullWidth sx={{ mb: 2 }}>
          <InputLabel>Default Provider</InputLabel>
          <Select value="AUTO" label="Default Provider">
            <MenuItem value="AUTO">Auto-Select</MenuItem>
            <MenuItem value="STC">STC</MenuItem>
            <MenuItem value="Mobily">Mobily</MenuItem>
            <MenuItem value="Zain">Zain</MenuItem>
          </Select>
        </FormControl>

        <Typography variant="subtitle1" gutterBottom sx={{ mt: 3 }}>
          Compliance Settings
        </Typography>
        <FormControlLabel
          control={<Switch defaultChecked />}
          label="Enable DND Checking"
        />
        <FormControlLabel
          control={<Switch defaultChecked />}
          label="Time Window Validation"
        />
        <FormControlLabel
          control={<Switch defaultChecked />}
          label="Content Filtering"
        />
        <FormControlLabel
          control={<Switch defaultChecked />}
          label="Real-time Alerts"
        />

        <Typography variant="subtitle1" gutterBottom sx={{ mt: 3 }}>
          Rate Limiting
        </Typography>
        <TextField
          fullWidth
          label="Messages per minute"
          type="number"
          defaultValue="100"
          sx={{ mb: 2 }}
        />
        <TextField
          fullWidth
          label="Messages per hour"
          type="number"
          defaultValue="5000"
          sx={{ mb: 2 }}
        />
      </Drawer>
    </Container>
  );
};

export default SmsGateway;
