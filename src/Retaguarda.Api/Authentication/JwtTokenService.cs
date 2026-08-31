using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Retaguarda.Business.Authentication;
using Retaguarda.Shared;
using Retaguarda.Shared.Authentication;

namespace Retaguarda.Api.Authentication;

// Implementação JWT (HMAC-SHA256) do ITokenService. Os tipos de claim seguem os mesmos do
// cookie do Web (ClaimTypes.NameIdentifier + RetaguardaClaims.SiteId), para que o
// CurrentUserService compartilhado e o Global Query Filter funcionem igual na API.
public sealed class JwtTokenService : ITokenService
{
    private const string PurposeClaim = "purpose";
    private const string SiteSelectionPurpose = "site_selection";

    private readonly JwtOptions _options;
    private readonly SymmetricSecurityKey _signingKey;
    private readonly JwtSecurityTokenHandler _handler;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));

        // Sem mapeamento de claims: os tipos ficam exatamente como emitidos (sub, SiteId, role...).
        _handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
    }

    public string CreatePreAuthToken(string userId)
    {
        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, userId),
            new(ClaimTypes.NameIdentifier, userId),
            new(PurposeClaim, SiteSelectionPurpose),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        ];

        return WriteToken(claims, _options.PreAuthAudience, _options.PreAuthTokenMinutes);
    }

    public AccessTokenResult CreateAccessToken(
        string userId, int siteId, string siteName, IReadOnlyCollection<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(ClaimTypes.NameIdentifier, userId),
            new(RetaguardaClaims.SiteId, siteId.ToString(CultureInfo.InvariantCulture)),
            new(RetaguardaClaims.SiteName, siteName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        return new AccessTokenResult(
            WriteToken(claims, _options.Audience, _options.AccessTokenMinutes), expiresAt);
    }

    public string? ValidatePreAuthToken(string token)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.PreAuthAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _signingKey,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role,
        };

        try
        {
            var principal = _handler.ValidateToken(token, parameters, out _);

            // Garante que é mesmo um token de pré-autenticação (e não um access token).
            var purpose = principal.FindFirstValue(PurposeClaim);
            if (!string.Equals(purpose, SiteSelectionPurpose, StringComparison.Ordinal))
            {
                return null;
            }

            return principal.FindFirstValue(ClaimTypes.NameIdentifier);
        }
        catch (SecurityTokenException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            // Token malformado (não é um JWT).
            return null;
        }
    }

    private string WriteToken(IEnumerable<Claim> claims, string audience, int lifetimeMinutes)
    {
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(lifetimeMinutes),
            signingCredentials: new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256));

        return _handler.WriteToken(token);
    }
}
