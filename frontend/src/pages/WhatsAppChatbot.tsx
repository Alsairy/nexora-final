import React, { useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  Container,
  Grid,
  Typography,
  Button,
  Paper,
  List,
  ListItem,
  ListItemText,
  ListItemAvatar,
  Avatar,
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
} from '@mui/material';
import {
  WhatsApp,
  Add,
  SmartToy,
  TrendingUp,
  Message,
  Person,
  Settings,
  PlayArrow,
  Pause,
} from '@mui/icons-material';

interface ChatSession {
  id: string;
  customerName: string;
  customerPhone: string;
  status: 'active' | 'resolved' | 'pending';
  lastMessage: string;
  timestamp: string;
  messageCount: number;
}

interface Automation {
  id: string;
  name: string;
  trigger: string;
  response: string;
  isActive: boolean;
  usageCount: number;
}

const WhatsAppChatbot: React.FC = () => {
  const [openDialog, setOpenDialog] = useState(false);
  const [dialogType, setDialogType] = useState<'automation' | 'broadcast'>('automation');
  const [newAutomation, setNewAutomation] = useState({
    name: '',
    trigger: '',
    response: '',
    isActive: true,
  });

  const chatSessions: ChatSession[] = [
    {
      id: 'CHAT-001',
      customerName: 'John Doe',
      customerPhone: '+1234567890',
      status: 'active',
      lastMessage: 'I need help with my payment',
      timestamp: '2024-06-18 10:30:00',
      messageCount: 5,
    },
    {
      id: 'CHAT-002',
      customerName: 'Jane Smith',
      customerPhone: '+1234567891',
      status: 'resolved',
      lastMessage: 'Thank you for your help!',
      timestamp: '2024-06-18 09:15:00',
      messageCount: 12,
    },
    {
      id: 'CHAT-003',
      customerName: 'Mike Johnson',
      customerPhone: '+1234567892',
      status: 'pending',
      lastMessage: 'What are your business hours?',
      timestamp: '2024-06-18 08:45:00',
      messageCount: 3,
    },
  ];

  const automations: Automation[] = [
    {
      id: 'AUTO-001',
      name: 'Welcome Message',
      trigger: 'hello',
      response: 'Welcome to Nexora! How can I help you today?',
      isActive: true,
      usageCount: 245,
    },
    {
      id: 'AUTO-002',
      name: 'Business Hours',
      trigger: 'hours',
      response: 'Our business hours are Monday-Friday 9AM-6PM EST.',
      isActive: true,
      usageCount: 89,
    },
    {
      id: 'AUTO-003',
      name: 'Payment Support',
      trigger: 'payment',
      response: 'For payment issues, please provide your transaction ID.',
      isActive: true,
      usageCount: 156,
    },
  ];

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'active':
        return 'success';
      case 'resolved':
        return 'info';
      case 'pending':
        return 'warning';
      default:
        return 'default';
    }
  };

  const handleCreateAutomation = () => {
    console.log('Creating automation:', newAutomation);
    setOpenDialog(false);
    setNewAutomation({ name: '', trigger: '', response: '', isActive: true });
  };

  const totalChats = chatSessions.length;
  const activeChats = chatSessions.filter(chat => chat.status === 'active').length;
  const totalAutomations = automations.length;
  const activeAutomations = automations.filter(auto => auto.isActive).length;

  return (
    <Container maxWidth="xl">
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" component="h1" gutterBottom>
          WhatsApp Chatbot
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Manage WhatsApp conversations and automated responses.
        </Typography>
      </Box>

      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography color="textSecondary" gutterBottom variant="h6">
                    Total Chats
                  </Typography>
                  <Typography variant="h4">
                    {totalChats}
                  </Typography>
                </Box>
                <WhatsApp color="success" sx={{ fontSize: 40 }} />
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
                    Active Chats
                  </Typography>
                  <Typography variant="h4">
                    {activeChats}
                  </Typography>
                </Box>
                <Message color="primary" sx={{ fontSize: 40 }} />
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
                    Automations
                  </Typography>
                  <Typography variant="h4">
                    {activeAutomations}/{totalAutomations}
                  </Typography>
                </Box>
                <SmartToy color="info" sx={{ fontSize: 40 }} />
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
                    Response Rate
                  </Typography>
                  <Typography variant="h4">
                    98%
                  </Typography>
                </Box>
                <TrendingUp color="success" sx={{ fontSize: 40 }} />
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Grid container spacing={3}>
        <Grid item xs={12} md={6}>
          <Paper sx={{ p: 3 }}>
            <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 3 }}>
              <Typography variant="h6">
                Recent Conversations
              </Typography>
              <Button variant="outlined" startIcon={<Message />}>
                View All
              </Button>
            </Box>
            
            <List>
              {chatSessions.map((session) => (
                <ListItem key={session.id} divider>
                  <ListItemAvatar>
                    <Avatar>
                      <Person />
                    </Avatar>
                  </ListItemAvatar>
                  <ListItemText
                    primary={
                      <Box display="flex" alignItems="center" justifyContent="space-between">
                        <Typography variant="subtitle1">
                          {session.customerName}
                        </Typography>
                        <Chip
                          label={session.status.toUpperCase()}
                          color={getStatusColor(session.status) as any}
                          size="small"
                        />
                      </Box>
                    }
                    secondary={
                      <Box>
                        <Typography variant="body2" color="text.secondary">
                          {session.lastMessage}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {new Date(session.timestamp).toLocaleString()} • {session.messageCount} messages
                        </Typography>
                      </Box>
                    }
                  />
                </ListItem>
              ))}
            </List>
          </Paper>
        </Grid>
        
        <Grid item xs={12} md={6}>
          <Paper sx={{ p: 3 }}>
            <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 3 }}>
              <Typography variant="h6">
                Automated Responses
              </Typography>
              <Button
                variant="contained"
                startIcon={<Add />}
                onClick={() => {
                  setDialogType('automation');
                  setOpenDialog(true);
                }}
              >
                Add Automation
              </Button>
            </Box>
            
            <List>
              {automations.map((automation) => (
                <ListItem key={automation.id} divider>
                  <ListItemAvatar>
                    <Avatar sx={{ bgcolor: automation.isActive ? 'success.main' : 'grey.500' }}>
                      <SmartToy />
                    </Avatar>
                  </ListItemAvatar>
                  <ListItemText
                    primary={
                      <Box display="flex" alignItems="center" justifyContent="space-between">
                        <Typography variant="subtitle1">
                          {automation.name}
                        </Typography>
                        <Box display="flex" alignItems="center" gap={1}>
                          <Typography variant="caption" color="text.secondary">
                            Used {automation.usageCount} times
                          </Typography>
                          {automation.isActive ? <PlayArrow color="success" /> : <Pause color="disabled" />}
                        </Box>
                      </Box>
                    }
                    secondary={
                      <Box>
                        <Typography variant="body2" color="text.secondary">
                          Trigger: "{automation.trigger}"
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          {automation.response.length > 50 
                            ? `${automation.response.substring(0, 50)}...` 
                            : automation.response}
                        </Typography>
                      </Box>
                    }
                  />
                </ListItem>
              ))}
            </List>
          </Paper>
        </Grid>
      </Grid>

      <Grid container spacing={3} sx={{ mt: 2 }}>
        <Grid item xs={12} md={4}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              Quick Actions
            </Typography>
            <Box display="flex" flexDirection="column" gap={2}>
              <Button
                variant="outlined"
                fullWidth
                startIcon={<Message />}
                onClick={() => {
                  setDialogType('broadcast');
                  setOpenDialog(true);
                }}
              >
                Send Broadcast
              </Button>
              <Button variant="outlined" fullWidth startIcon={<Settings />}>
                Bot Settings
              </Button>
              <Button variant="outlined" fullWidth startIcon={<TrendingUp />}>
                View Analytics
              </Button>
            </Box>
          </Paper>
        </Grid>
        
        <Grid item xs={12} md={8}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              Performance Metrics
            </Typography>
            <Grid container spacing={2}>
              <Grid item xs={6} md={3}>
                <Box textAlign="center">
                  <Typography variant="h4" color="primary">
                    1,234
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Messages Sent
                  </Typography>
                </Box>
              </Grid>
              <Grid item xs={6} md={3}>
                <Box textAlign="center">
                  <Typography variant="h4" color="success.main">
                    98%
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Delivery Rate
                  </Typography>
                </Box>
              </Grid>
              <Grid item xs={6} md={3}>
                <Box textAlign="center">
                  <Typography variant="h4" color="info.main">
                    2.5min
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Avg Response Time
                  </Typography>
                </Box>
              </Grid>
              <Grid item xs={6} md={3}>
                <Box textAlign="center">
                  <Typography variant="h4" color="warning.main">
                    85%
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Resolution Rate
                  </Typography>
                </Box>
              </Grid>
            </Grid>
          </Paper>
        </Grid>
      </Grid>

      <Dialog open={openDialog} onClose={() => setOpenDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>
          {dialogType === 'automation' ? 'Create Automation' : 'Send Broadcast Message'}
        </DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 2 }}>
            {dialogType === 'automation' ? (
              <Grid container spacing={2}>
                <Grid item xs={12}>
                  <TextField
                    label="Automation Name"
                    value={newAutomation.name}
                    onChange={(e) => setNewAutomation({ ...newAutomation, name: e.target.value })}
                    fullWidth
                    placeholder="Welcome Message"
                  />
                </Grid>
                <Grid item xs={12}>
                  <TextField
                    label="Trigger Keywords"
                    value={newAutomation.trigger}
                    onChange={(e) => setNewAutomation({ ...newAutomation, trigger: e.target.value })}
                    fullWidth
                    placeholder="hello, hi, start"
                    helperText="Keywords that will trigger this response (comma separated)"
                  />
                </Grid>
                <Grid item xs={12}>
                  <TextField
                    label="Response Message"
                    value={newAutomation.response}
                    onChange={(e) => setNewAutomation({ ...newAutomation, response: e.target.value })}
                    fullWidth
                    multiline
                    rows={4}
                    placeholder="Welcome to Nexora! How can I help you today?"
                  />
                </Grid>
                <Grid item xs={12}>
                  <FormControlLabel
                    control={
                      <Switch
                        checked={newAutomation.isActive}
                        onChange={(e) => setNewAutomation({ ...newAutomation, isActive: e.target.checked })}
                      />
                    }
                    label="Activate immediately"
                  />
                </Grid>
              </Grid>
            ) : (
              <Grid container spacing={2}>
                <Grid item xs={12}>
                  <TextField
                    label="Broadcast Message"
                    fullWidth
                    multiline
                    rows={4}
                    placeholder="Enter your broadcast message here..."
                  />
                </Grid>
                <Grid item xs={12}>
                  <FormControl fullWidth>
                    <InputLabel>Target Audience</InputLabel>
                    <Select defaultValue="all">
                      <MenuItem value="all">All Customers</MenuItem>
                      <MenuItem value="active">Active Customers</MenuItem>
                      <MenuItem value="recent">Recent Customers</MenuItem>
                    </Select>
                  </FormControl>
                </Grid>
              </Grid>
            )}
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenDialog(false)}>Cancel</Button>
          <Button
            onClick={handleCreateAutomation}
            variant="contained"
          >
            {dialogType === 'automation' ? 'Create Automation' : 'Send Broadcast'}
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
};

export default WhatsAppChatbot;
