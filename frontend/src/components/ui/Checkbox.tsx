import type { CheckboxProps as MuiCheckboxProps, FormControlLabelProps } from '@mui/material';
import {
  Checkbox as MuiCheckbox,
  FormControl,
  FormControlLabel,
  FormGroup,
  FormHelperText,
  Typography,
} from '@mui/material';
import { styled } from '@mui/material/styles';
import React, { forwardRef } from 'react';

export interface CheckboxProps extends Omit<MuiCheckboxProps, 'onChange' | 'checked'> {
  label: string;
  checked?: boolean;
  onChange?: (checked: boolean) => void;
  helperText?: string;
  error?: boolean;
  labelPlacement?: FormControlLabelProps['labelPlacement'];
  description?: string;
  required?: boolean;
}

const StyledFormControl = styled(FormControl)(({ theme }) => ({
  marginBottom: theme.spacing(1),
}));

const InputDescription = styled(Typography)(({ theme }) => ({
  marginTop: theme.spacing(0.5),
  fontSize: '0.75rem',
  color: theme.palette.text.secondary,
}));

/**
 * Accessible checkbox component with label, helper text, and error state.
 */
const Checkbox = forwardRef<HTMLInputElement, CheckboxProps>(
  (
    {
      id,
      label,
      checked = false,
      onChange,
      helperText,
      error = false,
      labelPlacement = 'end',
      description,
      required = false,
      disabled = false,
      ...rest
    },
    ref,
  ) => {
    // Generate unique IDs for accessibility
    const checkboxId = id || `checkbox-${label.toLowerCase().replace(/\s+/g, '-')}`;
    const helperId = `${checkboxId}-helper`;
    const descriptionId = description ? `${checkboxId}-description` : undefined;

    // Combine aria-describedby values
    const ariaDescribedBy = [helperText ? helperId : null, descriptionId, rest['aria-describedby']]
      .filter(Boolean)
      .join(' ');

    // Handle change event
    const handleChange = (event: React.ChangeEvent<HTMLInputElement>) => {
      if (onChange) {
        onChange(event.target.checked);
      }
    };

    return (
      <StyledFormControl error={error} required={required}>
        <FormGroup>
          <FormControlLabel
            control={
              <MuiCheckbox
                id={checkboxId}
                checked={checked}
                onChange={handleChange}
                inputRef={ref}
                inputProps={
                  {
                    'aria-describedby': ariaDescribedBy || undefined,
                    'aria-invalid': error,
                    'aria-required': required,
                  } as React.InputHTMLAttributes<HTMLInputElement>
                }
                disabled={disabled}
                {...rest}
              />
            }
            label={
              <Typography
                component="span"
                variant="body2"
                color={disabled ? 'text.disabled' : 'text.primary'}
              >
                {label}
                {required && (
                  <Typography component="span" color="error" aria-hidden="true">
                    {' *'}
                  </Typography>
                )}
              </Typography>
            }
            labelPlacement={labelPlacement}
          />
        </FormGroup>

        {helperText && <FormHelperText id={helperId}>{helperText}</FormHelperText>}

        {description && (
          <InputDescription id={descriptionId} variant="caption">
            {description}
          </InputDescription>
        )}
      </StyledFormControl>
    );
  },
);

Checkbox.displayName = 'Checkbox';

export default Checkbox;
