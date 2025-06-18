import React from 'react';

import {
  Tooltip as MuiTooltip,
  TooltipProps as MuiTooltipProps,
} from '@mui/material';
import { styled } from '@mui/material/styles';

export interface TooltipProps extends Omit<MuiTooltipProps, 'title'> {
  content: React.ReactNode;
  children: React.ReactElement;
  disabled?: boolean;
  id?: string;
}

const StyledTooltip = styled(MuiTooltip)(({ theme }) => ({
  '& .MuiTooltip-tooltip': {
    backgroundColor: theme.palette.grey[800],
    color: theme.palette.common.white,
    fontSize: '0.75rem',
    padding: theme.spacing(1, 1.5),
    maxWidth: 300,
    wordBreak: 'break-word',
  },
}));

/**
 * Accessible tooltip component with customizable content and positioning.
 */
const Tooltip: React.FC<TooltipProps> = ({
  content,
  children,
  disabled = false,
  id,
  ...rest
}) => {
  // Generate unique ID for accessibility
  const tooltipId = id || `tooltip-${Math.random().toString(36).substr(2, 9)}`;

  // If disabled, just render children
  if (disabled || !content) {
    return children;
  }

  // Clone child element to add aria attributes
  const child = React.cloneElement(children, {
    'aria-describedby': tooltipId,
  });

  return (
    <StyledTooltip
      title={content}
      id={tooltipId}
      arrow
      enterTouchDelay={50}
      leaveTouchDelay={1500}
      {...rest}
    >
      {child}
    </StyledTooltip>
  );
};

export default Tooltip;

