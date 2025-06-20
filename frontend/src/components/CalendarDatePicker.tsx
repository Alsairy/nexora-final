import React, { useState, useEffect } from 'react';
import {
  Box,
  TextField,
  IconButton,
  Popover,
  Paper,
  Typography,
  Grid,
  Button,
  ToggleButton,
  ToggleButtonGroup
} from '@mui/material';
import {
  CalendarToday,
  ChevronLeft,
  ChevronRight,
  Today
} from '@mui/icons-material';
import { useLanguage } from '../contexts/LanguageContext';
import {
  CalendarType,
  CalendarConfig,
  formatDateByCalendar,
  getCalendarDisplayName,
  getCalendarMonthNames,
  getCalendarDayNamesShort,
  addCalendarMonths
} from '../utils/calendar/calendarUtils';
import {
  convertToHijri,
  convertToGregorian,
  getHijriDaysInMonth
} from '../utils/calendar/hijriCalendar';

interface CalendarDatePickerProps {
  value?: Date | null;
  onChange: (date: Date | null) => void;
  label?: string;
  disabled?: boolean;
  error?: boolean;
  helperText?: string;
  minDate?: Date;
  maxDate?: Date;
  defaultCalendarType?: CalendarType;
  showCalendarToggle?: boolean;
  showBothCalendars?: boolean;
  fullWidth?: boolean;
}

export const CalendarDatePicker: React.FC<CalendarDatePickerProps> = ({
  value,
  onChange,
  label,
  disabled = false,
  error = false,
  helperText,
  minDate,
  maxDate,
  defaultCalendarType = 'gregorian',
  showCalendarToggle = true,
  showBothCalendars = false,
  fullWidth = false
}) => {
  const { language, t, isRTL } = useLanguage();
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);
  const [calendarType, setCalendarType] = useState<CalendarType>(defaultCalendarType);
  const [viewDate, setViewDate] = useState<Date>(value || new Date());
  const [selectedDate, setSelectedDate] = useState<Date | null>(value || null);

  const calendarConfig: CalendarConfig = {
    type: calendarType,
    language,
    showBothCalendars
  };

  useEffect(() => {
    setSelectedDate(value || null);
    if (value) {
      setViewDate(value);
    }
  }, [value]);

  const handleOpenCalendar = (event: React.MouseEvent<HTMLElement>) => {
    if (!disabled) {
      setAnchorEl(event.currentTarget);
    }
  };

  const handleCloseCalendar = () => {
    setAnchorEl(null);
  };

  const handleDateSelect = (date: Date) => {
    setSelectedDate(date);
    onChange(date);
    handleCloseCalendar();
  };

  const handleCalendarTypeChange = (
    _event: React.MouseEvent<HTMLElement>,
    newType: CalendarType | null
  ) => {
    if (newType) {
      setCalendarType(newType);
    }
  };

  const handlePreviousMonth = () => {
    setViewDate(addCalendarMonths(viewDate, -1, calendarType));
  };

  const handleNextMonth = () => {
    setViewDate(addCalendarMonths(viewDate, 1, calendarType));
  };

  const handleToday = () => {
    const today = new Date();
    setViewDate(today);
    setSelectedDate(today);
    onChange(today);
    handleCloseCalendar();
  };

  const isDateDisabled = (date: Date): boolean => {
    if (minDate && date < minDate) return true;
    if (maxDate && date > maxDate) return true;
    return false;
  };

  const formatDisplayValue = (): string => {
    if (!selectedDate) return '';
    const formatted = formatDateByCalendar(selectedDate, calendarConfig);
    return showBothCalendars && formatted.secondary 
      ? `${formatted.primary} (${formatted.secondary})`
      : formatted.primary;
  };

  const renderCalendarGrid = () => {
    const monthNames = getCalendarMonthNames(calendarType, language);
    const dayNames = getCalendarDayNamesShort(language);
    
    let currentMonth: number;
    let currentYear: number;
    let daysInMonth: number;
    let firstDayOfWeek: number;

    if (calendarType === 'gregorian') {
      currentMonth = viewDate.getMonth();
      currentYear = viewDate.getFullYear();
      daysInMonth = new Date(currentYear, currentMonth + 1, 0).getDate();
      firstDayOfWeek = new Date(currentYear, currentMonth, 1).getDay();
    } else {
      const hijriDate = convertToHijri(viewDate);
      currentMonth = hijriDate.month;
      currentYear = hijriDate.year;
      daysInMonth = getHijriDaysInMonth(currentYear, currentMonth);
      const firstDay = convertToGregorian({ year: currentYear, month: currentMonth, day: 1 });
      firstDayOfWeek = firstDay.getDay();
    }

    const days: JSX.Element[] = [];
    
    for (let i = 0; i < firstDayOfWeek; i++) {
      days.push(<Box key={`empty-${i}`} sx={{ height: 40 }} />);
    }

    for (let day = 1; day <= daysInMonth; day++) {
      let dateToCheck: Date;
      
      if (calendarType === 'gregorian') {
        dateToCheck = new Date(currentYear, currentMonth, day);
      } else {
        dateToCheck = convertToGregorian({ year: currentYear, month: currentMonth, day });
      }

      const isSelected = selectedDate && 
        dateToCheck.toDateString() === selectedDate.toDateString();
      const isToday = dateToCheck.toDateString() === new Date().toDateString();
      const isDisabled = isDateDisabled(dateToCheck);

      days.push(
        <Button
          key={day}
          variant={isSelected ? 'contained' : 'text'}
          color={isToday ? 'secondary' : 'primary'}
          disabled={isDisabled}
          onClick={() => handleDateSelect(dateToCheck)}
          sx={{
            minWidth: 40,
            height: 40,
            borderRadius: 1,
            fontSize: '0.875rem',
            '&:hover': {
              backgroundColor: isSelected ? undefined : 'action.hover'
            }
          }}
        >
          {day}
        </Button>
      );
    }

    return (
      <Box sx={{ p: 2 }}>
        {/* Calendar Header */}
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
          <IconButton onClick={handlePreviousMonth} size="small">
            {isRTL ? <ChevronRight /> : <ChevronLeft />}
          </IconButton>
          
          <Typography variant="h6" sx={{ textAlign: 'center', minWidth: 200 }}>
            {monthNames[currentMonth - 1]} {currentYear}
          </Typography>
          
          <IconButton onClick={handleNextMonth} size="small">
            {isRTL ? <ChevronLeft /> : <ChevronRight />}
          </IconButton>
        </Box>

        {/* Day Names Header */}
        <Grid container spacing={0} sx={{ mb: 1 }}>
          {dayNames.map((dayName, index) => (
            <Grid item xs key={index} sx={{ display: 'flex', justifyContent: 'center' }}>
              <Typography variant="caption" sx={{ fontWeight: 'bold', color: 'text.secondary' }}>
                {dayName}
              </Typography>
            </Grid>
          ))}
        </Grid>

        {/* Calendar Grid */}
        <Grid container spacing={0}>
          {days.map((day, index) => (
            <Grid item xs key={index} sx={{ display: 'flex', justifyContent: 'center' }}>
              {day}
            </Grid>
          ))}
        </Grid>

        {/* Footer Actions */}
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mt: 2, pt: 2, borderTop: 1, borderColor: 'divider' }}>
          <Button
            startIcon={<Today />}
            onClick={handleToday}
            size="small"
            variant="outlined"
          >
            {t('common.dates.today')}
          </Button>
          
          {showCalendarToggle && (
            <ToggleButtonGroup
              value={calendarType}
              exclusive
              onChange={handleCalendarTypeChange}
              size="small"
            >
              <ToggleButton value="gregorian">
                {getCalendarDisplayName('gregorian', language)}
              </ToggleButton>
              <ToggleButton value="hijri">
                {getCalendarDisplayName('hijri', language)}
              </ToggleButton>
            </ToggleButtonGroup>
          )}
        </Box>
      </Box>
    );
  };

  return (
    <Box>
      <TextField
        fullWidth={fullWidth}
        label={label}
        value={formatDisplayValue()}
        onClick={handleOpenCalendar}
        disabled={disabled}
        error={error}
        helperText={helperText}
        InputProps={{
          readOnly: true,
          endAdornment: (
            <IconButton onClick={handleOpenCalendar} disabled={disabled} edge="end">
              <CalendarToday />
            </IconButton>
          )
        }}
        sx={{ cursor: disabled ? 'default' : 'pointer' }}
      />

      <Popover
        open={Boolean(anchorEl)}
        anchorEl={anchorEl}
        onClose={handleCloseCalendar}
        anchorOrigin={{
          vertical: 'bottom',
          horizontal: isRTL ? 'right' : 'left'
        }}
        transformOrigin={{
          vertical: 'top',
          horizontal: isRTL ? 'right' : 'left'
        }}
      >
        <Paper elevation={3} sx={{ minWidth: 320 }}>
          {renderCalendarGrid()}
        </Paper>
      </Popover>
    </Box>
  );
};
