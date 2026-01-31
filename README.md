# ExtraTime

A social betting app where friends create leagues, predict football match outcomes, and compete for points. No real money involved - just bragging rights!

## Features

### Implemented

- **User Authentication** - Register, login with JWT tokens, automatic token refresh with rotation
- **Role-Based Authorization** - User and Admin roles with policy-based access control
- **Background Job Tracking** - Admin dashboard for monitoring async jobs (Azure Functions)
- **Modern Frontend** - Next.js 16 with React 19, TypeScript, and Tailwind CSS v4
- **API Documentation** - Swagger/OpenAPI with JWT security
- **Football Data Integration** - Live match data from Football-Data.org API
- **League System** - Create private leagues, invite friends with unique codes
- **Betting System** - Predict match scores, earn points (exact score: 3pts, correct result: 1pt)
- **Leaderboards** - Track rankings within leagues
- **Bot Players** - AI opponents with different strategies to keep leagues active (Random, Stats-based)
- **Gamification** - Achievements, streaks, levels, and celebrations
- **Design System** - Vibrant sports-themed UI with full dark mode support

### Planned

- **Extended Data** - Standings, top scorers, and match lineups
- **External Sources** - xG statistics, betting odds, and injury data
- **FastEndpoints** - Migration to a more streamlined API framework
- **Mobile App** - Dedicated mobile experience

## Tech Stack

### Backend
- **ASP.NET Core** (.NET 10) with Clean Architecture
- **Entity Framework Core** with SQL Server
- **Mediator** (source generator) for CQRS pattern
- **FluentValidation** for request validation
- **JWT Authentication** with BCrypt password hashing
- **Azure Functions** for serverless background jobs

### Frontend
- **Next.js 16** (App Router, React 19)
- **TypeScript**
- **TanStack Query** for server state
- **Zustand** for client state
- **Tailwind CSS** + **shadcn/ui**
- **Framer Motion** for animations

### Infrastructure
- **.NET Aspire** for local orchestration and observability
- **Docker Compose** for containerized development
- **SQL Server** database (Azure SQL for production)
- **Azure Static Web Apps** for frontend hosting
- **Azure App Service** for backend API
- **GitHub Actions** CI/CD

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later
- [Node.js 22](https://nodejs.org/) or [Bun](https://bun.sh/)
- [Docker](https://www.docker.com/) (for SQL Server container)

### Quick Start with .NET Aspire (Recommended)

```bash
# Clone the repository
git clone https://github.com/yourusername/ExtraTime.git
cd ExtraTime

# Start with Aspire
dotnet run --project src/ExtraTime.AppHost
```

This starts all services with the Aspire dashboard for observability:
- **Aspire Dashboard**: https://localhost:15170 (logs, traces, metrics)
- **Frontend**: http://localhost:3000
- **API**: https://localhost:5001
- **Swagger**: https://localhost:5001/swagger

### Alternative: Docker Compose

```bash
# Start all services in containers
docker-compose up --build
```

Services will be available at:
- **Frontend**: http://localhost:3000
- **API**: http://localhost:5200
- **Swagger**: http://localhost:5200/swagger

### Manual Setup

#### Backend

```bash
# Option 1: Use Aspire (handles SQL Server automatically)
dotnet run --project src/ExtraTime.AppHost

# Option 2: Run API standalone
# Start SQL Server first
docker-compose up db -d

# Apply migrations
dotnet ef database update --project src/ExtraTime.Infrastructure --startup-project src/ExtraTime.API

# Run the API
dotnet run --project src/ExtraTime.API
```

#### Database Management

For local development, you can connect to SQL Server using:
- **Azure Data Studio** (recommended)
- **SQL Server Management Studio (SSMS)**
- **VS Code with SQL Server extension**

Connection details:
- **Server**: localhost,1433
- **Username**: sa
- **Password**: ExtraTime_Dev123!
- **Database**: extratime

#### Frontend

```bash
cd web

# Install dependencies
npm install
# or with Bun
bun install

# Start development server
npm run dev
# or
bun dev
```

## Project Structure

```
ExtraTime/
├── src/
│   ├── ExtraTime.Domain/          # Entities, Enums, Value Objects
│   ├── ExtraTime.Application/     # Use Cases, DTOs, Interfaces
│   ├── ExtraTime.Infrastructure/  # EF Core, Services, External APIs
│   ├── ExtraTime.API/             # Minimal APIs, Middleware
│   ├── ExtraTime.AppHost/         # .NET Aspire orchestrator
│   └── ExtraTime.ServiceDefaults/ # Shared Aspire configuration
├── tests/
│   ├── ExtraTime.API.Tests/       # API integration tests
│   ├── ExtraTime.IntegrationTests/# Database integration tests
│   ├── ExtraTime.UnitTests/       # Unit tests
│   └── ExtraTime.Domain.Tests/    # Domain logic tests
├── web/                           # Next.js frontend
│   ├── src/
│   │   ├── app/                   # Pages and layouts
│   │   ├── components/            # React components
│   │   ├── hooks/                 # Custom hooks
│   │   ├── lib/                   # Utilities and API client
│   │   ├── stores/                # Zustand stores
│   │   └── types/                 # TypeScript types
│   └── public/                    # Static assets
├── docker-compose.yml             # Containerized development
└── ExtraTime.sln                  # Solution file
```

## API Endpoints

### Authentication

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/register` | Create a new account |
| POST | `/api/auth/login` | Login and get tokens |
| POST | `/api/auth/refresh` | Refresh access token |
| GET | `/api/auth/me` | Get current user (requires auth) |

### Admin (requires Admin role)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/admin/jobs` | List background jobs |
| GET | `/api/admin/jobs/stats` | Job statistics |
| GET | `/api/admin/jobs/{id}` | Get job details |
| POST | `/api/admin/jobs/{id}/retry` | Retry failed job |
| POST | `/api/admin/jobs/{id}/cancel` | Cancel pending job |

### Health

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/health` | API health check |
| GET | `/health` | Database health check |

## Environment Variables

### Backend (appsettings.json)

> **Note**: When running with Aspire, connection strings are injected automatically.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=extratime;User Id=sa;Password=ExtraTime_Dev123!;TrustServerCertificate=True"
  },
  "Jwt": {
    "Secret": "your-secret-key-minimum-32-characters",
    "Issuer": "ExtraTime",
    "Audience": "ExtraTime",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

### Frontend (.env.local)

```env
NEXT_PUBLIC_API_URL=http://localhost:5200/api
```

## Development

### Running Tests

```bash
# Backend tests
dotnet test

# Frontend lint
cd web && npm run lint
```

### Creating Migrations

```bash
dotnet ef migrations add MigrationName \
  --project src/ExtraTime.Infrastructure \
  --startup-project src/ExtraTime.API
```

### Code Standards

- **Backend**: Sealed classes by default, primary constructors, minimal APIs, file-scoped namespaces
- **Frontend**: Functional components, TypeScript strict mode, Tailwind CSS

## Supported Competitions (Planned)

- Premier League (England)
- La Liga (Spain)
- Bundesliga (Germany)
- Serie A (Italy)
- Ligue 1 (France)

---

Built with modern technologies and clean architecture principles.
