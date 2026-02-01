# Scalar API Integration Plan

## Overview
Replace Swagger UI with Scalar API documentation in the ExtraTime.API project. Scalar will be available **only in Development** environment at `/scalar/v1`.

## Prerequisites
- Project uses .NET 10
- Currently using Swashbuckle.AspNetCore for Swagger
- OpenAPI is already configured
- Endpoints use `.WithName()` and `.WithTags()` for documentation

## Implementation Steps

### 1. Update Package References

**File**: `src/ExtraTime.API/ExtraTime.API.csproj`

**Changes**:
- Remove `Swashbuckle.AspNetCore` package
- Add `Scalar.AspNetCore` package (latest stable version)

```xml
<!-- REMOVE -->
<PackageReference Include="Swashbuckle.AspNetCore" />

<!-- ADD -->
<PackageReference Include="Scalar.AspNetCore" Version="2.1.18" />
```

> **Note**: Keep `Microsoft.AspNetCore.OpenApi` as it's required for OpenAPI document generation.

### 2. Update Program.cs - Remove Swagger Configuration

**File**: `src/ExtraTime.API/Program.cs`

**Remove**: Swagger/Swashbuckle configuration (lines ~60-87)

```csharp
// REMOVE THIS ENTIRE BLOCK:
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ExtraTime API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            []
        }
    });
});
```

**Remove**: Swagger UI middleware (lines ~93-97)

```csharp
// REMOVE THIS BLOCK:
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

### 3. Update Program.cs - Add Scalar Configuration

**File**: `src/ExtraTime.API/Program.cs`

**Add**: After `builder.AddServiceDefaults()` (around line 29)

```csharp
// Add OpenAPI document generation (required for Scalar)
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "ExtraTime API",
            Version = "v1",
            Description = "ExtraTime Football Betting API"
        };
        return Task.CompletedTask;
    });

    // Add JWT Bearer authentication support
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT token"
            }
        };

        document.SecurityRequirements = new List<OpenApiSecurityRequirement>
        {
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme { Reference = new OpenApiReference { Id = "Bearer", Type = ReferenceType.SecurityScheme } }] = Array.Empty<string>()
            }
        };
        return Task.CompletedTask;
    });
});
```

**Add**: In development middleware section (replacing the Swagger block)

```csharp
// Development-only API documentation
if (app.Environment.IsDevelopment())
{
    // Map OpenAPI document endpoint (required for Scalar)
    app.MapOpenApi();
    
    // Configure Scalar UI
    app.MapScalarApiReference(options =>
    {
        options.Title = "ExtraTime API";
        options.OpenApiRoute = "/openapi/v1.json";
        
        // Optional: Configure additional settings
        options.Theme = ScalarTheme.Mars;
        options.DefaultHttpClient = new KeyValuePair<string, string>("http", "http://localhost:5000");
        options.HideModels = false;
        options.HideDownloadButton = false;
    });
}
```

### 4. Update Usings

**File**: `src/ExtraTime.API/Program.cs`

**Remove** (if not used elsewhere):
```csharp
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
```

**Add**:
```csharp
using Scalar.AspNetCore;
```

### 5. Verify Endpoint Documentation

Scalar automatically picks up:
- `.WithName()` - Sets operation names
- `.WithTags()` - Groups endpoints by tags
- `[ProducesResponseType]` - Response types (if used)
- `[EndpointDescription]` - Descriptions (if used)

**No changes required** to existing endpoints as they already follow best practices.

### 6. Testing & Verification

**Steps**:
1. Run the API project in Development mode
2. Navigate to `http://localhost:5000/scalar/v1`
3. Verify all endpoints are visible
4. Test authentication flow:
   - Click "Authentication" button in Scalar UI
   - Enter JWT token
   - Verify authenticated endpoints work

### 7. Post-Implementation Cleanup

After successful implementation:
- [ ] Verify no Swagger references remain in code
- [ ] Test all API endpoints through Scalar UI
- [ ] Verify JWT authentication works in Scalar
- [ ] Update any documentation mentioning Swagger
- [ ] Inform team about the change

## Summary of Changes

| Aspect | Before | After |
|--------|--------|-------|
| Documentation Tool | Swagger UI | Scalar |
| Route | `/swagger` | `/scalar/v1` |
| Package | `Swashbuckle.AspNetCore` | `Scalar.AspNetCore` |
| Environment | Development only | Development only |
| OpenAPI Document | Generated by Swagger | Generated by `AddOpenApi()` |

## Access URLs

- **Development**: `http://localhost:5000/scalar/v1`
- **Production**: Not available (intentionally)

## References

- [Scalar.AspNetCore Documentation](https://github.com/scalar/scalar/blob/main/documentation/integrations/dotnet.md)
- [ASP.NET Core OpenAPI](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi)
