namespace TowFast.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string Header = "X-Correlation-ID";
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(Header, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()[..Math.Min(value.ToString().Length, 100)]
            : Guid.NewGuid().ToString("N");
        context.TraceIdentifier = correlationId;
        context.Response.Headers[Header] = correlationId;
        await next(context);
    }
}
