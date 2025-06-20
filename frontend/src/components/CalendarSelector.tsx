import React from 'react';
import {
  FormControl,
  Select,
  MenuItem,
  Box,
  Typography,
  SelectChangeEvent,
  ToggleButton,
  ToggleButtonGroup
} from '@mui/material';
import { CalendarToday, Event } from '@mui/icons-material';
import { useLanguage } from '../contexts/LanguageContext';
import { CalendarType, getCalendarDisplayName } from '../utils/calendar/calendarUtils';

interface CalendarSelectorProps {
  value: CalendarType;
  onChange: (calendarType: CalendarType) => void;
  variant?: 'dropdown' | 'toggle';
  showIcon?: boolean;
  size?: 'small' | 'medium';
  disabled?: boolean;
}

export const CalendarSelector: React.FC<CalendarSelectorProps> = ({
  value,
  onChange,
  variant = 'dropdown',
  showIcon = true,
  size = 'medium',
  disabled = false
}) => {
  const { language } = useLanguage();

  const handleChange = (event: SelectChangeEvent<CalendarType>) => {
    onChange(event.target.value as CalendarType);
  };

  const handleToggleChange = (
    _event: React.MouseEvent<HTMLElement>,
    newValue: CalendarType | null
  ) => {
    if (newValue) {
      onChange(newValue);
    }
  };

  const calendarOptions = [
    { value: 'gregorian' as CalendarType, icon: <CalendarToday /> },
    { value: 'hijri' as CalendarType, icon: <Event /> }
  ];

  if (variant === 'toggle') {
    return (
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
        {showIcon && <CalendarToday sx={{ fontSize: size === 'small' ? 20 : 24 }} />}
        <ToggleButtonGroup
          value={value}
          exclusive
          onChange={handleToggleChange}
          size={size}
          disabled={disabled}
        >
          {calendarOptions.map((option) => (
            <ToggleButton key={option.value} value={option.value}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                {showIcon && option.icon}
                <Typography variant={size === 'small' ? 'caption' : 'body2'}>
                  {getCalendarDisplayName(option.value, language)}
                </Typography>
              </Box>
            </ToggleButton>
          ))}
        </ToggleButtonGroup>
      </Box>
    );
  }

  return (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
      {showIcon && <CalendarToday sx={{ fontSize: size === 'small' ? 20 : 24 }} />}
      <FormControl size={size} disabled={disabled} sx={{ minWidth: 120 }}>
        <Select
          value={value}
          onChange={handleChange}
          displayEmpty
        >
          {calendarOptions.map((option) => (
            <MenuItem key={option.value} value={option.value}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                {showIcon && option.icon}
                <Typography>
                  {getCalendarDisplayName(option.value, language)}
                </Typography>
              </Box>
            </MenuItem>
          ))}
        </Select>
      </FormControl>
    </Box>
  );
};
