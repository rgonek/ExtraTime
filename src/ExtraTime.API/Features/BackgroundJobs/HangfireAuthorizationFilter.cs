using Hangfire.Dashboard;

namespace ExtraTime.API.Features.BackgroundJobs;

public sealed class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // In production, check for admin role
        if (httpContext.RequestServices
            .GetRequiredService<IWebHostEnvironment>()
            .IsProduction())
        {
            return httpContext.User.IsInRole("Admin");
        }

        // Allow all in development
        return true;
    }
}
