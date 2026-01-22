using ExtraTime.Application.Features.Auth;
using ExtraTime.Application.Features.Auth.Commands.Login;
using ExtraTime.Application.Features.Auth.Commands.RefreshToken;
using ExtraTime.Application.Features.Auth.Commands.Register;
using ExtraTime.Application.Features.Auth.DTOs;
using ExtraTime.Application.Features.Auth.Queries.GetCurrentUser;
using FluentValidation;
using Mediator;

namespace ExtraTime.API.Features.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .WithOpenApi();

        group.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .AllowAnonymous();

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .AllowAnonymous();

        group.MapPost("/refresh", RefreshTokenAsync)
            .WithName("RefreshToken")
            .AllowAnonymous();

        group.MapGet("/me", GetCurrentUserAsync)
            .WithName("GetCurrentUser")
            .RequireAuthorization();
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        IMediator mediator,
        IValidator<RegisterCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(request.Email, request.Username, request.Password);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == AuthErrors.EmailAlreadyExists ||
                result.Error == AuthErrors.UsernameAlreadyExists)
            {
                return Results.Conflict(new { error = result.Error });
            }
            return Results.BadRequest(new { error = result.Error });
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IMediator mediator,
        IValidator<LoginCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == AuthErrors.InvalidCredentials)
            {
                return Results.Unauthorized();
            }
            return Results.BadRequest(new { error = result.Error });
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> RefreshTokenAsync(
        RefreshTokenRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand(request.RefreshToken);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == AuthErrors.InvalidRefreshToken ||
                result.Error == AuthErrors.TokenReused)
            {
                return Results.Unauthorized();
            }
            return Results.BadRequest(new { error = result.Error });
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetCurrentUserAsync(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetCurrentUserQuery();

        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return Results.NotFound(new { error = result.Error });
        }

        return Results.Ok(result.Value);
    }
}
