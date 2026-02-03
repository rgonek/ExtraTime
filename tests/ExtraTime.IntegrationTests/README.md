# Integration Tests

This project uses a high-performance architecture designed for dual-mode execution.

## Modes

### 1. InMemory Mode (Fast)
Uses EF Core InMemory provider. Extremely fast setup and execution.
Suitable for logic verification where relational constraints are less critical.
**Execution Time Goal:** < 2 minutes.

**How to run:**
```bash
# Default mode
dotnet test tests/ExtraTime.IntegrationTests/ExtraTime.IntegrationTests.csproj
```

### 2. SQL Server Mode (Full)
Uses Testcontainers with a real SQL Server 2022 instance.
Tests run in **PARALLEL** by creating a unique database for each test on the shared container.
**Execution Time Goal:** < 7 minutes.

**How to run:**
```bash
# Windows (PowerShell)
$env:TEST_MODE='SqlServer'; dotnet test tests/ExtraTime.IntegrationTests/ExtraTime.IntegrationTests.csproj

# Bash
TEST_MODE=SqlServer dotnet test tests/ExtraTime.IntegrationTests/ExtraTime.IntegrationTests.csproj
```

## Architecture

- **Parallelism**: Enabled by default (unlike the old project).
- **Isolation**:
  - InMemory: Unique DB name per test.
  - SqlServer: Single Container (Assembly Scope) -> Unique Database per Test (Test Scope).
- **Factories**: `TestConfig` uses `ITestDatabase` factory to switch implementations.
