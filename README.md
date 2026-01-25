# ExtraTime

A social betting app where friends create leagues, predict football match outcomes, and compete for points. No real money involved - just bragging rights!

## Features

### Implemented

- **User Authentication** - Register, login with JWT tokens, automatic token refresh with rotation
- **Role-Based Authorization** - User and Admin roles with policy-based access control
- **Background Job Tracking** - Admin dashboard for monitoring async jobs with retry/cancel capabilities
- **Modern Frontend** - Next.js 16 with React 19, TypeScript, and Tailwind CSS v4
- **API Documentation** - Swagger/OpenAPI with JWT security
- **Football Data Integration** - Live match data from Football-Data.org API
- **League System** - Create private leagues, invite friends with unique codes
- **Betting System** - Predict match scores, earn points (exact score: 3pts, correct result: 1pt)
- **Leaderboards** - Track rankings within leagues

### Planned

- **Gamification** - Achievements, streaks, levels, and celebrations
- **Bot Players** - AI opponents with different strategies to keep leagues active

## Tech Stack

### Backend
- **ASP.NET Core** (.NET 10) with Clean Architecture
- **Entity Framework Core** with PostgreSQL
- **Mediator** (source generator) for CQRS pattern
- **FluentValidation** for request validation
- **JWT Authentication** with BCrypt password hashing

### Frontend
- **Next.js 16** (App Router, React 19)
- **TypeScript**
- **TanStack Query** for server state
- **Zustand** for client state
- **Tailwind CSS** + **shadcn/ui**
- **Framer Motion** for animations

### Infrastructure
- **Docker Compose** for local development
- **PostgreSQL** database
- **GitHub Actions** CI/CD

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later
- [Node.js 22](https://nodejs.org/) or [Bun](https://bun.sh/)
- [Docker](https://www.docker.com/) and Docker Compose

### Quick Start with Docker

```bash
# Clone the repository
git clone https://github.com/yourusername/ExtraTime.git
cd ExtraTime

# Start all services
docker-compose up --build
```

Services will be available at:
- **Frontend**: http://localhost:3000
- **API**: http://localhost:5000
- **Swagger**: http://localhost:5000/swagger

### Manual Setup

#### Backend

```bash
# Start PostgreSQL (or use docker-compose up db)
docker-compose up db -d

# Restore packages
dotnet restore

# Apply migrations
dotnet ef database update --project src/ExtraTime.Infrastructure --startup-project src/ExtraTime.API

# Run the API
dotnet run --project src/ExtraTime.API
```

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
│   └── ExtraTime.API/             # Minimal APIs, Middleware
├── tests/
│   └── ExtraTime.API.Tests/       # Integration tests
├── web/                           # Next.js frontend
│   ├── src/
│   │   ├── app/                   # Pages and layouts
│   │   ├── components/            # React components
│   │   ├── hooks/                 # Custom hooks
│   │   ├── lib/                   # Utilities and API client
│   │   ├── stores/                # Zustand stores
│   │   └── types/                 # TypeScript types
│   └── public/                    # Static assets
├── docker-compose.yml             # Local development setup
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

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=extratime;Username=extratime;Password=extratime_dev"
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
NEXT_PUBLIC_API_URL=http://localhost:5000/api
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
