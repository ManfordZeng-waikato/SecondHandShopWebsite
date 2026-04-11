using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using SecondHandShop.Application.Abstractions.Security;
using SecondHandShop.Application.Common.Exceptions;
using SecondHandShop.Application.UseCases.Inquiries;
using SecondHandShop.Domain.Common;
using SecondHandShop.WebApi.Contracts;
using AppValidationException = SecondHandShop.Application.Common.Exceptions.ValidationException;

namespace SecondHandShop.WebApi.Filters;

public sealed class ApiExceptionFilter(ILogger<ApiExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        // Client disconnected or aborted the request (e.g. React Strict Mode double-fetch, fast navigation).
        // Do not surface as HTTP 500 or log as a server failure.
        if (context.Exception is OperationCanceledException && context.HttpContext.RequestAborted.IsCancellationRequested)
        {
            context.ExceptionHandled = true;
            context.Result = new StatusCodeResult(StatusCodes.Status499ClientClosedRequest);
            return;
        }

        var (statusCode, message) = context.Exception switch
        {
            KeyNotFoundException ex =>
                (StatusCodes.Status404NotFound, ex.Message),

            ArgumentException ex =>
                (StatusCodes.Status400BadRequest, ex.Message),

            AppValidationException ex =>
                (StatusCodes.Status400BadRequest, ex.Message),

            InquiryTurnstileValidationException ex =>
                (StatusCodes.Status400BadRequest, ex.Message),

            UnauthorizedAccessException =>
                (StatusCodes.Status401Unauthorized, "Invalid credentials"),

            ConflictException ex =>
                (StatusCodes.Status409Conflict, ex.Message),

            DomainRuleViolationException ex =>
                (StatusCodes.Status422UnprocessableEntity, ex.Message),

            // Only optimistic-concurrency failures are real 409s. Other DbUpdateException
            // variants (constraint violations, driver faults like the Npgsql
            // ObjectDisposedException race) must not be masqueraded as conflicts — that
            // hides root causes and confuses clients. Fall through to the 500 branch.
            DbUpdateConcurrencyException =>
                (StatusCodes.Status409Conflict,
                 "The resource was modified by another request. Please refresh and try again."),

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

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(context.Exception,
                "Unhandled server error in {Action}", context.ActionDescriptor.DisplayName);
        }
        else if (statusCode >= StatusCodes.Status400BadRequest)
        {
            logger.LogWarning(context.Exception,
                "Handled client error {StatusCode} in {Action}: {Message}",
                statusCode,
                context.ActionDescriptor.DisplayName,
                message);
        }

        context.Result = new ObjectResult(new ErrorResponse(message!)) { StatusCode = statusCode };
        context.ExceptionHandled = true;
    }
}
