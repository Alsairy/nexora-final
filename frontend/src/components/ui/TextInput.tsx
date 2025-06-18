import React, { forwardRef } from 'react';

import {
  FormControl,
  FormHelperText,
  InputLabel,
  OutlinedInput,
  OutlinedInputProps,
  Typography,
} from '@mui/material';
import { styled } from '@mui/material/styles';

export interface TextInputProps extends Omit<OutlinedInputProps, 'label'> {
  label: string;
  helperText?: string;
  error?: boolean;
  required?: boolean;
  fullWidth?: boolean;
  startAdornment?: React.ReactNode;
  endAdornment?: React.ReactNode;
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
 * Accessible text input component with label, helper text, and error state.
 */
const TextInput = forwardRef<HTMLInputElement, TextInputProps>(
  (
    {
      id,
      label,
      helperText,
      error = false,
      required = false,
      fullWidth = true,
      startAdornment,
      endAdornment,
      description,
      disabled = false,
      ...rest
    },
    ref
  ) => {
    // Generate unique IDs for accessibility
    const inputId = id || `input-${label.toLowerCase().replace(/\s+/g, '-')}`;
    const helperId = `${inputId}-helper`;
    const descriptionId = description ? `${inputId}-description` : undefined;

    // Combine aria-describedby values
    const ariaDescribedBy = [
      helperText ? helperId : null,
      descriptionId,
      rest['aria-describedby'],
    ]
      .filter(Boolean)
      .join(' ');

    return (
      <StyledFormControl variant="outlined" fullWidth={fullWidth} error={error}>
        <InputLabel htmlFor={inputId} required={required}>
          {label}
        </InputLabel>
        <OutlinedInput
          id={inputId}
          label={label}
          ref={ref}
          startAdornment={startAdornment}
          endAdornment={endAdornment}
          aria-describedby={ariaDescribedBy || undefined}
          aria-invalid={error}
          aria-required={required}
          disabled={disabled}
          {...rest}
        />
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

TextInput.displayName = 'TextInput';

export default TextInput;

