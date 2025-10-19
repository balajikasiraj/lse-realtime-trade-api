using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Lse.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            var problem = new
            {
                success = false,
                message = ex.Message,
                detail = ex.StackTrace
            };

            var payload = JsonSerializer.Serialize(problem);

            context.Response.ContentType = "application/json";

            // Map specific exception types to status codes here
            var status = HttpStatusCode.InternalServerError;
            if (ex is ArgumentException || ex is ArgumentNullException)
                status = HttpStatusCode.BadRequest;
            else if (ex is KeyNotFoundException)
                status = HttpStatusCode.NotFound;

            context.Response.StatusCode = (int)status;
            return context.Response.WriteAsync(payload);
        }
    }
}
