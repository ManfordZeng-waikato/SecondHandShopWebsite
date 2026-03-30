using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SecondHandShop.Application.Abstractions.Security;
using SecondHandShop.Application.UseCases.Inquiries;
using SecondHandShop.WebApi.Contracts;

namespace SecondHandShop.WebApi.Filters;

public sealed class ApiExceptionFilter(ILogger<ApiExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var (statusCode, message) = context.Exception switch
        {
            KeyNotFoundException ex =>
                (StatusCodes.Status404NotFound, ex.Message),

            ArgumentException ex =>
                (StatusCodes.Status400BadRequest, ex.Message),

            InquiryTurnstileValidationException ex =>
                (StatusCodes.Status400BadRequest, ex.Message),

            UnauthorizedAccessException =>
                (StatusCodes.Status401Unauthorized, "Invalid credentials"),

            InvalidOperationException ex =>
                (StatusCodes.Status409Conflict, ex.Message),

            // DbUpdateConcurrencyException inherits from DbUpdateException; match it first.
            DbUpdateConcurrencyException =>
                (StatusCodes.Status409Conflict,
                 "The resource was modified by another request. Please refresh and try again."),

            DbUpdateException =>
                (StatusCodes.Status409Conflict,
                 "A data conflict occurred while saving changes. Please try again."),

            InquiryRateLimitExceededException ex =>
                (StatusCodes.Status429TooManyRequests, ex.Message),

            TurnstileValidationUnavailableException ex =>
                (StatusCodes.Status502BadGateway, ex.Message),

            HttpRequestException =>
                (StatusCodes.Status502BadGateway,
                 "An external service is temporarily unavailable. Please try again later."),

            _ => (0, (string?)null)
        };

        if (statusCode == 0)
            return;

        if (statusCode >= 500)
        {
            logger.LogError(context.Exception,
                "Unhandled server error in {Action}", context.ActionDescriptor.DisplayName);
        }

        context.Result = new ObjectResult(new ErrorResponse(message!)) { StatusCode = statusCode };
        context.ExceptionHandled = true;
    }
}
