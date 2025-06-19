import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Grid,
  Button,
  Tab,
  Tabs,
  TextField,
  MenuItem,
  FormControl,
  InputLabel,
  Select,
  Chip,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  CircularProgress,
  Alert,
  Tooltip,
  LinearProgress
} from '@mui/material';
import { DatePicker, LocalizationProvider } from '@mui/x-date-pickers';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import {
  Assessment as ReportsIcon,
  Download as DownloadIcon,
  PictureAsPdf as PdfIcon,
  TableChart as ExcelIcon,
  Description as CsvIcon,
  Schedule as ScheduleIcon,
  FilterList as FilterIcon,
  Refresh as RefreshIcon,
  TrendingUp as TrendingUpIcon,
  Payment as PaymentIcon,
  Sms as SmsIcon,
  AccountBalance as RevenueIcon,
  Security as ComplianceIcon,
  Settings as CustomIcon
} from '@mui/icons-material';
import { format, subDays, startOfMonth, endOfMonth } from 'date-fns';

interface ReportData {
  id: string;
  type: 'transaction' | 'sms-usage' | 'revenue' | 'compliance' | 'custom';
  name: string;
  generatedAt: string;
  status: 'generating' | 'completed' | 'failed';
  downloadUrl?: string;
  summary?: any;
}

interface ExportJob {
  id: string;
  reportId: string;
  format: 'pdf' | 'excel' | 'csv';
  status: 'queued' | 'processing' | 'completed' | 'failed' | 'cancelled';
  createdAt: string;
  downloadUrl?: string;
  progress?: number;
}

interface ReportFilters {
  startDate: Date;
  endDate: Date;
  reportType: string;
  status?: string[];
  paymentMethods?: string[];
  currencies?: string[];
}

const Reports: React.FC = () => {
  const [activeTab, setActiveTab] = useState(0);
  const [reports, setReports] = useState<ReportData[]>([]);
  const [exportJobs, setExportJobs] = useState<ExportJob[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [generateDialogOpen, setGenerateDialogOpen] = useState(false);
  const [exportDialogOpen, setExportDialogOpen] = useState(false);
  const [selectedReport, setSelectedReport] = useState<ReportData | null>(null);
  const [filters, setFilters] = useState<ReportFilters>({
    startDate: startOfMonth(new Date()),
    endDate: endOfMonth(new Date()),
    reportType: 'transaction',
    status: [],
    paymentMethods: [],
    currencies: ['SAR', 'USD']
  });

  useEffect(() => {
    loadReports();
    loadExportJobs();
  }, []);

  const loadReports = async () => {
    setLoading(true);
    try {
      const mockReports: ReportData[] = [
        {
          id: '1',
          type: 'transaction',
          name: 'Monthly Transaction Report',
          generatedAt: new Date().toISOString(),
          status: 'completed',
          summary: {
            totalTransactions: 1250,
            totalAmount: 125000,
            successRate: 98.5
          }
        },
        {
          id: '2',
          type: 'sms-usage',
          name: 'SMS Usage Analysis',
          generatedAt: new Date().toISOString(),
          status: 'completed',
          summary: {
            totalMessages: 5000,
            totalCost: 2500,
            deliveryRate: 97.2
          }
        },
        {
          id: '3',
          type: 'revenue',
          name: 'Revenue Analytics',
          generatedAt: new Date().toISOString(),
          status: 'generating'
        }
      ];
      setReports(mockReports);
    } catch (err) {
      setError('Failed to load reports');
    } finally {
      setLoading(false);
    }
  };

  const loadExportJobs = async () => {
    try {
      const mockExportJobs: ExportJob[] = [
        {
          id: '1',
          reportId: '1',
          format: 'pdf',
          status: 'completed',
          createdAt: new Date().toISOString(),
          downloadUrl: '/api/downloads/report-1.pdf'
        },
        {
          id: '2',
          reportId: '2',
          format: 'excel',
          status: 'processing',
          createdAt: new Date().toISOString(),
          progress: 65
        }
      ];
      setExportJobs(mockExportJobs);
    } catch (err) {
      console.error('Failed to load export jobs:', err);
    }
  };

  const generateReport = async (reportType: string) => {
    setLoading(true);
    try {
      const newReport: ReportData = {
        id: Date.now().toString(),
        type: reportType as any,
        name: `${reportType.charAt(0).toUpperCase() + reportType.slice(1)} Report`,
        generatedAt: new Date().toISOString(),
        status: 'generating'
      };

      setReports(prev => [newReport, ...prev]);
      setGenerateDialogOpen(false);

      setTimeout(() => {
        setReports(prev => prev.map(r => 
          r.id === newReport.id 
            ? { ...r, status: 'completed' as const, summary: getMockSummary(reportType) }
            : r
        ));
      }, 3000);

    } catch (err) {
      setError('Failed to generate report');
    } finally {
      setLoading(false);
    }
  };

  const exportReport = async (reportId: string, format: 'pdf' | 'excel' | 'csv') => {
    try {
      const newExportJob: ExportJob = {
        id: Date.now().toString(),
        reportId,
        format,
        status: 'processing',
        createdAt: new Date().toISOString(),
        progress: 0
      };

      setExportJobs(prev => [newExportJob, ...prev]);
      setExportDialogOpen(false);

      const progressInterval = setInterval(() => {
        setExportJobs(prev => prev.map(job => 
          job.id === newExportJob.id && job.progress !== undefined
            ? { ...job, progress: Math.min((job.progress || 0) + 20, 100) }
            : job
        ));
      }, 500);

      setTimeout(() => {
        clearInterval(progressInterval);
        setExportJobs(prev => prev.map(job => 
          job.id === newExportJob.id 
            ? { 
                ...job, 
                status: 'completed' as const, 
                downloadUrl: `/api/downloads/report-${reportId}.${format}`,
                progress: 100
              }
            : job
        ));
      }, 3000);

    } catch (err) {
      setError('Failed to export report');
    }
  };

  const getMockSummary = (reportType: string) => {
    switch (reportType) {
      case 'transaction':
        return {
          totalTransactions: Math.floor(Math.random() * 2000) + 500,
          totalAmount: Math.floor(Math.random() * 200000) + 50000,
          successRate: 95 + Math.random() * 5
        };
      case 'sms-usage':
        return {
          totalMessages: Math.floor(Math.random() * 10000) + 1000,
          totalCost: Math.floor(Math.random() * 5000) + 1000,
          deliveryRate: 95 + Math.random() * 5
        };
      case 'revenue':
        return {
          totalRevenue: Math.floor(Math.random() * 500000) + 100000,
          growthRate: Math.random() * 20 - 5,
          arpu: Math.floor(Math.random() * 200) + 50
        };
      default:
        return {};
    }
  };

  const getReportIcon = (type: string) => {
    switch (type) {
      case 'transaction': return <PaymentIcon />;
      case 'sms-usage': return <SmsIcon />;
      case 'revenue': return <RevenueIcon />;
      case 'compliance': return <ComplianceIcon />;
      case 'custom': return <CustomIcon />;
      default: return <ReportsIcon />;
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'completed': return 'success';
      case 'generating': case 'processing': return 'warning';
      case 'failed': return 'error';
      default: return 'default';
    }
  };

  const formatCurrency = (amount: number, currency: string = 'SAR') => {
    return new Intl.NumberFormat('en-SA', {
      style: 'currency',
      currency: currency
    }).format(amount);
  };

  const renderReportSummary = (report: ReportData) => {
    if (!report.summary) return null;

    switch (report.type) {
      case 'transaction':
        return (
          <Box display="flex" gap={2} flexWrap="wrap">
            <Chip 
              label={`${report.summary.totalTransactions} Transactions`} 
              color="primary" 
              size="small" 
            />
            <Chip 
              label={formatCurrency(report.summary.totalAmount)} 
              color="success" 
              size="small" 
            />
            <Chip 
              label={`${report.summary.successRate.toFixed(1)}% Success`} 
              color="info" 
              size="small" 
            />
          </Box>
        );
      case 'sms-usage':
        return (
          <Box display="flex" gap={2} flexWrap="wrap">
            <Chip 
              label={`${report.summary.totalMessages} Messages`} 
              color="primary" 
              size="small" 
            />
            <Chip 
              label={formatCurrency(report.summary.totalCost)} 
              color="warning" 
              size="small" 
            />
            <Chip 
              label={`${report.summary.deliveryRate.toFixed(1)}% Delivered`} 
              color="success" 
              size="small" 
            />
          </Box>
        );
      case 'revenue':
        return (
          <Box display="flex" gap={2} flexWrap="wrap">
            <Chip 
              label={formatCurrency(report.summary.totalRevenue)} 
              color="success" 
              size="small" 
            />
            <Chip 
              label={`${report.summary.growthRate > 0 ? '+' : ''}${report.summary.growthRate.toFixed(1)}% Growth`} 
              color={report.summary.growthRate > 0 ? 'success' : 'error'} 
              size="small" 
            />
            <Chip 
              label={`${formatCurrency(report.summary.arpu)} ARPU`} 
              color="info" 
              size="small" 
            />
          </Box>
        );
      default:
        return null;
    }
  };

  const tabContent = [
    <Box key="reports">
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h5" component="h2">
          Generated Reports
        </Typography>
        <Box display="flex" gap={2}>
          <Button
            variant="outlined"
            startIcon={<RefreshIcon />}
            onClick={loadReports}
            disabled={loading}
          >
            Refresh
          </Button>
          <Button
            variant="contained"
            startIcon={<ReportsIcon />}
            onClick={() => setGenerateDialogOpen(true)}
          >
            Generate Report
          </Button>
        </Box>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      <Grid container spacing={3}>
        {reports.map((report) => (
          <Grid item xs={12} md={6} lg={4} key={report.id}>
            <Card>
              <CardContent>
                <Box display="flex" alignItems="center" gap={2} mb={2}>
                  {getReportIcon(report.type)}
                  <Box flex={1}>
                    <Typography variant="h6" component="h3">
                      {report.name}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {format(new Date(report.generatedAt), 'MMM dd, yyyy HH:mm')}
                    </Typography>
                  </Box>
                  <Chip 
                    label={report.status} 
                    color={getStatusColor(report.status) as any}
                    size="small"
                  />
                </Box>

                {report.status === 'generating' && (
                  <LinearProgress sx={{ mb: 2 }} />
                )}

                {renderReportSummary(report)}

                {report.status === 'completed' && (
                  <Box display="flex" gap={1} mt={2}>
                    <Tooltip title="Export to PDF">
                      <IconButton 
                        size="small" 
                        onClick={() => {
                          setSelectedReport(report);
                          setExportDialogOpen(true);
                        }}
                      >
                        <DownloadIcon />
                      </IconButton>
                    </Tooltip>
                  </Box>
                )}
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>

      {reports.length === 0 && !loading && (
        <Box textAlign="center" py={8}>
          <ReportsIcon sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
          <Typography variant="h6" color="text.secondary">
            No reports generated yet
          </Typography>
          <Typography variant="body2" color="text.secondary" mb={3}>
            Generate your first report to get started with analytics
          </Typography>
          <Button
            variant="contained"
            startIcon={<ReportsIcon />}
            onClick={() => setGenerateDialogOpen(true)}
          >
            Generate Report
          </Button>
        </Box>
      )}
    </Box>,

    <Box key="exports">
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h5" component="h2">
          Export Jobs
        </Typography>
        <Button
          variant="outlined"
          startIcon={<RefreshIcon />}
          onClick={loadExportJobs}
        >
          Refresh
        </Button>
      </Box>

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Report</TableCell>
              <TableCell>Format</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Created</TableCell>
              <TableCell>Progress</TableCell>
              <TableCell>Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {exportJobs.map((job) => (
              <TableRow key={job.id}>
                <TableCell>
                  {reports.find(r => r.id === job.reportId)?.name || 'Unknown Report'}
                </TableCell>
                <TableCell>
                  <Chip 
                    label={job.format.toUpperCase()} 
                    size="small"
                    icon={
                      job.format === 'pdf' ? <PdfIcon /> :
                      job.format === 'excel' ? <ExcelIcon /> :
                      <CsvIcon />
                    }
                  />
                </TableCell>
                <TableCell>
                  <Chip 
                    label={job.status} 
                    color={getStatusColor(job.status) as any}
                    size="small"
                  />
                </TableCell>
                <TableCell>
                  {format(new Date(job.createdAt), 'MMM dd, HH:mm')}
                </TableCell>
                <TableCell>
                  {job.status === 'processing' && job.progress !== undefined ? (
                    <Box display="flex" alignItems="center" gap={1}>
                      <LinearProgress 
                        variant="determinate" 
                        value={job.progress} 
                        sx={{ width: 100 }}
                      />
                      <Typography variant="body2">
                        {job.progress}%
                      </Typography>
                    </Box>
                  ) : (
                    <Typography variant="body2" color="text.secondary">
                      {job.status === 'completed' ? '100%' : '-'}
                    </Typography>
                  )}
                </TableCell>
                <TableCell>
                  {job.status === 'completed' && job.downloadUrl && (
                    <Button
                      size="small"
                      startIcon={<DownloadIcon />}
                      onClick={() => window.open(job.downloadUrl, '_blank')}
                    >
                      Download
                    </Button>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      {exportJobs.length === 0 && (
        <Box textAlign="center" py={8}>
          <DownloadIcon sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
          <Typography variant="h6" color="text.secondary">
            No export jobs found
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Export jobs will appear here when you download reports
          </Typography>
        </Box>
      )}
    </Box>
  ];

  return (
    <LocalizationProvider dateAdapter={AdapterDateFns}>
      <Box sx={{ p: 3 }}>
        <Box display="flex" alignItems="center" gap={2} mb={4}>
          <ReportsIcon sx={{ fontSize: 32 }} />
          <Typography variant="h4" component="h1">
            Reports & Analytics
          </Typography>
        </Box>

        <Tabs 
          value={activeTab} 
          onChange={(_, newValue) => setActiveTab(newValue)}
          sx={{ mb: 3 }}
        >
          <Tab label="Reports" />
          <Tab label="Export Jobs" />
        </Tabs>

        {tabContent[activeTab]}

        {/* Generate Report Dialog */}
        <Dialog 
          open={generateDialogOpen} 
          onClose={() => setGenerateDialogOpen(false)}
          maxWidth="md"
          fullWidth
        >
          <DialogTitle>Generate New Report</DialogTitle>
          <DialogContent>
            <Grid container spacing={3} sx={{ mt: 1 }}>
              <Grid item xs={12} md={6}>
                <FormControl fullWidth>
                  <InputLabel>Report Type</InputLabel>
                  <Select
                    value={filters.reportType}
                    label="Report Type"
                    onChange={(e) => setFilters(prev => ({ ...prev, reportType: e.target.value }))}
                  >
                    <MenuItem value="transaction">Transaction Report</MenuItem>
                    <MenuItem value="sms-usage">SMS Usage Report</MenuItem>
                    <MenuItem value="revenue">Revenue Analytics</MenuItem>
                    <MenuItem value="compliance">Compliance Report</MenuItem>
                    <MenuItem value="custom">Custom Report</MenuItem>
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={12} md={6}>
                <DatePicker
                  label="Start Date"
                  value={filters.startDate}
                  onChange={(date: Date | null) => date && setFilters(prev => ({ ...prev, startDate: date }))}
                  slotProps={{ textField: { fullWidth: true } }}
                />
              </Grid>
              <Grid item xs={12} md={6}>
                <DatePicker
                  label="End Date"
                  value={filters.endDate}
                  onChange={(date: Date | null) => date && setFilters(prev => ({ ...prev, endDate: date }))}
                  slotProps={{ textField: { fullWidth: true } }}
                />
              </Grid>
            </Grid>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setGenerateDialogOpen(false)}>
              Cancel
            </Button>
            <Button 
              variant="contained" 
              onClick={() => generateReport(filters.reportType)}
              disabled={loading}
            >
              {loading ? <CircularProgress size={20} /> : 'Generate Report'}
            </Button>
          </DialogActions>
        </Dialog>

        {/* Export Dialog */}
        <Dialog 
          open={exportDialogOpen} 
          onClose={() => setExportDialogOpen(false)}
        >
          <DialogTitle>Export Report</DialogTitle>
          <DialogContent>
            <Typography variant="body1" sx={{ mb: 3 }}>
              Export "{selectedReport?.name}" in your preferred format:
            </Typography>
            <Box display="flex" gap={2} flexDirection="column">
              <Button
                variant="outlined"
                startIcon={<PdfIcon />}
                onClick={() => selectedReport && exportReport(selectedReport.id, 'pdf')}
                fullWidth
              >
                Export as PDF
              </Button>
              <Button
                variant="outlined"
                startIcon={<ExcelIcon />}
                onClick={() => selectedReport && exportReport(selectedReport.id, 'excel')}
                fullWidth
              >
                Export as Excel
              </Button>
              <Button
                variant="outlined"
                startIcon={<CsvIcon />}
                onClick={() => selectedReport && exportReport(selectedReport.id, 'csv')}
                fullWidth
              >
                Export as CSV
              </Button>
            </Box>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setExportDialogOpen(false)}>
              Cancel
            </Button>
          </DialogActions>
        </Dialog>
      </Box>
    </LocalizationProvider>
  );
};

export default Reports;
