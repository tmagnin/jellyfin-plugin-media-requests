using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Plugin.MediaRequests.Services;

/// <summary>Puts the plugin's middleware at the front of Jellyfin's request pipeline.</summary>
public sealed class IndexInjectionStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseMiddleware<IndexInjectionMiddleware>();
            next(app);
        };
    }
}

/// <summary>
/// Adds one script tag to the served web client index page, so the Requests button shows up in the
/// normal user interface. Nothing on disk is modified: the page is changed as it is sent.
/// </summary>
public sealed class IndexInjectionMiddleware
{
    // Relative to /web/ or /web/index.html, so it also works behind a base URL such as /jellyfin.
    private const string ScriptTag = "<script src=\"../MediaRequests/Client.js\" defer data-media-requests></script>";

    private readonly RequestDelegate _next;

    public IndexInjectionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsIndexRequest(context.Request))
        {
            await _next(context);
            return;
        }

        // Ask for a plain, complete response so the page can be edited before it is sent.
        var headers = context.Request.Headers;
        headers.Remove(HeaderNames.AcceptEncoding);
        headers.Remove(HeaderNames.IfNoneMatch);
        headers.Remove(HeaderNames.IfModifiedSince);
        headers.Remove(HeaderNames.Range);
        headers.Remove(HeaderNames.IfRange);

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);
        }
        finally
        {
            context.Response.Body = originalBody;
        }

        var contentType = context.Response.ContentType;
        var isHtml = context.Response.StatusCode == StatusCodes.Status200OK
                     && contentType is not null
                     && contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase);

        buffer.Position = 0;
        if (!isHtml)
        {
            await buffer.CopyToAsync(originalBody, context.RequestAborted);
            return;
        }

        var html = Encoding.UTF8.GetString(buffer.ToArray());
        var index = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        if (index >= 0 && !html.Contains("data-media-requests", StringComparison.Ordinal))
        {
            html = html.Insert(index, ScriptTag);
        }

        var bytes = Encoding.UTF8.GetBytes(html);
        context.Response.ContentLength = bytes.Length;
        context.Response.Headers.Remove(HeaderNames.ETag);
        context.Response.Headers.Remove(HeaderNames.LastModified);
        await originalBody.WriteAsync(bytes, context.RequestAborted);
    }

    private static bool IsIndexRequest(HttpRequest request)
    {
        if (!HttpMethods.IsGet(request.Method))
        {
            return false;
        }

        var path = request.Path.Value ?? string.Empty;
        return path.EndsWith("/web/index.html", StringComparison.OrdinalIgnoreCase)
               || path.EndsWith("/web/", StringComparison.OrdinalIgnoreCase);
    }
}
