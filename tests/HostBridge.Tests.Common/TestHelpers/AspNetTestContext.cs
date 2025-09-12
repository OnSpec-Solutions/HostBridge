using HostBridge.Abstractions;

namespace HostBridge.Tests.Common.TestHelpers;

/// <summary>
/// Utilities for simulating classic ASP.NET requests in tests.
/// </summary>
public static class AspNetTestContext
{
    /// <summary>
    /// Creates a fresh HttpContext and assigns it to HttpContext.Current.
    /// Also optionally attaches a provided IServiceScope into the context items under the ASP.NET ScopeKey.
    /// </summary>
    /// <param name="scope">Optional request scope to attach.</param>
    public static HttpContext NewRequest(IServiceScope? scope = null)
    {
        var httpReq = new HttpRequest("test.aspx", "http://localhost/test.aspx", string.Empty);
        var httpResp = new HttpResponse(new StringWriter());
        var ctx = new HttpContext(httpReq, httpResp);
        HttpContext.Current = ctx;
        if (scope != null)
        {
            ctx.Items[Constants.ScopeKey] = scope;
        }

        return ctx;
    }
}