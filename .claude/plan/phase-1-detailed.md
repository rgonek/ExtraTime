# Phase 1: Project Foundation - Detailed Implementation Plan

## Overview
This plan sets up the complete development environment for ExtraTime with ASP.NET Core backend and Next.js 15 frontend.

> **Note:** If .NET 10 is not installed, you can use .NET 9 by changing target frameworks to `net9.0` and EF Core packages to version `9.0.x`.

---

## Part 1: Backend Setup (ASP.NET Core Clean Architecture)

### 1.1 Solution Structure

```
D:\Dev\ExtraTime\
├── src/
│   ├── ExtraTime.Domain/           # Entities, Value Objects, Enums
│   │   ├── Common/
│   │   │   └── BaseEntity.cs
│   │   ├── Entities/
│   │   ├── Enums/
│   │   ├── Exceptions/
│   │   └── ValueObjects/
│   │
│   ├── ExtraTime.Application/      # Use Cases, DTOs, Interfaces
│   │   ├── Common/
│   │   │   ├── Behaviors/
│   │   │   ├── Interfaces/
│   │   │   └── Mappings/
│   │   └── DependencyInjection.cs
│   │
│   ├── ExtraTime.Infrastructure/   # EF Core, External APIs
│   │   ├── Data/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── Configurations/
│   │   │   └── Migrations/
│   │   ├── Services/
│   │   └── DependencyInjection.cs
│   │
│   └── ExtraTime.API/              # Controllers, Middleware
│       ├── Controllers/
│       ├── Middleware/
│       ├── Program.cs
│       └── Dockerfile
│
├── tests/
│   └── ExtraTime.API.Tests/
│
├── web/                            # Next.js frontend
├── docker-compose.yml
├── global.json
└── ExtraTime.sln
```

### 1.2 Scaffolding Commands

Execute from `D:\Dev\ExtraTime`:

```powershell
# Create solution
dotnet new sln -n ExtraTime

# Create projects
mkdir src
dotnet new classlib -n ExtraTime.Domain -o src/ExtraTime.Domain
dotnet new classlib -n ExtraTime.Application -o src/ExtraTime.Application
dotnet new classlib -n ExtraTime.Infrastructure -o src/ExtraTime.Infrastructure
dotnet new webapi -n ExtraTime.API -o src/ExtraTime.API --use-controllers

# Remove default Class1.cs files
Remove-Item src/ExtraTime.Domain/Class1.cs
Remove-Item src/ExtraTime.Application/Class1.cs
Remove-Item src/ExtraTime.Infrastructure/Class1.cs

# Add projects to solution
dotnet sln add src/ExtraTime.Domain/ExtraTime.Domain.csproj
dotnet sln add src/ExtraTime.Application/ExtraTime.Application.csproj
dotnet sln add src/ExtraTime.Infrastructure/ExtraTime.Infrastructure.csproj
dotnet sln add src/ExtraTime.API/ExtraTime.API.csproj

# Add project references
dotnet add src/ExtraTime.Application reference src/ExtraTime.Domain
dotnet add src/ExtraTime.Infrastructure reference src/ExtraTime.Application
dotnet add src/ExtraTime.API reference src/ExtraTime.Infrastructure

# Add NuGet packages - Application
dotnet add src/ExtraTime.Application package Mediator.Abstractions
dotnet add src/ExtraTime.Application package Mediator.SourceGenerator
dotnet add src/ExtraTime.Application package FluentValidation
dotnet add src/ExtraTime.Application package FluentValidation.DependencyInjectionExtensions

# Add NuGet packages - Infrastructure
dotnet add src/ExtraTime.Infrastructure package Microsoft.EntityFrameworkCore
dotnet add src/ExtraTime.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/ExtraTime.Infrastructure package Microsoft.EntityFrameworkCore.Design

# Add NuGet packages - API
dotnet add src/ExtraTime.API package Serilog.AspNetCore
dotnet add src/ExtraTime.API package Serilog.Sinks.Console

# Create test project
mkdir tests
dotnet new xunit -n ExtraTime.API.Tests -o tests/ExtraTime.API.Tests
dotnet sln add tests/ExtraTime.API.Tests/ExtraTime.API.Tests.csproj
dotnet add tests/ExtraTime.API.Tests reference src/ExtraTime.API

# Create folder structure
mkdir src/ExtraTime.Domain/Common
mkdir src/ExtraTime.Domain/Entities
mkdir src/ExtraTime.Domain/Enums
mkdir src/ExtraTime.Domain/Exceptions
mkdir src/ExtraTime.Domain/ValueObjects
mkdir src/ExtraTime.Application/Common
mkdir src/ExtraTime.Application/Common/Behaviors
mkdir src/ExtraTime.Application/Common/Interfaces
mkdir src/ExtraTime.Application/Common/Mappings
mkdir src/ExtraTime.Infrastructure/Data
mkdir src/ExtraTime.Infrastructure/Data/Configurations
mkdir src/ExtraTime.Infrastructure/Data/Migrations
mkdir src/ExtraTime.Infrastructure/Services
mkdir src/ExtraTime.API/Controllers
mkdir src/ExtraTime.API/Middleware
```

### 1.3 Key Backend Files

**global.json** (root):
```json
{
  "sdk": {
    "version": "10.0.0",
    "rollForward": "latestMinor"
  }
}
```

**src/ExtraTime.Domain/Common/BaseEntity.cs**:
```csharp
namespace ExtraTime.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
}

public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
```

**src/ExtraTime.Application/Common/Interfaces/IApplicationDbContext.cs**:
```csharp
namespace ExtraTime.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
```

**src/ExtraTime.Application/DependencyInjection.cs**:
```csharp
using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ExtraTime.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
        });
        return services;
    }
}
```

**src/ExtraTime.Infrastructure/Data/ApplicationDbContext.cs**:
```csharp
using ExtraTime.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
```

**src/ExtraTime.Infrastructure/DependencyInjection.cs**:
```csharp
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExtraTime.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());
        return services;
    }
}
```

**src/ExtraTime.API/Program.cs**:
```csharp
using ExtraTime.Application;
using ExtraTime.Infrastructure;
using ExtraTime.Infrastructure.Data;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

**src/ExtraTime.API/Controllers/HealthController.cs**:
```csharp
using Microsoft.AspNetCore.Mvc;

namespace ExtraTime.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        Status = "Healthy",
        Timestamp = DateTime.UtcNow,
        Version = "1.0.0"
    });
}
```

**src/ExtraTime.API/appsettings.json**:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=extratime;Username=extratime;Password=extratime_dev"
  }
}
```

---

## Part 2: Frontend Setup (Next.js 15)

### 2.1 Frontend Structure

```
D:\Dev\ExtraTime\web\
├── src/
│   ├── app/
│   │   ├── layout.tsx
│   │   ├── page.tsx
│   │   ├── globals.css
│   │   └── providers.tsx
│   ├── components/
│   │   ├── ui/               # shadcn/ui components
│   │   └── common/
│   ├── hooks/
│   ├── lib/
│   │   ├── api-client.ts
│   │   └── utils.ts
│   ├── stores/
│   │   ├── index.ts
│   │   └── auth-store.ts
│   └── types/
│       └── index.ts
├── public/
├── package.json
├── next.config.ts
├── tailwind.config.ts
└── Dockerfile
```

### 2.2 Scaffolding Commands

```powershell
# Create Next.js project
npx create-next-app@latest web --typescript --tailwind --eslint --app --src-dir --import-alias "@/*" --turbopack

cd web

# Install dependencies
npm install @tanstack/react-query @tanstack/react-query-devtools
npm install zustand
npm install framer-motion
npm install clsx tailwind-merge class-variance-authority lucide-react
npm install -D prettier prettier-plugin-tailwindcss

# Initialize shadcn/ui
npx shadcn@latest init
# Select: Default style, Slate color, Yes to CSS variables

# Add base components
npx shadcn@latest add button card input toast

# Create folders
mkdir src/stores src/hooks src/types src/components/common

cd ..
```

### 2.3 Key Frontend Files

**web/src/app/providers.tsx**:
```tsx
'use client';

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { useState, type ReactNode } from 'react';

export function Providers({ children }: { children: ReactNode }) {
  const [queryClient] = useState(
    () => new QueryClient({
      defaultOptions: {
        queries: {
          staleTime: 60 * 1000,
          refetchOnWindowFocus: false,
        },
      },
    })
  );

  return (
    <QueryClientProvider client={queryClient}>
      {children}
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  );
}
```

**web/src/app/layout.tsx**:
```tsx
import type { Metadata } from 'next';
import { Inter } from 'next/font/google';
import './globals.css';
import { Providers } from './providers';

const inter = Inter({ subsets: ['latin'] });

export const metadata: Metadata = {
  title: 'ExtraTime - Social Betting with Friends',
  description: 'Create leagues, predict football matches, and compete with friends.',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body className={inter.className}>
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}
```

**web/src/lib/api-client.ts**:
```typescript
const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api';

class ApiClient {
  private baseUrl: string;

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl;
  }

  private async request<T>(endpoint: string, config: RequestInit = {}): Promise<T> {
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
      ...config.headers,
    };

    const token = typeof window !== 'undefined' ? localStorage.getItem('token') : null;
    if (token) {
      (headers as Record<string, string>)['Authorization'] = `Bearer ${token}`;
    }

    const response = await fetch(`${this.baseUrl}${endpoint}`, { ...config, headers });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'An error occurred' }));
      throw new Error(error.message);
    }

    const text = await response.text();
    return text ? JSON.parse(text) : null;
  }

  get<T>(endpoint: string) { return this.request<T>(endpoint, { method: 'GET' }); }
  post<T>(endpoint: string, data?: unknown) {
    return this.request<T>(endpoint, { method: 'POST', body: data ? JSON.stringify(data) : undefined });
  }
  put<T>(endpoint: string, data?: unknown) {
    return this.request<T>(endpoint, { method: 'PUT', body: data ? JSON.stringify(data) : undefined });
  }
  delete<T>(endpoint: string) { return this.request<T>(endpoint, { method: 'DELETE' }); }
}

export const apiClient = new ApiClient(API_BASE_URL);
```

**web/src/stores/auth-store.ts**:
```typescript
import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface User {
  id: string;
  email: string;
  username: string;
}

interface AuthState {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  setAuth: (user: User, token: string) => void;
  clearAuth: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      token: null,
      isAuthenticated: false,
      setAuth: (user, token) => set({ user, token, isAuthenticated: true }),
      clearAuth: () => set({ user: null, token: null, isAuthenticated: false }),
    }),
    { name: 'auth-storage' }
  )
);
```

**web/.prettierrc**:
```json
{
  "semi": true,
  "singleQuote": true,
  "tabWidth": 2,
  "trailingComma": "es5",
  "printWidth": 100,
  "plugins": ["prettier-plugin-tailwindcss"]
}
```

---

## Part 3: Docker Compose

**docker-compose.yml** (root):
```yaml
services:
  db:
    image: postgres:16-alpine
    container_name: extratime-db
    environment:
      POSTGRES_USER: extratime
      POSTGRES_PASSWORD: extratime_dev
      POSTGRES_DB: extratime
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U extratime -d extratime"]
      interval: 10s
      timeout: 5s
      retries: 5

  api:
    build:
      context: .
      dockerfile: src/ExtraTime.API/Dockerfile
    container_name: extratime-api
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=extratime;Username=extratime;Password=extratime_dev
    depends_on:
      db:
        condition: service_healthy

  web:
    build:
      context: ./web
      dockerfile: Dockerfile
    container_name: extratime-web
    ports:
      - "3000:3000"
    environment:
      - NEXT_PUBLIC_API_URL=http://localhost:5000/api
    depends_on:
      - api

volumes:
  postgres_data:
```

---

## Part 4: GitHub Actions CI

**.github/workflows/ci.yml**:
```yaml
name: CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]

jobs:
  backend:
    name: Backend Build & Test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet restore
      - run: dotnet build --no-restore --configuration Release
      - run: dotnet test --no-build --configuration Release

  frontend:
    name: Frontend Build & Lint
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: ./web
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: '22'
          cache: 'npm'
          cache-dependency-path: './web/package-lock.json'
      - run: npm ci
      - run: npm run lint
      - run: npm run build
        env:
          NEXT_PUBLIC_API_URL: http://localhost:5000/api
```

---

## Implementation Checklist

### Phase 1A: Backend (execute in order)
- [ ] Create solution and projects (Section 1.2 commands)
- [ ] Add NuGet packages
- [ ] Create folder structure
- [ ] Implement BaseEntity.cs
- [ ] Implement IApplicationDbContext.cs
- [ ] Implement Application DependencyInjection.cs
- [ ] Implement ApplicationDbContext.cs
- [ ] Implement Infrastructure DependencyInjection.cs
- [ ] Update Program.cs
- [ ] Create HealthController.cs
- [ ] Configure appsettings.json
- [ ] Verify: `dotnet build`

### Phase 1B: Frontend (execute in order)
- [ ] Create Next.js project
- [ ] Install npm dependencies
- [ ] Initialize shadcn/ui
- [ ] Create providers.tsx
- [ ] Update layout.tsx
- [ ] Create api-client.ts
- [ ] Create auth-store.ts
- [ ] Configure .prettierrc
- [ ] Verify: `npm run build`

### Phase 1C: DevOps
- [ ] Create docker-compose.yml
- [ ] Create .github/workflows/ci.yml
- [ ] Create .gitignore
- [ ] Test: `docker-compose up --build`
- [ ] Verify health: `curl http://localhost:5000/api/health`
- [ ] Verify frontend: `http://localhost:3000`
- [ ] Commit and push - verify CI passes
