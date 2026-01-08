using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuctionService.IntegrationTests.Util;

internal class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string UserHeaderName = "X-User";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock) : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Determine the username from header or fallback
        var userName = Request.Headers.ContainsKey(UserHeaderName)
            ? Request.Headers[UserHeaderName].ToString()
            : "test-user";

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, userName)
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
