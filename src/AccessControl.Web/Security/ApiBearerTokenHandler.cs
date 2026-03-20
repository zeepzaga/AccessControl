using System.Net.Http.Headers;

namespace AccessControl.Web.Security;

public class ApiBearerTokenHandler : DelegatingHandler
{
    private readonly ApiAuthenticationService _apiAuthenticationService;

    public ApiBearerTokenHandler(ApiAuthenticationService apiAuthenticationService)
    {
        _apiAuthenticationService = apiAuthenticationService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessToken = await _apiAuthenticationService.GetValidAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
