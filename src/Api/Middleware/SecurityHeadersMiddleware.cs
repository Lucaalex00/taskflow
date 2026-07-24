namespace TaskFlow.Api.Middleware;

/// <summary>
/// Adds standard defensive HTTP response headers to every API response. The API only ever
/// returns JSON (never renders HTML), so these are mostly belt-and-braces: they cost nothing
/// and remove an entire class of "why doesn't this API set X" findings from a security review.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // The API never serves content a browser should sniff/render as HTML.
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

        // JSON-only responses, so the strictest possible CSP applies — there's no HTML page
        // here that could load a script/style/frame from anywhere.
        headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

        // Only meaningful once the app sits behind TLS (the Docker Compose demo is plain HTTP
        // for local/portfolio simplicity) — browsers ignore Strict-Transport-Security received
        // over an insecure connection, so it's safe to always send.
        headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        return next(context);
    }
}
