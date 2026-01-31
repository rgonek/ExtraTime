using ExtraTime.Application.Common.Interfaces;

namespace ExtraTime.Infrastructure.Services;

/// <summary>
/// User service for background job contexts where there's no HTTP request.
/// Used by Azure Functions and other background processes.
/// </summary>
public sealed class BackgroundUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public string? Role => "System";
    public bool IsAuthenticated => false;
    public bool IsAdmin => true; // System operations have admin privileges
}
