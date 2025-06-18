import React, { forwardRef } from 'react';

import {
  FormControl,
  FormHelperText,
  InputLabel,
  MenuItem,
  Select,
  SelectProps,
  Typography,
} from '@mui/material';
import { styled } from '@mui/material/styles';

export interface DropdownOption {
  value: string | number;
  label: string;
  disabled?: boolean;
}

export interface DropdownProps extends Omit<SelectProps, 'onChange'> {
  options: DropdownOption[];
  label: string;
  value?: string | number;
  onChange?: (value: string | number) => void;
  helperText?: string;
  error?: boolean;
  required?: boolean;
  fullWidth?: boolean;
  description?: string;
}

const StyledFormControl = styled(FormControl)(({ theme }) => ({
  marginBottom: theme.spacing(2),
}));

const InputDescription = styled(Typography)(({ theme }) => ({
  marginTop: theme.spacing(0.5),
  fontSize: '0.75rem',
  color: theme.palette.text.secondary,
}));

/**
 * Accessible dropdown component with label, helper text, and error state.
 */
const Dropdown = forwardRef<HTMLInputElement, DropdownProps>(
  (
    {
      id,
      options,
      label,
      value,
      onChange,
      helperText,
      error = false,
      required = false,
      fullWidth = true,
      description,
      disabled = false,
      ...rest
    },
    ref
  ) => {
    // Generate unique IDs for accessibility
    const selectId = id || `dropdown-${label.toLowerCase().replace(/\s+/g, '-')}`;
    const labelId = `${selectId}-label`;
    const helperId = `${selectId}-helper`;
    const descriptionId = description ? `${selectId}-description` : undefined;

    // Combine aria-describedby values
    const ariaDescribedBy = [
      helperText ? helperId : null,
      descriptionId,
      rest['aria-describedby'],
    ]
      .filter(Boolean)
      .join(' ');

    // Handle change event
    const handleChange = (event: React.ChangeEvent<{ value: unknown }>) => {
      if (onChange) {
        onChange(event.target.value as string | number);
      }
    };

    return (
      <StyledFormControl
        variant="outlined"
        fullWidth={fullWidth}
        error={error}
        required={required}
        disabled={disabled}
      >
        <InputLabel id={labelId} htmlFor={selectId}>
          {label}
        </InputLabel>
        
        <Select
          id={selectId}
          labelId={labelId}
          value={value || ''}
          onChange={handleChange as any}
          label={label}
          inputRef={ref}
          aria-describedby={ariaDescribedBy || undefined}
          aria-invalid={error}
          aria-required={required}
          {...rest}
        >
          {options.map((option) => (
            <MenuItem
              key={option.value}
              value={option.value}
              disabled={option.disabled}
            >
              {option.label}
            </MenuItem>
          ))}
        </Select>
        
        {helperText && (
          <FormHelperText id={helperId}>{helperText}</FormHelperText>
        )}
        
        {description && (
          <InputDescription id={descriptionId} variant="caption">
            {description}
          </InputDescription>
        )}
      </StyledFormControl>
    );
  }
);

Dropdown.displayName = 'Dropdown';

export default Dropdown;

