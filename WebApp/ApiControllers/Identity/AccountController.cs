using App.DTO.v1;
using App.DTO.v1.Identity;
using Base.Helpers;

namespace WebApp.ApiControllers.Identity;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using App.DAL.EF;
using App.Domain.Identity;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiVersion("1.0")]
[ApiController]
[Route("/api/v{version:apiVersion}/identity/[controller]/[action]")]
public class AccountController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<AccountController> _logger;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;

    public AccountController(UserManager<AppUser> userManager, ILogger<AccountController> logger,
        SignInManager<AppUser> signInManager, IConfiguration configuration, AppDbContext context)
    {
        _userManager = userManager;
        _logger = logger;
        _signInManager = signInManager;
        _configuration = configuration;
        _context = context;
    }


    [HttpPost]
    public async Task<ActionResult<JWTResponse>> Login(
        [FromBody]
        LoginInfo loginInfo,
        [FromQuery]
        int expiresInSeconds
    )
    {
        if (expiresInSeconds <= 0) expiresInSeconds = int.MaxValue;
        expiresInSeconds = expiresInSeconds < _configuration.GetValue<int>("JWT:ExpiresInSeconds")
            ? expiresInSeconds
            : _configuration.GetValue<int>("JWT:ExpiresInSeconds");

        // verify user
        var appUser = await _userManager.FindByEmailAsync(loginInfo.Email);
        if (appUser == null)
        {
            _logger.LogWarning("WebApi login failed, email {} not found", loginInfo.Email);
            // TODO: random delay to prevent user enumeration timing attacks
            return NotFound("User/Password problem");
        }

        // verify password
        var result = await _signInManager.CheckPasswordSignInAsync(appUser, loginInfo.Password, false);
        if (!result.Succeeded)
        {
            _logger.LogWarning("WebApi login failed, password for email {} was wrong",
                loginInfo.Email);
            // TODO: random delay to prevent user enumeration timing attacks
            return NotFound("User/Password problem");
        }

        var claimsPrincipal = await _signInManager.CreateUserPrincipalAsync(appUser);


        // clean up expired refresh tokens
        // EF Core InMemory provider does not support ExecuteDeleteAsync, so skip during integration tests
        if (!_context.Database.ProviderName!.Contains("InMemory"))
        {
            var deletedRows = await _context.RefreshTokens
                .Where(t => t.AppUserId == appUser.Id && t.ExpirationDT < DateTime.UtcNow)
                .ExecuteDeleteAsync();
            _logger.LogInformation("Deleted {} refresh tokens", deletedRows);
        }


        var refreshToken = new AppRefreshToken()
        {
            AppUserId = appUser.Id
        };
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        var jwt = IdentityHelpers.GenerateJwt(
            claimsPrincipal.Claims,
            _configuration.GetValue<string>("JWT:Key")!,
            _configuration.GetValue<string>("JWT:Issuer")!,
            _configuration.GetValue<string>("JWT:Audience")!,
            expiresInSeconds
        );

        var responseData = new JWTResponse()
        {
            Jwt = jwt,
            RefreshToken = refreshToken.RefreshToken
        };

        return Ok(responseData);
    }

    [HttpPost]
    public async Task<ActionResult<JWTResponse>> RefreshTokenData(
        [FromBody]
        TokenRefreshInfo tokenRefreshInfo,
        [FromQuery]
        int expiresInSeconds
    )
    {
        if (expiresInSeconds <= 0) expiresInSeconds = int.MaxValue;
        expiresInSeconds = expiresInSeconds < _configuration.GetValue<int>("JWT:ExpiresInSeconds")
            ? expiresInSeconds
            : _configuration.GetValue<int>("JWT:ExpiresInSeconds");

        // extract jwt object
        JwtSecurityToken? jwt;
        try
        {
            jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokenRefreshInfo.Jwt);
            if (jwt == null)
            {
                return BadRequest(
                    new RestApiErrorResponse()
                    {
                        Status = HttpStatusCode.BadRequest,
                        Error = "No token"
                    }
                );
            }
        }
        catch (Exception e)
        {
            return BadRequest(new RestApiErrorResponse()
                {
                    Status = HttpStatusCode.BadRequest,
                    Error = "No token"
                }
            );
        }

        // validate jwt, ignore expiration date

        if (!IdentityHelpers.ValidateJWT(
                tokenRefreshInfo.Jwt,
                _configuration.GetValue<string>("JWT:Key")!,
                _configuration.GetValue<string>("JWT:Issuer")!,
                _configuration.GetValue<string>("JWT:Audience")!
            ))
        {
            return BadRequest("JWT validation fail");
        }

        var userEmail = jwt.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
        if (userEmail == null)
        {
            return BadRequest(
                new RestApiErrorResponse()
                {
                    Status = HttpStatusCode.BadRequest,
                    Error = "No email in jwt"
                }
            );
        }

        var appUser = await _userManager.FindByEmailAsync(userEmail);
        if (appUser == null)
        {
            return NotFound($"User with email {userEmail} not found");
        }

        // load and compare refresh tokens
        await _context.Entry(appUser).Collection(u => u.RefreshTokens!)
            .Query()
            .Where(x =>
                (x.RefreshToken == tokenRefreshInfo.RefreshToken && x.ExpirationDT > DateTime.UtcNow) ||
                (x.PreviousRefreshToken == tokenRefreshInfo.RefreshToken &&
                 x.PreviousExpirationDT > DateTime.UtcNow)
            )
            .ToListAsync();

        if (appUser.RefreshTokens == null || appUser.RefreshTokens.Count == 0)
        {
            return NotFound(
                new RestApiErrorResponse()
                {
                    Status = HttpStatusCode.NotFound,
                    Error = $"RefreshTokens collection is null or empty - {appUser.RefreshTokens?.Count}"
                }
            );
        }

        if (appUser.RefreshTokens.Count != 1)
        {
            return NotFound("More than one valid refresh token found");
        }


        // get claims based user
        var claimsPrincipal = await _signInManager.CreateUserPrincipalAsync(appUser);

        // generate jwt
        var jwtResponseStr = IdentityHelpers.GenerateJwt(
            claimsPrincipal.Claims,
            _configuration.GetValue<string>("JWT:Key")!,
            _configuration.GetValue<string>("JWT:Issuer")!,
            _configuration.GetValue<string>("JWT:Audience")!,
            expiresInSeconds
        );

        // make new refresh token, keep old one still valid for some time
        var refreshToken = appUser.RefreshTokens.First();
        if (refreshToken.RefreshToken == tokenRefreshInfo.RefreshToken)
        {
            refreshToken.PreviousRefreshToken = refreshToken.RefreshToken;
            refreshToken.PreviousExpirationDT = DateTime.UtcNow.AddMinutes(1);

            refreshToken.RefreshToken = Guid.NewGuid().ToString();
            refreshToken.ExpirationDT = DateTime.UtcNow.AddDays(7);

            await _context.SaveChangesAsync();
        }

        var res = new JWTResponse()
        {
            Jwt = jwtResponseStr,
            RefreshToken = refreshToken.RefreshToken,
        };

        return Ok(res);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost]
    public async Task<ActionResult> Logout(
        [FromBody]
        LogoutInfo logout)
    {
        // delete the refresh token - so user is kicked out after jwt expiration
        // We do not invalidate the jwt on serverside - that would require pipeline modification and checking against db on every request
        // so client can actually continue to use the jwt until it expires (keep the jwt expiration time short ~1 min)

        var userIdStr = _userManager.GetUserId(User);
        if (userIdStr == null)
        {
            return BadRequest(
                new RestApiErrorResponse()
                {
                    Status = HttpStatusCode.BadRequest,
                    Error = "Invalid refresh token"
                }
            );
        }

        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return BadRequest("Deserialization error");
        }

        var appUser = await _context.Users
            .Where(u => u.Id == userId)
            .SingleOrDefaultAsync();
        if (appUser == null)
        {
            return NotFound(
                new RestApiErrorResponse()
                {
                    Status = HttpStatusCode.NotFound,
                    Error = "User/Password problem"
                }
            );
        }

        await _context.Entry(appUser)
            .Collection(u => u.RefreshTokens!)
            .Query()
            .Where(x =>
                (x.RefreshToken == logout.RefreshToken) ||
                (x.PreviousRefreshToken == logout.RefreshToken)
            )
            .ToListAsync();

        foreach (var appRefreshToken in appUser.RefreshTokens!)
        {
            _context.RefreshTokens.Remove(appRefreshToken);
        }

        var deleteCount = await _context.SaveChangesAsync();

        return Ok(new {TokenDeleteCount = deleteCount});
    }
}