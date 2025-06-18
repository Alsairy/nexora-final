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
  LinearProgress,
} from '@mui/material';
import {
  Draw,
  Add,
  Send,
  CheckCircle,
  Error,
  Pending,
  Visibility,
  GetApp,
  Person,
} from '@mui/icons-material';

interface Document {
  id: string;
  name: string;
  status: 'draft' | 'sent' | 'signed' | 'completed' | 'expired';
  signers: number;
  signedBy: number;
  createdDate: string;
  expiryDate: string;
  progress: number;
}

const ESignature: React.FC = () => {
  const [openDialog, setOpenDialog] = useState(false);
  const [newDocument, setNewDocument] = useState({
    name: '',
    signers: '',
    expiryDays: '30',
    message: '',
  });

  const documents: Document[] = [
    {
      id: 'DOC-001',
      name: 'Employment Contract - John Doe',
      status: 'completed',
      signers: 2,
      signedBy: 2,
      createdDate: '2024-06-15',
      expiryDate: '2024-07-15',
      progress: 100,
    },
    {
      id: 'DOC-002',
      name: 'NDA Agreement - Tech Corp',
      status: 'sent',
      signers: 3,
      signedBy: 1,
      createdDate: '2024-06-16',
      expiryDate: '2024-07-16',
      progress: 33,
    },
    {
      id: 'DOC-003',
      name: 'Service Agreement - Client ABC',
      status: 'draft',
      signers: 2,
      signedBy: 0,
      createdDate: '2024-06-17',
      expiryDate: '2024-07-17',
      progress: 0,
    },
    {
      id: 'DOC-004',
      name: 'Partnership Agreement',
      status: 'signed',
      signers: 4,
      signedBy: 3,
      createdDate: '2024-06-14',
      expiryDate: '2024-07-14',
      progress: 75,
    },
  ];

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'completed':
        return <CheckCircle color="success" />;
      case 'signed':
        return <CheckCircle color="info" />;
      case 'sent':
        return <Send color="primary" />;
      case 'draft':
        return <Pending color="warning" />;
      case 'expired':
        return <Error color="error" />;
      default:
        return <Pending />;
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'completed':
        return 'success';
      case 'signed':
        return 'info';
      case 'sent':
        return 'primary';
      case 'draft':
        return 'warning';
      case 'expired':
        return 'error';
      default:
        return 'default';
    }
  };

  const handleCreateDocument = () => {
    console.log('Creating document:', newDocument);
    setOpenDialog(false);
    setNewDocument({ name: '', signers: '', expiryDays: '30', message: '' });
  };

  const totalDocuments = documents.length;
  const completedDocuments = documents.filter(doc => doc.status === 'completed').length;
  const pendingDocuments = documents.filter(doc => doc.status === 'sent').length;
  const completionRate = Math.round((completedDocuments / totalDocuments) * 100);

  return (
    <Container maxWidth="xl">
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" component="h1" gutterBottom>
          E-Signature
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Create, send, and manage digital signature documents.
        </Typography>
      </Box>

      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography color="textSecondary" gutterBottom variant="h6">
                    Total Documents
                  </Typography>
                  <Typography variant="h4">
                    {totalDocuments}
                  </Typography>
                </Box>
                <Draw color="primary" sx={{ fontSize: 40 }} />
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
                    {completedDocuments}
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
                    {pendingDocuments}
                  </Typography>
                </Box>
                <Send color="primary" sx={{ fontSize: 40 }} />
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
                    Completion Rate
                  </Typography>
                  <Typography variant="h4">
                    {completionRate}%
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
                Documents
              </Typography>
              <Button
                variant="contained"
                startIcon={<Add />}
                onClick={() => setOpenDialog(true)}
              >
                New Document
              </Button>
            </Box>
            
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Document Name</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Progress</TableCell>
                    <TableCell>Signers</TableCell>
                    <TableCell>Created</TableCell>
                    <TableCell>Expires</TableCell>
                    <TableCell>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {documents.map((document) => (
                    <TableRow key={document.id}>
                      <TableCell>{document.name}</TableCell>
                      <TableCell>
                        <Chip
                          icon={getStatusIcon(document.status)}
                          label={document.status.toUpperCase()}
                          color={getStatusColor(document.status) as any}
                          size="small"
                        />
                      </TableCell>
                      <TableCell>
                        <Box display="flex" alignItems="center" gap={1}>
                          <LinearProgress
                            variant="determinate"
                            value={document.progress}
                            sx={{ width: 60, height: 6 }}
                          />
                          <Typography variant="body2">
                            {document.progress}%
                          </Typography>
                        </Box>
                      </TableCell>
                      <TableCell>
                        {document.signedBy}/{document.signers}
                      </TableCell>
                      <TableCell>{new Date(document.createdDate).toLocaleDateString()}</TableCell>
                      <TableCell>{new Date(document.expiryDate).toLocaleDateString()}</TableCell>
                      <TableCell>
                        <Button size="small" startIcon={<Visibility />}>
                          View
                        </Button>
                        <Button size="small" startIcon={<GetApp />}>
                          Download
                        </Button>
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
              Document Templates
            </Typography>
            <Box display="flex" flexDirection="column" gap={2}>
              <Button variant="outlined" fullWidth>
                Employment Contract
              </Button>
              <Button variant="outlined" fullWidth>
                NDA Agreement
              </Button>
              <Button variant="outlined" fullWidth>
                Service Agreement
              </Button>
              <Button variant="outlined" fullWidth>
                Partnership Agreement
              </Button>
            </Box>
          </Paper>
          
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              Recent Activity
            </Typography>
            <Box display="flex" flexDirection="column" gap={2}>
              <Box display="flex" alignItems="center" gap={2}>
                <Person fontSize="small" />
                <Box>
                  <Typography variant="body2">
                    John Doe signed Employment Contract
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    2 hours ago
                  </Typography>
                </Box>
              </Box>
              <Box display="flex" alignItems="center" gap={2}>
                <Send fontSize="small" />
                <Box>
                  <Typography variant="body2">
                    NDA sent to Tech Corp
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    1 day ago
                  </Typography>
                </Box>
              </Box>
              <Box display="flex" alignItems="center" gap={2}>
                <CheckCircle fontSize="small" />
                <Box>
                  <Typography variant="body2">
                    Service Agreement completed
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    3 days ago
                  </Typography>
                </Box>
              </Box>
            </Box>
          </Paper>
        </Grid>
      </Grid>

      <Dialog open={openDialog} onClose={() => setOpenDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Create New Document</DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 2 }}>
            <Grid container spacing={2}>
              <Grid item xs={12}>
                <TextField
                  label="Document Name"
                  value={newDocument.name}
                  onChange={(e) => setNewDocument({ ...newDocument, name: e.target.value })}
                  fullWidth
                  placeholder="Employment Contract - John Doe"
                />
              </Grid>
              <Grid item xs={12}>
                <TextField
                  label="Signer Email Addresses"
                  value={newDocument.signers}
                  onChange={(e) => setNewDocument({ ...newDocument, signers: e.target.value })}
                  fullWidth
                  multiline
                  rows={3}
                  placeholder="john@example.com, jane@example.com"
                  helperText="Enter email addresses separated by commas"
                />
              </Grid>
              <Grid item xs={6}>
                <FormControl fullWidth>
                  <InputLabel>Expires In</InputLabel>
                  <Select
                    value={newDocument.expiryDays}
                    label="Expires In"
                    onChange={(e) => setNewDocument({ ...newDocument, expiryDays: e.target.value })}
                  >
                    <MenuItem value="7">7 days</MenuItem>
                    <MenuItem value="14">14 days</MenuItem>
                    <MenuItem value="30">30 days</MenuItem>
                    <MenuItem value="60">60 days</MenuItem>
                    <MenuItem value="90">90 days</MenuItem>
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={12}>
                <TextField
                  label="Message to Signers"
                  value={newDocument.message}
                  onChange={(e) => setNewDocument({ ...newDocument, message: e.target.value })}
                  fullWidth
                  multiline
                  rows={3}
                  placeholder="Please review and sign this document..."
                />
              </Grid>
            </Grid>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenDialog(false)}>Cancel</Button>
          <Button onClick={handleCreateDocument} variant="contained">
            Create & Send
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
};

export default ESignature;
