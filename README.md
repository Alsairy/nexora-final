# Nexora Platform

[![Build Status](https://img.shields.io/github/workflow/status/nexora/nexora-platform/CI-CD?style=flat-square)](https://github.com/nexora/nexora-platform/actions)
[![Test Coverage](https://img.shields.io/codecov/c/github/nexora/nexora-platform?style=flat-square)](https://codecov.io/gh/nexora/nexora-platform)
[![Lighthouse Score](https://img.shields.io/badge/lighthouse-90%2B-brightgreen?style=flat-square)](https://github.com/nexora/nexora-platform/actions)
[![License](https://img.shields.io/badge/license-MIT-blue?style=flat-square)](LICENSE)

A comprehensive enterprise-grade fintech platform with payment processing, SMS gateway, e-wallet, API gateway, e-signature, WhatsApp chatbot, and loan marketplace capabilities.

## Features

- **Payment Gateway**: Process payments with multiple providers
- **SMS Gateway**: Send and receive SMS messages
- **E-Wallet**: Manage digital wallets and transactions
- **API Gateway**: Secure API management and monitoring
- **E-Signature**: Digital document signing
- **WhatsApp Chatbot**: Automated customer interactions
- **Loan Marketplace**: Connect borrowers with lenders

## Architecture

The Nexora Platform is built with a modern, scalable architecture:

- **Backend**: ASP.NET Core 8 with CQRS and Event Sourcing
- **Frontend**: React with TypeScript and Material-UI
- **Database**: PostgreSQL with Entity Framework Core
- **Caching**: Redis with 30-minute default TTL
- **Authentication**: JWT with key rotation
- **Multi-tenancy**: Support for subdomain and header-based tenant resolution
- **Containerization**: Docker with non-root users and security contexts
- **Orchestration**: Kubernetes with resource limits and security policies

## Getting Started

### Prerequisites

- .NET 8 SDK
- Node.js 20+
- Docker and Docker Compose
- PostgreSQL 15+
- Redis 7+

### Development Setup

1. Clone the repository:

```bash
git clone https://github.com/nexora/nexora-platform.git
cd nexora-platform
```

2. Set up environment variables:

```bash
cp .env.example .env
# Edit .env with your configuration
```

3. Start the development environment:

```bash
docker-compose up -d
```

4. Run the backend:

```bash
cd backend/src/Nexora.Web.Api
dotnet run
```

5. Run the frontend:

```bash
cd nexora-frontend
pnpm install
pnpm dev
```

### Using the Dev Container

For a consistent development environment, use the provided Dev Container:

1. Install the VS Code Remote - Containers extension
2. Open the project in VS Code
3. Click "Reopen in Container" when prompted
4. Wait for the container to build and initialize

## Configuration

### Route Constants

Always use the route constants from `ROUTES` enum instead of hardcoding paths:

```typescript
import ROUTES from '@/routes';

// Good
navigate(ROUTES.DASHBOARD);

// Bad
navigate('/dashboard');
```

### Environment Variables

The platform uses environment variables for configuration. See `.env.example` for available options.

Example placeholder values:

```
DB_CONNECTION_STRING=Host=localhost;Database=nexora;Username=postgres;Password=postgres123
REDIS_CONNECTION_STRING=localhost:6379,password=redis123
JWT_SECRET=your-jwt-secret-key-here
```

## Testing

### Backend Tests

```bash
cd backend
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

### Frontend Tests

```bash
cd nexora-frontend
pnpm test
```

## Deployment

### Docker

Build and run the Docker containers:

```bash
docker-compose -f docker-compose.prod.yml up -d
```

### Kubernetes

Deploy to Kubernetes:

```bash
kubectl apply -f k8s/
```

## Security Features

- Non-root container execution
- Read-only root filesystem
- Resource limits and quotas
- Secrets management with ExternalSecrets
- Key rotation for cryptographic keys
- Global soft-delete filter
- Idempotency key support for payments

## Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-feature`
3. Commit your changes: `git commit -am 'Add my feature'`
4. Push to the branch: `git push origin feature/my-feature`
5. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core)
- [React](https://reactjs.org/)
- [Material-UI](https://mui.com/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [Docker](https://www.docker.com/)
- [Kubernetes](https://kubernetes.io/)

