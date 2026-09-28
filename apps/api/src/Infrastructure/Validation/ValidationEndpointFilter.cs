using System.Text.Json;
using FluentValidation;

namespace Xpeak.Api.Infrastructure.Validation;

/// <summary>
/// Runs the registered <see cref="IValidator{T}"/> against the first
/// argument of the endpoint that matches <typeparamref name="T"/>.
/// Returns HTTP 422 with a structured error list if any rule fails;
/// otherwise passes through to the endpoint handler.
///
/// Errors are shaped as
/// <c>{ "errors": [ { "field": "camelCaseName", "message": "..." } ] }</c>
/// so the mobile client can render field-scoped hints without extra
/// mapping.
/// </summary>
public sealed class ValidationEndpointFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var payload = context.Arguments.OfType<T>().FirstOrDefault();
        if (payload is null)
        {
            return await next(context);
        }

        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is null)
        {
            return await next(context);
        }

        var result = await validator.ValidateAsync(payload, context.HttpContext.RequestAborted);
        if (result.IsValid)
        {
            return await next(context);
        }

        var camel = JsonNamingPolicy.CamelCase;
        var errors = result.Errors
            .Select(e => new
            {
                field = camel.ConvertName(e.PropertyName),
                message = e.ErrorMessage,
            })
            .ToList();

        return Results.UnprocessableEntity(new { errors });
    }
}

public static class ValidationEndpointFilterExtensions
{
    public static RouteHandlerBuilder ValidateBody<T>(this RouteHandlerBuilder builder)
        where T : class =>
        builder.AddEndpointFilter<ValidationEndpointFilter<T>>();
}
