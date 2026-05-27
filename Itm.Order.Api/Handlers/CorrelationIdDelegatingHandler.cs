using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Itm.Order.Api.Handlers;

public class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdDelegatingHandler(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // =====================================================
        // PROPAGAR CORRELATION ID
        // =====================================================

        var correlationId = _httpContextAccessor.HttpContext?
            .Request.Headers["X-Correlation-ID"]
            .FirstOrDefault();

        if (!string.IsNullOrEmpty(correlationId)
            && !request.Headers.Contains("X-Correlation-ID"))
        {
            request.Headers.Add("X-Correlation-ID", correlationId);
        }

        // =====================================================
        // PROPAGAR JWT TOKEN
        // =====================================================

        var authHeader = _httpContextAccessor.HttpContext?
            .Request.Headers["Authorization"]
            .FirstOrDefault();

        if (!string.IsNullOrEmpty(authHeader))
        {
            request.Headers.Authorization =
                AuthenticationHeaderValue.Parse(authHeader);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}