namespace ExtraTime.Application.Features.Admin;

public static class AdminErrors
{
    public const string JobNotFound = "Job not found.";
    public const string JobCannotBeRetried = "Job cannot be retried. Only failed jobs can be retried.";
    public const string JobCannotBeCancelled = "Job cannot be cancelled. Only pending or processing jobs can be cancelled.";
    public const string MaxRetriesExceeded = "Maximum retry attempts exceeded.";
}
