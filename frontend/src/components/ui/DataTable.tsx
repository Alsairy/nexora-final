import React, { useState } from 'react';

import {
  Box,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TableSortLabel,
  Typography,
  visuallyHidden,
} from '@mui/material';
import { styled } from '@mui/material/styles';

type Order = 'asc' | 'desc';

export interface Column<T> {
  id: keyof T;
  label: string;
  minWidth?: number;
  align?: 'right' | 'left' | 'center';
  format?: (value: any) => React.ReactNode;
  sortable?: boolean;
}

export interface DataTableProps<T> {
  columns: Column<T>[];
  rows: T[];
  defaultSortColumn?: keyof T;
  defaultSortDirection?: Order;
  onSort?: (column: keyof T, direction: Order) => void;
  rowsPerPageOptions?: number[];
  defaultRowsPerPage?: number;
  onPageChange?: (page: number) => void;
  onRowsPerPageChange?: (rowsPerPage: number) => void;
  totalCount?: number;
  page?: number;
  rowsPerPage?: number;
  loading?: boolean;
  emptyMessage?: string;
  getRowId?: (row: T) => string | number;
  onRowClick?: (row: T) => void;
  caption?: string;
  ariaLabel?: string;
}

const StyledTableContainer = styled(TableContainer)(({ theme }) => ({
  maxHeight: '100%',
  overflow: 'auto',
}));

const EmptyStateContainer = styled(Box)(({ theme }) => ({
  padding: theme.spacing(4),
  textAlign: 'center',
}));

/**
 * Accessible data table component with sorting, pagination, and empty state.
 */
function DataTable<T extends object>({
  columns,
  rows,
  defaultSortColumn,
  defaultSortDirection = 'asc',
  onSort,
  rowsPerPageOptions = [10, 25, 50, 100],
  defaultRowsPerPage = 10,
  onPageChange,
  onRowsPerPageChange,
  totalCount,
  page: controlledPage,
  rowsPerPage: controlledRowsPerPage,
  loading = false,
  emptyMessage = 'No data available',
  getRowId = (row: any) => row.id,
  onRowClick,
  caption,
  ariaLabel = 'Data table',
}: DataTableProps<T>) {
  // State for uncontrolled mode
  const [uncontrolledPage, setUncontrolledPage] = useState(0);
  const [uncontrolledRowsPerPage, setUncontrolledRowsPerPage] = useState(defaultRowsPerPage);
  const [orderBy, setOrderBy] = useState<keyof T | undefined>(defaultSortColumn);
  const [order, setOrder] = useState<Order>(defaultSortDirection);

  // Determine if we're in controlled or uncontrolled mode
  const isControlled = controlledPage !== undefined && controlledRowsPerPage !== undefined;
  const currentPage = isControlled ? controlledPage : uncontrolledPage;
  const currentRowsPerPage = isControlled ? controlledRowsPerPage : uncontrolledRowsPerPage;

  // Handle sort
  const handleRequestSort = (property: keyof T) => {
    const isAsc = orderBy === property && order === 'asc';
    const newOrder = isAsc ? 'desc' : 'asc';
    
    setOrder(newOrder);
    setOrderBy(property);
    
    if (onSort) {
      onSort(property, newOrder);
    }
  };

  // Handle page change
  const handleChangePage = (_event: unknown, newPage: number) => {
    if (!isControlled) {
      setUncontrolledPage(newPage);
    }
    
    if (onPageChange) {
      onPageChange(newPage);
    }
  };

  // Handle rows per page change
  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    const newRowsPerPage = parseInt(event.target.value, 10);
    
    if (!isControlled) {
      setUncontrolledRowsPerPage(newRowsPerPage);
      setUncontrolledPage(0);
    }
    
    if (onRowsPerPageChange) {
      onRowsPerPageChange(newRowsPerPage);
    }
    
    if (onPageChange && !isControlled) {
      onPageChange(0);
    }
  };

  // Sort rows if not controlled
  const sortedRows = React.useMemo(() => {
    if (!orderBy || onSort) {
      return rows;
    }
    
    return [...rows].sort((a, b) => {
      const aValue = a[orderBy];
      const bValue = b[orderBy];
      
      if (aValue === bValue) {
        return 0;
      }
      
      if (aValue === null || aValue === undefined) {
        return order === 'asc' ? -1 : 1;
      }
      
      if (bValue === null || bValue === undefined) {
        return order === 'asc' ? 1 : -1;
      }
      
      if (typeof aValue === 'string' && typeof bValue === 'string') {
        return order === 'asc'
          ? aValue.localeCompare(bValue)
          : bValue.localeCompare(aValue);
      }
      
      return order === 'asc'
        ? (aValue < bValue ? -1 : 1)
        : (bValue < aValue ? -1 : 1);
    });
  }, [rows, orderBy, order, onSort]);

  // Empty state
  if (!loading && rows.length === 0) {
    return (
      <Paper>
        <EmptyStateContainer>
          <Typography variant="body1" color="textSecondary">
            {emptyMessage}
          </Typography>
        </EmptyStateContainer>
      </Paper>
    );
  }

  return (
    <Paper>
      <StyledTableContainer>
        <Table stickyHeader aria-label={ariaLabel}>
          {caption && <caption>{caption}</caption>}
          
          <TableHead>
            <TableRow>
              {columns.map((column) => (
                <TableCell
                  key={String(column.id)}
                  align={column.align || 'left'}
                  style={{ minWidth: column.minWidth }}
                  sortDirection={orderBy === column.id ? order : false}
                >
                  {column.sortable !== false ? (
                    <TableSortLabel
                      active={orderBy === column.id}
                      direction={orderBy === column.id ? order : 'asc'}
                      onClick={() => handleRequestSort(column.id)}
                    >
                      {column.label}
                      {orderBy === column.id ? (
                        <Box component="span" sx={visuallyHidden}>
                          {order === 'desc' ? 'sorted descending' : 'sorted ascending'}
                        </Box>
                      ) : null}
                    </TableSortLabel>
                  ) : (
                    column.label
                  )}
                </TableCell>
              ))}
            </TableRow>
          </TableHead>
          
          <TableBody>
            {sortedRows.map((row) => {
              const rowId = getRowId(row);
              
              return (
                <TableRow
                  hover
                  tabIndex={-1}
                  key={rowId}
                  onClick={onRowClick ? () => onRowClick(row) : undefined}
                  sx={onRowClick ? { cursor: 'pointer' } : undefined}
                  role={onRowClick ? 'button' : undefined}
                >
                  {columns.map((column) => {
                    const value = row[column.id];
                    return (
                      <TableCell key={`${rowId}-${String(column.id)}`} align={column.align}>
                        {column.format ? column.format(value) : value}
                      </TableCell>
                    );
                  })}
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </StyledTableContainer>
      
      <TablePagination
        rowsPerPageOptions={rowsPerPageOptions}
        component="div"
        count={totalCount || rows.length}
        rowsPerPage={currentRowsPerPage}
        page={currentPage}
        onPageChange={handleChangePage}
        onRowsPerPageChange={handleChangeRowsPerPage}
        labelDisplayedRows={({ from, to, count }) => `${from}-${to} of ${count}`}
        labelRowsPerPage="Rows per page:"
      />
    </Paper>
  );
}

export default DataTable;

