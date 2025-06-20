import React, { useState, useEffect } from 'react';
import { Link as RouterLink } from 'react-router-dom';

import { Box, Button, Link, Paper, Typography, useTheme } from '@mui/material';
import { styled } from '@mui/material/styles';

import { ROUTES } from '../routes';

interface CookieBannerProps {
  onAccept?: () => void;
  onDecline?: () => void;
}

const BannerContainer = styled(Paper)(({ theme }) => ({
  position: 'fixed',
  bottom: 0,
  left: 0,
  right: 0,
  zIndex: theme.zIndex.snackbar,
  padding: theme.spacing(2, 3),
  display: 'flex',
  flexDirection: 'column',
  alignItems: 'flex-start',
  [theme.breakpoints.up('sm')]: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  boxShadow: theme.shadows[6],
  borderRadius: 0,
}));

const ButtonContainer = styled(Box)(({ theme }) => ({
  display: 'flex',
  gap: theme.spacing(2),
  marginTop: theme.spacing(2),
  [theme.breakpoints.up('sm')]: {
    marginTop: 0,
    marginLeft: 'auto',
  },
}));

/**
 * Cookie consent banner component that displays privacy information
 * and provides options to accept or decline cookies.
 */
const CookieBanner: React.FC<CookieBannerProps> = ({ onAccept, onDecline }) => {
  const [isVisible, setIsVisible] = useState<boolean>(false);
  const theme = useTheme();

  useEffect(() => {
    // Check if user has already made a cookie choice
    const cookieConsent = localStorage.getItem('cookie-consent');
    
    if (!cookieConsent) {
      // Show banner after a short delay
      const timer = setTimeout(() => {
        setIsVisible(true);
      }, 1000);
      
      return () => clearTimeout(timer);
    }
    
    return undefined;
  }, []);

  const handleAccept = (): void => {
    localStorage.setItem('cookie-consent', 'accepted');
    setIsVisible(false);
    if (onAccept) onAccept();
  };

  const handleDecline = (): void => {
    localStorage.setItem('cookie-consent', 'declined');
    setIsVisible(false);
    if (onDecline) onDecline();
  };

  if (!isVisible) {
    return null;
  }

  return (
    <BannerContainer 
      role="alertdialog"
      aria-labelledby="cookie-title"
      aria-describedby="cookie-description"
    >
      <Box>
        <Typography 
          variant="h6" 
          component="h2" 
          id="cookie-title"
          gutterBottom
        >
          Cookie Consent
        </Typography>
        <Typography 
          variant="body2" 
          color="textSecondary" 
          id="cookie-description"
        >
          We use cookies to enhance your experience on our website. By continuing to use this site, 
          you consent to our use of cookies. Learn more in our{' '}
          <Link 
            component={RouterLink} 
            to={ROUTES.PRIVACY_POLICY}
            color="primary"
          >
            Privacy Policy
          </Link>.
        </Typography>
      </Box>
      
      <ButtonContainer>
        <Button 
          variant="outlined" 
          color="primary" 
          onClick={handleDecline}
          aria-label="Decline cookies"
        >
          Decline
        </Button>
        <Button 
          variant="contained" 
          color="primary" 
          onClick={handleAccept}
          aria-label="Accept cookies"
        >
          Accept
        </Button>
      </ButtonContainer>
    </BannerContainer>
  );
};

export default CookieBanner;

