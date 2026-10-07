using System.ComponentModel.DataAnnotations;
using ArcaneVault.Server.Dtos;

namespace ArcaneVault.Server.Apis;

internal sealed class ApiValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var details = new List<ApiErrorDetail>();
        foreach (var argument in context.Arguments.Where(argument => argument is not null))
        {
            var validationResults = new List<ValidationResult>();
            Validator.TryValidateObject(argument!, new ValidationContext(argument!), validationResults, validateAllProperties: true);
            details.AddRange(validationResults.SelectMany(result =>
                (result.MemberNames.DefaultIfEmpty(string.Empty)).Select(member => new ApiErrorDetail(member, result.ErrorMessage ?? "is invalid"))));
        }

        return details.Count > 0
            ? TypedResults.BadRequest(new ApiErrorResponse(new ApiError("VALIDATION_ERROR", "One or more fields are invalid.", details)))
            : await next(context);
    }
}
