using System.Net;
using System.Text.Json;

namespace TranTranHiep_2122110538.Infrastructure;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _log;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> log)
    {
        _next = next;
        _log = log;
    }

    public async Task InvokeAsync(HttpContext context, IWebHostEnvironment env)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Lỗi chưa xử lý");
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var payload = new
            {
                message = "Đã xảy ra lỗi trên máy chủ.",
                detail = env.IsDevelopment() ? ex.Message : null
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
