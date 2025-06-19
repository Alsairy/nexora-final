import {
  TrendingUp,
  Payment,
  People,
  AccountBalance,
  Notifications,
  CheckCircle,
  Warning,
  Error,
} from '@mui/icons-material';
import {
  Box,
  Card,
  CardContent,
  Container,
  Grid,
  Typography,
  Paper,
  Avatar,
  List,
  ListItem,
  ListItemText,
  ListItemAvatar,
  Chip,
} from '@mui/material';
import React from 'react';

import { useAuth } from '../contexts/AuthContext';

interface StatCardProps {
  title: string;
  value: string;
  icon: React.ReactElement;
  color: string;
  trend?: string;
}

const StatCard: React.FC<StatCardProps> = ({ title, value, icon, color, trend }) => (
  <Card>
    <CardContent>
      <Box display="flex" alignItems="center" justifyContent="space-between">
        <Box>
          <Typography color="textSecondary" gutterBottom variant="h6">
            {title}
          </Typography>
          <Typography variant="h4" component="h2">
            {value}
          </Typography>
          {trend && (
            <Typography color="textSecondary" variant="body2">
              {trend}
            </Typography>
          )}
        </Box>
        <Avatar sx={{ bgcolor: color, width: 56, height: 56 }}>{icon}</Avatar>
      </Box>
    </CardContent>
  </Card>
);

const Dashboard: React.FC = () => {
  const { user } = useAuth();

  const recentActivities = [
    {
      id: 1,
      title: 'Payment processed',
      description: 'Payment of $1,250.00 processed successfully',
      time: '2 minutes ago',
      status: 'success',
      icon: <CheckCircle color="success" />,
    },
    {
      id: 2,
      title: 'New user registered',
      description: 'John Doe joined your platform',
      time: '15 minutes ago',
      status: 'info',
      icon: <People color="info" />,
    },
    {
      id: 3,
      title: 'API rate limit warning',
      description: 'Approaching rate limit for SMS Gateway',
      time: '1 hour ago',
      status: 'warning',
      icon: <Warning color="warning" />,
    },
    {
      id: 4,
      title: 'Failed transaction',
      description: 'Transaction #TX-2024-001 failed due to insufficient funds',
      time: '2 hours ago',
      status: 'error',
      icon: <Error color="error" />,
    },
  ];

  return (
    <Container maxWidth="xl">
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" component="h1" gutterBottom>
          Welcome back, {user?.firstName}!
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Here's what's happening with your Nexora platform today.
        </Typography>
      </Box>

      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            title="Total Revenue"
            value="$24,567"
            icon={<TrendingUp />}
            color="#4caf50"
            trend="+12% from last month"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            title="Transactions"
            value="1,234"
            icon={<Payment />}
            color="#2196f3"
            trend="+8% from last month"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            title="Active Users"
            value="567"
            icon={<People />}
            color="#ff9800"
            trend="+15% from last month"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            title="Account Balance"
            value="$89,432"
            icon={<AccountBalance />}
            color="#9c27b0"
            trend="+5% from last month"
          />
        </Grid>
      </Grid>

      <Grid container spacing={3}>
        <Grid item xs={12} md={8}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              Recent Activity
            </Typography>
            <List>
              {recentActivities.map(activity => (
                <ListItem key={activity.id} divider>
                  <ListItemAvatar>
                    <Avatar sx={{ bgcolor: 'transparent' }}>{activity.icon}</Avatar>
                  </ListItemAvatar>
                  <ListItemText
                    primary={activity.title}
                    secondary={
                      <Box>
                        <Typography variant="body2" color="text.secondary">
                          {activity.description}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {activity.time}
                        </Typography>
                      </Box>
                    }
                  />
                  <Chip
                    label={activity.status}
                    color={
                      activity.status === 'success'
                        ? 'success'
                        : activity.status === 'warning'
                          ? 'warning'
                          : activity.status === 'error'
                            ? 'error'
                            : 'info'
                    }
                    size="small"
                  />
                </ListItem>
              ))}
            </List>
          </Paper>
        </Grid>

        <Grid item xs={12} md={4}>
          <Paper sx={{ p: 3, mb: 3 }}>
            <Typography variant="h6" gutterBottom>
              Quick Actions
            </Typography>
            <Box display="flex" flexDirection="column" gap={2}>
              <Chip label="Process Payment" clickable color="primary" icon={<Payment />} />
              <Chip label="Send SMS" clickable color="secondary" icon={<Notifications />} />
              <Chip label="View Reports" clickable color="info" icon={<TrendingUp />} />
            </Box>
          </Paper>

          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              System Status
            </Typography>
            <Box display="flex" flexDirection="column" gap={1}>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Typography variant="body2">Payment Gateway</Typography>
                <Chip label="Online" color="success" size="small" />
              </Box>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Typography variant="body2">SMS Gateway</Typography>
                <Chip label="Online" color="success" size="small" />
              </Box>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Typography variant="body2">API Gateway</Typography>
                <Chip label="Warning" color="warning" size="small" />
              </Box>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Typography variant="body2">E-Wallet</Typography>
                <Chip label="Online" color="success" size="small" />
              </Box>
            </Box>
          </Paper>
        </Grid>
      </Grid>
    </Container>
  );
};

export default Dashboard;
