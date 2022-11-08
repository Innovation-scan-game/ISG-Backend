using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Domain.Models;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Resolvers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Services;

[OpenApiExample(typeof(LoginRequestExample))]
public class LoginRequest
{
    [OpenApiProperty(Description = "Username for the user logging in.")]
    [JsonRequired]
    public string Username { get; set; } = "";

    [OpenApiProperty(Description = "Password for the user logging in.")]
    [JsonRequired]
    public string Password { get; set; } = "";
}

public class LoginRequestExample : OpenApiExample<LoginRequest>
{
    public override IOpenApiExample<LoginRequest> Build(NamingStrategy? namingStrategy = null)
    {
        Examples.Add(OpenApiExampleResolver.Resolve("Erwin",
            new LoginRequest
            {
                Username = "Erwin",
                Password = "SuperSecretPassword123!!"
            },
            namingStrategy));

        return this;
    }
}

public class LoginResult
{
    private JwtSecurityToken Token { get; }

    [OpenApiProperty(Description = "The access token to be used in every subsequent operation for this user.")]
    [JsonRequired]
    public string AccessToken => new JwtSecurityTokenHandler().WriteToken(Token);

    [OpenApiProperty(Description = "The token type.")]
    [JsonRequired]
    public string TokenType => "Bearer";

    [OpenApiProperty(Description = "The amount of seconds until the token expires.")]
    [JsonRequired]
    public int ExpiresIn => (int) (Token.ValidTo - DateTime.UtcNow).TotalSeconds;

    public User User { get; }

    public LoginResult(JwtSecurityToken token, User user)
    {
        Token = token;
        User = user;
    }
}

public interface ITokenService
{
    Task<LoginResult> CreateToken(User user);
    Task<ClaimsPrincipal> GetByValue(string value);
}

public class TokenIdentityValidationParameters : TokenValidationParameters
{
    public TokenIdentityValidationParameters(string issuer, string audience, SymmetricSecurityKey securityKey)
    {
        RequireSignedTokens = true;
        ValidAudience = audience;
        ValidateAudience = true;
        ValidIssuer = issuer;
        ValidateIssuer = true;
        ValidateIssuerSigningKey = true;
        ValidateLifetime = true;
        IssuerSigningKey = securityKey;
        AuthenticationType = "Bearer";
    }
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private ILogger Logger { get; }

    private string Issuer { get; }
    private string Audience { get; }
    private TimeSpan ValidityDuration { get; }

    private SigningCredentials Credentials { get; }
    private TokenIdentityValidationParameters ValidationParameters { get; }

    public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
    {
        _configuration = configuration;
        this.Logger = logger;

        Issuer = /*Configuration.GetClassValueChecked("JWT:Issuer", */"DebugIssuer"; //, Logger);
        Audience = /*Configuration.GetClassValueChecked("JWT:Audience", */"DebugAudience"; //, Logger);
        ValidityDuration = TimeSpan.FromDays(1); // Todo: configure
        const string key = "DebugKey DebugKey"; //, Logger);

        SymmetricSecurityKey securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

        Credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

        ValidationParameters = new TokenIdentityValidationParameters(Issuer, Audience, securityKey);
    }

    public async Task<LoginResult> CreateToken(User user)
    {
        JwtSecurityToken token = await CreateToken(new Claim[]
        {
            new(ClaimTypes.Role, user.Role.ToString()),
            new(ClaimTypes.Name, user.Name),
        });

        return new LoginResult(token, user);
    }

    private async Task<JwtSecurityToken> CreateToken(Claim[] claims)
    {
        JwtHeader header = new JwtHeader(Credentials);

        JwtPayload payload = new JwtPayload(Issuer,
            Audience,
            claims,
            DateTime.UtcNow,
            DateTime.UtcNow.Add(ValidityDuration),
            DateTime.UtcNow);

        JwtSecurityToken securityToken = new JwtSecurityToken(header, payload);

        return await Task.FromResult(securityToken);
    }

    public async Task<ClaimsPrincipal> GetByValue(string value)
    {
        if (value == null)
        {
            throw new Exception("No Token supplied");
        }

        JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();

        ClaimsPrincipal principal = handler.ValidateToken(value, ValidationParameters, out _);

        return await Task.FromResult(principal);
    }
}
