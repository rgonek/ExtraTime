# Phase 2: Authentication System - Implementation Plan

## Overview
Custom JWT authentication with BCrypt password hashing, access/refresh token flow, following Clean Architecture patterns established in Phase 1.

## Entities

### User (`src/ExtraTime.Domain/Entities/User.cs`)
```csharp
public sealed class User : BaseAuditableEntity
{
    public required string Email { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
```

### RefreshToken (`src/ExtraTime.Domain/Entities/RefreshToken.cs`)
```csharp
public sealed class RefreshToken : BaseEntity
{
    public required string Token { get; set; }
    public required DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
```

## API Endpoints

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| POST | `/api/auth/register` | Create account | No |
| POST | `/api/auth/login` | Get tokens | No |
| POST | `/api/auth/refresh` | Rotate tokens | No |
| GET | `/api/auth/me` | Current user | Yes |

## Application Layer Structure

```
Features/Auth/
├── AuthErrors.cs
├── DTOs/AuthDtos.cs
├── Commands/
│   ├── Register/
│   │   ├── RegisterCommand.cs
│   │   ├── RegisterCommandHandler.cs
│   │   └── RegisterCommandValidator.cs
│   ├── Login/
│   │   ├── LoginCommand.cs
│   │   ├── LoginCommandHandler.cs
│   │   └── LoginCommandValidator.cs
│   └── RefreshToken/
│       ├── RefreshTokenCommand.cs
│       └── RefreshTokenCommandHandler.cs
└── Queries/
    └── GetCurrentUser/
        ├── GetCurrentUserQuery.cs
        └── GetCurrentUserQueryHandler.cs
```

## Interfaces to Add

- `IPasswordHasher` - Hash/verify passwords (BCrypt)
- `ITokenService` - Generate/validate JWT and refresh tokens
- `ICurrentUserService` - Get current user from HttpContext

## Infrastructure Services

| Service | File | Purpose |
|---------|------|---------|
| PasswordHasher | `Services/PasswordHasher.cs` | BCrypt (work factor 12) |
| TokenService | `Services/TokenService.cs` | JWT generation, refresh token generation |
| CurrentUserService | `Services/CurrentUserService.cs` | Extract user from JWT claims |

## Configuration

**appsettings.json additions:**
```json
{
  "Jwt": {
    "Secret": "minimum-32-character-secret-key-here",
    "Issuer": "ExtraTime",
    "Audience": "ExtraTime",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

## NuGet Packages to Add

- `BCrypt.Net-Next` (Infrastructure)
- `Microsoft.AspNetCore.Authentication.JwtBearer` (Infrastructure + API)

## Implementation Order

1. **Domain** - User and RefreshToken entities
2. **Application Common** - Result pattern, interfaces, update IApplicationDbContext
3. **Application Features** - DTOs, errors, commands/queries with handlers and validators
4. **Infrastructure** - JwtSettings, services, EF configurations, update DI
5. **API** - AuthEndpoints, update Program.cs with auth middleware, update appsettings
6. **Database** - Create and apply migration

## Security Features

- BCrypt password hashing (work factor 12)
- Short-lived access tokens (15 min)
- Refresh token rotation on each use
- Token reuse detection (revokes all user tokens if detected)
- Email normalization (lowercase)

## Validation Rules

**Register:**
- Email: required, valid format, max 256 chars
- Username: 3-50 chars, alphanumeric + underscore only
- Password: 8-128 chars, requires uppercase + lowercase + number

**Login:**
- Email: required, valid format
- Password: required

## Files to Modify (Existing)

- `src/ExtraTime.Application/Common/Interfaces/IApplicationDbContext.cs` - Add DbSets
- `src/ExtraTime.Infrastructure/Data/ApplicationDbContext.cs` - Add DbSets, apply configurations
- `src/ExtraTime.Infrastructure/DependencyInjection.cs` - Register services, JWT auth
- `src/ExtraTime.API/Program.cs` - Add auth middleware, Swagger JWT support
- `src/ExtraTime.API/appsettings.json` - Add JWT settings

## Verification (Backend)

1. Run `dotnet build` - ensure no compilation errors
2. Run `dotnet ef migrations add AddAuthEntities` - create migration
3. Run `dotnet ef database update` - apply migration
4. Start API and test via Swagger:
   - POST `/api/auth/register` with valid payload
   - POST `/api/auth/login` with same credentials
   - POST `/api/auth/refresh` with refresh token
   - GET `/api/auth/me` with Bearer token (Authorization header)
5. Verify validation errors return proper format
6. Verify duplicate email/username returns 409 Conflict
7. Verify invalid credentials returns 401 Unauthorized

---

## Frontend Implementation

### Overview
React-based authentication UI with Zustand for state management, TanStack Query for API calls, and automatic token refresh. Following the playful/gamified design direction from MVP plan.

### File Structure

```
web/src/
├── app/
│   ├── (auth)/
│   │   ├── login/
│   │   │   └── page.tsx
│   │   └── register/
│   │       └── page.tsx
│   ├── (protected)/
│   │   └── dashboard/
│   │       └── page.tsx
│   ├── layout.tsx
│   ├── page.tsx
│   └── providers.tsx
├── components/
│   ├── auth/
│   │   ├── login-form.tsx
│   │   ├── register-form.tsx
│   │   └── protected-route.tsx
│   └── ui/ (shadcn components)
├── hooks/
│   └── use-auth.ts
├── lib/
│   ├── api-client.ts
│   └── utils.ts
├── stores/
│   └── auth-store.ts
└── types/
    └── index.ts
```

### Types (`web/src/types/index.ts`)

```typescript
// User
export interface User {
  id: string;
  email: string;
  username: string;
  role: 'User' | 'Admin';
}

// Auth API Requests
export interface RegisterRequest {
  email: string;
  username: string;
  password: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

// Auth API Responses
export interface AuthResponse {
  user: User;
  accessToken: string;
  refreshToken: string;
}

export interface CurrentUserResponse {
  id: string;
  email: string;
  username: string;
  role: string;
}

// API Error
export interface ApiError {
  message: string;
  statusCode?: number;
  errors?: Record<string, string[]>;
}
```

### Auth Store (`web/src/stores/auth-store.ts`)

```typescript
import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { User } from '@/types';

interface AuthState {
  user: User | null;
  accessToken: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  setAuth: (user: User, accessToken: string, refreshToken: string) => void;
  setTokens: (accessToken: string, refreshToken: string) => void;
  clearAuth: () => void;
  setLoading: (loading: boolean) => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      accessToken: null,
      refreshToken: null,
      isAuthenticated: false,
      isLoading: true,
      setAuth: (user, accessToken, refreshToken) =>
        set({ user, accessToken, refreshToken, isAuthenticated: true, isLoading: false }),
      setTokens: (accessToken, refreshToken) =>
        set({ accessToken, refreshToken }),
      clearAuth: () =>
        set({ user: null, accessToken: null, refreshToken: null, isAuthenticated: false, isLoading: false }),
      setLoading: (isLoading) => set({ isLoading }),
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({
        user: state.user,
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
);
```

### API Client with Token Refresh (`web/src/lib/api-client.ts`)

Features:
- Automatic token refresh on 401 responses
- Request queue during token refresh
- Refresh token rotation support

### Auth Hooks (`web/src/hooks/use-auth.ts`)

```typescript
// TanStack Query hooks for auth operations
export function useLogin() // Returns mutation for login
export function useRegister() // Returns mutation for register
export function useLogout() // Returns mutation for logout
export function useCurrentUser() // Returns query for current user (validates token on app load)
```

### Components

#### Login Form (`web/src/components/auth/login-form.tsx`)
- Email and password inputs
- Form validation with error display
- Loading state during submission
- Link to register page
- Success redirect to dashboard

#### Register Form (`web/src/components/auth/register-form.tsx`)
- Email, username, and password inputs
- Password requirements display
- Form validation with error display
- Loading state during submission
- Link to login page
- Success redirect to login or dashboard

#### Protected Route (`web/src/components/auth/protected-route.tsx`)
- Wraps protected pages
- Redirects to login if not authenticated
- Shows loading state while checking auth
- Optional role-based access (for admin pages)

### Pages

#### Login Page (`web/src/app/(auth)/login/page.tsx`)
- Centered card layout
- App branding/logo
- Login form component
- "Don't have an account?" link

#### Register Page (`web/src/app/(auth)/register/page.tsx`)
- Centered card layout
- App branding/logo
- Register form component
- "Already have an account?" link

#### Dashboard Page (`web/src/app/(protected)/dashboard/page.tsx`)
- Protected route wrapper
- Welcome message with username
- Logout button
- Placeholder for future content

### Design Guidelines

Following MVP design direction:
- **Playful & Gamified**: Subtle animations, friendly colors
- **Card-based layouts**: Use shadcn Card components
- **Feedback**: Toast notifications for success/error
- **Loading states**: Skeleton loaders, button loading states
- **Mobile-first**: Responsive design

### Implementation Order

1. **Types** - Update types with full auth DTOs
2. **Auth Store** - Add refresh token support
3. **API Client** - Add token refresh logic
4. **Auth Hooks** - Create TanStack Query hooks
5. **Components** - Build form components and protected route
6. **Pages** - Create login, register, dashboard pages
7. **Integration** - Wire up providers and navigation

### Validation Rules (Client-side)

Match backend validation:
- **Email**: Required, valid email format
- **Username**: 3-50 chars, alphanumeric + underscore
- **Password**: 8+ chars, uppercase + lowercase + number

### Error Handling

- Display field-level validation errors from API
- Show toast notifications for general errors
- Handle network errors gracefully
- Auto-logout on refresh token failure

### Verification (Frontend)

1. Run `npm run dev` - ensure no compilation errors
2. Test register flow:
   - Navigate to /register
   - Submit invalid data - verify validation errors display
   - Submit valid data - verify redirect and auth state
3. Test login flow:
   - Navigate to /login
   - Submit invalid credentials - verify error display
   - Submit valid credentials - verify redirect and auth state
4. Test protected routes:
   - Access /dashboard without auth - verify redirect to login
   - Access /dashboard with auth - verify page loads
5. Test logout:
   - Click logout - verify redirect and auth state cleared
6. Test token refresh:
   - Wait for access token expiry
   - Make API request - verify token refreshes automatically
7. Test persistence:
   - Refresh browser - verify auth state persists
