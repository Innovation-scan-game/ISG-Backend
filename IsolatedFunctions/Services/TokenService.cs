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

namespace IsolatedFunctions.Services;

[OpenApiExample(typeof(LoginRequestExample))]
public class LoginRequest
{
    [OpenApiProperty(Description = "Username for the user logging in.")]
    [JsonRequired]
    public string Username { get; set; }

    [OpenApiProperty(Description = "Password for the user logging in.")]
    [JsonRequired]
    public string Password { get; set; }
}

public class LoginRequestExample : OpenApiExample<LoginRequest>
{
    public override IOpenApiExample<LoginRequest> Build(NamingStrategy NamingStrategy = null)
    {
        Examples.Add(OpenApiExampleResolver.Resolve("Erwin",
            new LoginRequest()
            {
                Username = "Erwin",
                Password = "SuperSecretPassword123!!"
            },
            NamingStrategy));

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

    public Guid UserId { get; }

    public LoginResult(JwtSecurityToken token, User user)
    {
        Token = token;
        UserId = user.Id;
    }
}

public interface ITokenService
{
    Task<LoginResult> CreateToken(User user);
    Task<ClaimsPrincipal> GetByValue(string Value);
}

public class TokenIdentityValidationParameters : TokenValidationParameters
{
    public TokenIdentityValidationParameters(string Issuer, string Audience, SymmetricSecurityKey SecurityKey)
    {
        RequireSignedTokens = true;
        ValidAudience = Audience;
        ValidateAudience = true;
        ValidIssuer = Issuer;
        ValidateIssuer = true;
        ValidateIssuerSigningKey = true;
        ValidateLifetime = true;
        IssuerSigningKey = SecurityKey;
        AuthenticationType = "Bearer";
    }
}

public class TokenService : ITokenService
{
    private ILogger Logger { get; }

    private string Issuer { get; }
    private string Audience { get; }
    private TimeSpan ValidityDuration { get; }

    private SigningCredentials Credentials { get; }
    private TokenIdentityValidationParameters ValidationParameters { get; }

    public TokenService(IConfiguration Configuration, ILogger<TokenService> Logger)
    {
        this.Logger = Logger;

        Issuer = /*Configuration.GetClassValueChecked("JWT:Issuer", */"DebugIssuer"; //, Logger);
        Audience = /*Configuration.GetClassValueChecked("JWT:Audience", */"DebugAudience"; //, Logger);
        ValidityDuration = TimeSpan.FromDays(1); // Todo: configure
        string Key = /*Configuration.GetClassValueChecked("JWT:Key", */"DebugKey DebugKey"; //, Logger);

        SymmetricSecurityKey SecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));

        Credentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256Signature);

        ValidationParameters = new TokenIdentityValidationParameters(Issuer, Audience, SecurityKey);
    }

    public async Task<LoginResult> CreateToken(User user)
    {
        JwtSecurityToken Token = await CreateToken(new Claim[]
        {
            new(ClaimTypes.Role, user.Role.ToString()),
            new(ClaimTypes.Name, user.Name),
        });

        return new LoginResult(Token, user);
    }

    private async Task<JwtSecurityToken> CreateToken(Claim[] Claims)
    {
        JwtHeader Header = new JwtHeader(Credentials);

        JwtPayload Payload = new JwtPayload(Issuer,
            Audience,
            Claims,
            DateTime.UtcNow,
            DateTime.UtcNow.Add(ValidityDuration),
            DateTime.UtcNow);

        JwtSecurityToken SecurityToken = new JwtSecurityToken(Header, Payload);

        return await Task.FromResult(SecurityToken);
    }

    public async Task<ClaimsPrincipal> GetByValue(string Value)
    {
        if (Value == null)
        {
            throw new Exception("No Token supplied");
        }

        JwtSecurityTokenHandler Handler = new JwtSecurityTokenHandler();

        SecurityToken ValidatedToken;
        ClaimsPrincipal Principal = Handler.ValidateToken(Value, ValidationParameters, out ValidatedToken);

        return await Task.FromResult(Principal);
    }
}
