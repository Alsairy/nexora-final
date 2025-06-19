import { Box, Breadcrumbs, Container, Link, Paper, Typography } from '@mui/material';
import { styled } from '@mui/material/styles';
import React from 'react';
import { Link as RouterLink } from 'react-router-dom';

import ROUTES from '../routes';

const StyledPaper = styled(Paper)(({ theme }) => ({
  padding: theme.spacing(4),
  marginTop: theme.spacing(3),
  marginBottom: theme.spacing(3),
}));

const Section = styled(Box)(({ theme }) => ({
  marginBottom: theme.spacing(4),
}));

const SectionTitle = styled(Typography)(({ theme }) => ({
  marginBottom: theme.spacing(2),
}));

/**
 * Privacy Policy page component.
 */
const PrivacyPolicy: React.FC = () => {
  return (
    <Container maxWidth="lg">
      <Box my={4}>
        <Breadcrumbs aria-label="breadcrumb">
          <Link component={RouterLink} to={ROUTES.DASHBOARD} color="inherit">
            Dashboard
          </Link>
          <Typography color="textPrimary">Privacy Policy</Typography>
        </Breadcrumbs>

        <Typography variant="h4" component="h1" gutterBottom mt={2}>
          Privacy Policy
        </Typography>

        <StyledPaper elevation={2}>
          <Section>
            <SectionTitle variant="h5" component="h2">
              Introduction
            </SectionTitle>
            <Typography paragraph>
              Nexora ("we", "our", or "us") is committed to protecting your privacy. This Privacy
              Policy explains how we collect, use, disclose, and safeguard your information when you
              use our platform and services.
            </Typography>
            <Typography paragraph>
              By accessing or using the Nexora Platform, you agree to the collection and use of
              information in accordance with this policy. If you do not agree with our policies and
              practices, please do not use our services.
            </Typography>
          </Section>

          <Section>
            <SectionTitle variant="h5" component="h2">
              Information We Collect
            </SectionTitle>
            <Typography paragraph>
              We collect several types of information from and about users of our platform,
              including:
            </Typography>
            <Typography component="ul" sx={{ pl: 4 }}>
              <li>
                <Typography paragraph>
                  <strong>Personal Information:</strong> Name, email address, phone number, billing
                  information, and other identifiers by which you may be contacted online or
                  offline.
                </Typography>
              </li>
              <li>
                <Typography paragraph>
                  <strong>Transaction Information:</strong> Details about payments to and from you
                  and other details of products or services you have purchased through our platform.
                </Typography>
              </li>
              <li>
                <Typography paragraph>
                  <strong>Usage Information:</strong> Information about how you use our website,
                  products, and services.
                </Typography>
              </li>
              <li>
                <Typography paragraph>
                  <strong>Device Information:</strong> Information about your internet connection,
                  the equipment you use to access our platform, and usage details.
                </Typography>
              </li>
            </Typography>
          </Section>

          <Section>
            <SectionTitle variant="h5" component="h2">
              How We Use Your Information
            </SectionTitle>
            <Typography paragraph>
              We use the information we collect about you or that you provide to us, including any
              personal information:
            </Typography>
            <Typography component="ul" sx={{ pl: 4 }}>
              <li>
                <Typography paragraph>
                  To provide, maintain, and improve our platform and services.
                </Typography>
              </li>
              <li>
                <Typography paragraph>
                  To process transactions and send related information.
                </Typography>
              </li>
              <li>
                <Typography paragraph>
                  To provide customer support and respond to inquiries.
                </Typography>
              </li>
              <li>
                <Typography paragraph>
                  To send administrative information, such as updates, security alerts, and support
                  messages.
                </Typography>
              </li>
              <li>
                <Typography paragraph>
                  To personalize your experience and deliver content relevant to your interests.
                </Typography>
              </li>
              <li>
                <Typography paragraph>
                  To comply with legal obligations and enforce our terms of service.
                </Typography>
              </li>
            </Typography>
          </Section>

          <Section>
            <SectionTitle variant="h5" component="h2">
              Cookies and Similar Technologies
            </SectionTitle>
            <Typography paragraph>
              We use cookies and similar tracking technologies to track activity on our platform and
              hold certain information. Cookies are files with a small amount of data which may
              include an anonymous unique identifier.
            </Typography>
            <Typography paragraph>
              You can instruct your browser to refuse all cookies or to indicate when a cookie is
              being sent. However, if you do not accept cookies, you may not be able to use some
              portions of our platform.
            </Typography>
          </Section>

          <Section>
            <SectionTitle variant="h5" component="h2">
              Data Security
            </SectionTitle>
            <Typography paragraph>
              We have implemented measures designed to secure your personal information from
              accidental loss and from unauthorized access, use, alteration, and disclosure. All
              information you provide to us is stored on secure servers behind firewalls.
            </Typography>
            <Typography paragraph>
              The safety and security of your information also depends on you. We urge you to be
              careful about sharing your password or other authentication information with anyone.
            </Typography>
          </Section>

          <Section>
            <SectionTitle variant="h5" component="h2">
              Changes to Our Privacy Policy
            </SectionTitle>
            <Typography paragraph>
              We may update our Privacy Policy from time to time. We will notify you of any changes
              by posting the new Privacy Policy on this page and updating the "Last Updated" date.
            </Typography>
            <Typography paragraph>
              You are advised to review this Privacy Policy periodically for any changes. Changes to
              this Privacy Policy are effective when they are posted on this page.
            </Typography>
          </Section>

          <Section>
            <SectionTitle variant="h5" component="h2">
              Contact Us
            </SectionTitle>
            <Typography paragraph>
              If you have any questions about this Privacy Policy, please contact us at:
            </Typography>
            <Typography paragraph>
              <strong>Email:</strong> privacy@nexora.com
              <br />
              <strong>Address:</strong> 123 Tech Street, Suite 100, San Francisco, CA 94105
            </Typography>
          </Section>

          <Box mt={4}>
            <Typography variant="body2" color="textSecondary">
              Last Updated: June 17, 2025
            </Typography>
          </Box>
        </StyledPaper>
      </Box>
    </Container>
  );
};

export default PrivacyPolicy;
