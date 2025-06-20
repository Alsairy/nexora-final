import React from 'react';
import {
  FormControl,
  Select,
  MenuItem,
  Box,
  Typography,
  SelectChangeEvent
} from '@mui/material';
import { Language as LanguageIcon } from '@mui/icons-material';
import { useLanguage } from '../contexts/LanguageContext';

interface LanguageSelectorProps {
  variant?: 'header' | 'settings';
  showIcon?: boolean;
}

export const LanguageSelector: React.FC<LanguageSelectorProps> = ({ 
  variant = 'header',
  showIcon = true 
}) => {
  const { language, setLanguage, t } = useLanguage();

  const handleLanguageChange = (event: SelectChangeEvent<string>) => {
    setLanguage(event.target.value as 'en' | 'ar');
  };

  const languages = [
    { code: 'en', name: 'English', nativeName: 'English' },
    { code: 'ar', name: 'Arabic', nativeName: 'العربية' }
  ];

  if (variant === 'settings') {
    return (
      <Box sx={{ minWidth: 200 }}>
        <Typography variant="subtitle2" gutterBottom>
          {t('common.language.selectLanguage')}
        </Typography>
        <FormControl fullWidth>
          <Select
            value={language}
            onChange={handleLanguageChange}
            displayEmpty
          >
            {languages.map((lang) => (
              <MenuItem key={lang.code} value={lang.code}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Typography>{lang.nativeName}</Typography>
                </Box>
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      </Box>
    );
  }

  return (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
      {showIcon && <LanguageIcon sx={{ fontSize: 20 }} />}
      <FormControl size="small" sx={{ minWidth: 100 }}>
        <Select
          value={language}
          onChange={handleLanguageChange}
          variant="outlined"
          sx={{
            '& .MuiOutlinedInput-notchedOutline': {
              border: 'none',
            },
            '& .MuiSelect-select': {
              py: 0.5,
              fontSize: '0.875rem',
            },
          }}
        >
          {languages.map((lang) => (
            <MenuItem key={lang.code} value={lang.code}>
              <Typography variant="body2">
                {lang.nativeName}
              </Typography>
            </MenuItem>
          ))}
        </Select>
      </FormControl>
    </Box>
  );
};
