using System.Text.Json;
using TeamNotificationService.Api.Dtos.Common;
using TeamNotificationService.Domain.Exceptions;

namespace TeamNotificationService.Api.Middlewares;

public class GlobalExceptionHandlerMiddleware(
    RequestDelegate next,
    Serilog.ILogger logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var (statusCode, response) = CreateResponse(exception);

            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                logger.Error(
                    exception,
                    "Unhandled exception while processing [{RequestMethod}] {RequestPath}",
                    context.Request.Method,
                    context.Request.Path);
            }
            else
            {
                logger.Warning(
                    exception,
                    "Request failed with status code {StatusCode}: [{RequestMethod}] {RequestPath}",
                    statusCode,
                    context.Request.Method,
                    context.Request.Path);
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
    }

    private static (int StatusCode, ErrorResponse Response) CreateResponse(Exception exception)
    {
        return exception switch
        {
            UnauthorizedException unauthorized =>
                (StatusCodes.Status401Unauthorized,
                new ErrorResponse
                {
                    Message = unauthorized.Message,
                    Details = unauthorized.Details
                }),

            ForbiddenException forbidden =>
                (StatusCodes.Status403Forbidden,
                new ErrorResponse
                {
                    Message = forbidden.Message,
                    Details = forbidden.Details
                }),

            NotFoundException notFound =>
                (StatusCodes.Status404NotFound,
                new ErrorResponse
                {
                    Message = notFound.Message,
                    Details = notFound.Details
                }),

            ConflictException conflict =>
                (StatusCodes.Status409Conflict,
                new ErrorResponse
                {
                    Message = conflict.Message,
                    Details = conflict.Details
                }),

            UnprocessableEntityException unprocessable =>
                (StatusCodes.Status422UnprocessableEntity,
                new ErrorResponse
                {
                    Message = unprocessable.Message,
                    Details = unprocessable.Details
                }),

            _ =>
                (StatusCodes.Status500InternalServerError,
                new ErrorResponse
                {
                    Message = "Something went wrong."
                })
        };
    }
}
