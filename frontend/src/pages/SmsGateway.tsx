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
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
} from '@mui/material';
import {
  Sms,
  Send,
  CheckCircle,
  Error,
  Pending,
  TrendingUp,
} from '@mui/icons-material';

interface SmsMessage {
  id: string;
  recipient: string;
  message: string;
  status: 'sent' | 'pending' | 'failed';
  timestamp: string;
  cost: number;
}

const SmsGateway: React.FC = () => {
  const [openDialog, setOpenDialog] = useState(false);
  const [newMessage, setNewMessage] = useState({
    recipient: '',
    message: '',
    type: 'single',
  });

  const messages: SmsMessage[] = [
    {
      id: 'SMS-001',
      recipient: '+1234567890',
      message: 'Your verification code is 123456',
      status: 'sent',
      timestamp: '2024-06-18 10:30:00',
      cost: 0.05,
    },
    {
      id: 'SMS-002',
      recipient: '+1234567891',
      message: 'Payment confirmation: $1,250.00 received',
      status: 'sent',
      timestamp: '2024-06-18 09:15:00',
      cost: 0.05,
    },
    {
      id: 'SMS-003',
      recipient: '+1234567892',
      message: 'Welcome to Nexora platform!',
      status: 'pending',
      timestamp: '2024-06-18 08:45:00',
      cost: 0.05,
    },
  ];

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
    setNewMessage({ recipient: '', message: '', type: 'single' });
  };

  const totalMessages = messages.length;
  const sentMessages = messages.filter(msg => msg.status === 'sent').length;
  const totalCost = messages.reduce((sum, msg) => sum + msg.cost, 0);
  const deliveryRate = Math.round((sentMessages / totalMessages) * 100);

  return (
    <Container maxWidth="xl">
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" component="h1" gutterBottom>
          SMS Gateway
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Send SMS messages and manage your messaging campaigns.
        </Typography>
      </Box>

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
                    Delivered
                  </Typography>
                  <Typography variant="h4">
                    {sentMessages}
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
                    Total Cost
                  </Typography>
                  <Typography variant="h4">
                    ${totalCost.toFixed(2)}
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
                    Delivery Rate
                  </Typography>
                  <Typography variant="h4">
                    {deliveryRate}%
                  </Typography>
                </Box>
                <CheckCircle color="success" sx={{ fontSize: 40 }} />
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
                Recent Messages
              </Typography>
              <Button
                variant="contained"
                startIcon={<Send />}
                onClick={() => setOpenDialog(true)}
              >
                Send SMS
              </Button>
            </Box>
            
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Message ID</TableCell>
                    <TableCell>Recipient</TableCell>
                    <TableCell>Message</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Timestamp</TableCell>
                    <TableCell>Cost</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {messages.map((message) => (
                    <TableRow key={message.id}>
                      <TableCell>{message.id}</TableCell>
                      <TableCell>{message.recipient}</TableCell>
                      <TableCell>
                        {message.message.length > 50 
                          ? `${message.message.substring(0, 50)}...` 
                          : message.message}
                      </TableCell>
                      <TableCell>
                        <Chip
                          icon={getStatusIcon(message.status)}
                          label={message.status.toUpperCase()}
                          color={getStatusColor(message.status) as any}
                          size="small"
                        />
                      </TableCell>
                      <TableCell>{new Date(message.timestamp).toLocaleString()}</TableCell>
                      <TableCell>${message.cost.toFixed(2)}</TableCell>
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
              Message Templates
            </Typography>
            <Box display="flex" flexDirection="column" gap={2}>
              <Button variant="outlined" fullWidth>
                Verification Code
              </Button>
              <Button variant="outlined" fullWidth>
                Payment Confirmation
              </Button>
              <Button variant="outlined" fullWidth>
                Welcome Message
              </Button>
              <Button variant="outlined" fullWidth>
                Password Reset
              </Button>
            </Box>
          </Paper>
          
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              SMS Credits
            </Typography>
            <Box>
              <Typography variant="h4" color="primary" gutterBottom>
                1,250
              </Typography>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                Credits remaining
              </Typography>
              <Button variant="contained" fullWidth sx={{ mt: 2 }}>
                Buy More Credits
              </Button>
            </Box>
          </Paper>
        </Grid>
      </Grid>

      <Dialog open={openDialog} onClose={() => setOpenDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Send SMS Message</DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 2 }}>
            <Grid container spacing={2}>
              <Grid item xs={12}>
                <FormControl fullWidth>
                  <InputLabel>Message Type</InputLabel>
                  <Select
                    value={newMessage.type}
                    label="Message Type"
                    onChange={(e) => setNewMessage({ ...newMessage, type: e.target.value })}
                  >
                    <MenuItem value="single">Single Message</MenuItem>
                    <MenuItem value="bulk">Bulk Message</MenuItem>
                    <MenuItem value="template">Template Message</MenuItem>
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={12}>
                <TextField
                  label="Recipient Phone Number"
                  value={newMessage.recipient}
                  onChange={(e) => setNewMessage({ ...newMessage, recipient: e.target.value })}
                  fullWidth
                  placeholder="+1234567890"
                />
              </Grid>
              <Grid item xs={12}>
                <TextField
                  label="Message"
                  value={newMessage.message}
                  onChange={(e) => setNewMessage({ ...newMessage, message: e.target.value })}
                  fullWidth
                  multiline
                  rows={4}
                  placeholder="Enter your message here..."
                  helperText={`${newMessage.message.length}/160 characters`}
                />
              </Grid>
            </Grid>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenDialog(false)}>Cancel</Button>
          <Button onClick={handleSendMessage} variant="contained">
            Send Message
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
};

export default SmsGateway;
