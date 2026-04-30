using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AwesomeAssertions;
using Base.Helpers;
using Microsoft.IdentityModel.Tokens;

namespace TestProject.Helpers;

public class IdentityHelpersTests
{
    private const string Key = "this_is_a_long_enough_test_signing_key_!!";
    private const string Issuer = "test-issuer";
    private const string Audience = "test-audience";

    private static List<Claim> SampleClaims() =>
        new()
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
            new Claim(ClaimTypes.Email, "user@example.com"),
        };

    [Fact]
    public void GenerateJwt_ValidInputs_ReturnsParseableToken()
    {
        var claims = SampleClaims();
        
        var jwt = IdentityHelpers.GenerateJwt(claims, Key, Issuer, Audience, expiresInSeconds: 60);

        jwt.Should().NotBeNullOrWhiteSpace();
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(jwt);
        parsed.Issuer.Should().Be(Issuer);
        parsed.Audiences.Should().Contain(Audience);
    }

    [Fact]
    public void GenerateJwt_WithClaims_TokenContainsAllClaims()
    {
        var claims = SampleClaims();

        var jwt = IdentityHelpers.GenerateJwt(claims, Key, Issuer, Audience, expiresInSeconds: 60);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(jwt);
        parsed.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "user-123");
        parsed.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "user@example.com");
    }

    [Fact]
    public void GenerateJwt_PositiveExpiresInSeconds_TokenExpirationMatches()
    {
        var before = DateTime.UtcNow;

        var jwt = IdentityHelpers.GenerateJwt(SampleClaims(), Key, Issuer, Audience, expiresInSeconds: 120);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(jwt);
        parsed.ValidTo.Should().BeOnOrAfter(before.AddSeconds(115));
        parsed.ValidTo.Should().BeOnOrBefore(DateTime.UtcNow.AddSeconds(125));
    }

    [Fact]
    public void ValidateJWT_TokenSignedWithSameKey_ReturnsTrue()
    {
        var jwt = IdentityHelpers.GenerateJwt(SampleClaims(), Key, Issuer, Audience, expiresInSeconds: 60);

        var result = IdentityHelpers.ValidateJWT(jwt, Key, Issuer, Audience);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateJWT_TokenSignedWithDifferentKey_ReturnsFalse()
    {
        var jwt = IdentityHelpers.GenerateJwt(SampleClaims(), Key, Issuer, Audience, expiresInSeconds: 60);
        var wrongKey = "completely_different_signing_key_value_!!";

        var result = IdentityHelpers.ValidateJWT(jwt, wrongKey, Issuer, Audience);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateJWT_WrongIssuer_ReturnsFalse()
    {
        var jwt = IdentityHelpers.GenerateJwt(SampleClaims(), Key, Issuer, Audience, expiresInSeconds: 60);

        var result = IdentityHelpers.ValidateJWT(jwt, Key, "wrong-issuer", Audience);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateJWT_WrongAudience_ReturnsFalse()
    {
        var jwt = IdentityHelpers.GenerateJwt(SampleClaims(), Key, Issuer, Audience, expiresInSeconds: 60);

        var result = IdentityHelpers.ValidateJWT(jwt, Key, Issuer, "wrong-audience");

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateJWT_ExpiredToken_ReturnsTrue()
    {
        // Arrange — construct a token whose expiry is already in the past;
        // ValidateJWT sets ValidateLifetime = false, so this should still validate.
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: SampleClaims(),
            notBefore: DateTime.UtcNow.AddHours(-2),
            expires: DateTime.UtcNow.AddHours(-1),
            signingCredentials: creds);
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        var result = IdentityHelpers.ValidateJWT(jwt, Key, Issuer, Audience);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateJWT_MalformedTokenString_ReturnsFalse()
    {
        var malformed = "not.a.real.jwt";

        var result = IdentityHelpers.ValidateJWT(malformed, Key, Issuer, Audience);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateJWT_EmptyString_ReturnsFalse()
    {
        var empty = "";

        var result = IdentityHelpers.ValidateJWT(empty, Key, Issuer, Audience);

        result.Should().BeFalse();
    }
}
