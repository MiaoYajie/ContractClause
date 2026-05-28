using ContractClause.Api.Options;
using Duende.AspNetCore.Authentication.OAuth2Introspection;
using Microsoft.AspNetCore.Authentication.JwtBearer;

using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ContractClause.Api.Auth;

public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddAuthServerAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authServer = configuration.GetSection(AuthServerOptions.SectionName).Get<AuthServerOptions>()
            ?? throw new InvalidOperationException($"Missing configuration section '{AuthServerOptions.SectionName}'.");

        if (string.IsNullOrWhiteSpace(authServer.Authority))
            throw new InvalidOperationException($"{AuthServerOptions.SectionName}:Authority is required.");

        if (string.IsNullOrWhiteSpace(authServer.ApiName))
            throw new InvalidOperationException($"{AuthServerOptions.SectionName}:ApiName is required.");

        services.Configure<AuthServerOptions>(configuration.GetSection(AuthServerOptions.SectionName));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddOAuth2Introspection(options =>
            {
                options.Authority = authServer.Authority.TrimEnd('/');
                options.ClientSecret = authServer.ApiSecret;
                options.ClientId = authServer.ApiName;

            });
        //.AddJwtBearer(options =>
        //{
        //    options.Authority = authServer.Authority.TrimEnd('/');
        //    // options.Authority = "https://localhost:5000";
        //    options.Audience = authServer.ApiName;
        //    options.RequireHttpsMetadata = authServer.RequireHttpsMetadata;
        //    options.MapInboundClaims = false;
        //    options.RequireHttpsMetadata = false;
        //    options.TokenValidationParameters = new TokenValidationParameters
        //    {

        //        //NameClaimType = JwtRegisteredClaimNames.Name,
        //        //RoleClaimType = "role",
        //        ValidateIssuerSigningKey = true,
        //        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authServer.ApiSecret)),
        //    };
        //    options.Events = new JwtBearerEvents
        //    {
        //        OnAuthenticationFailed = context =>
        //        {
        //            // 这里是打印错误原因的核心！
        //            Console.WriteLine($"认证失败: {context.Exception.Message}");
        //            return Task.CompletedTask;
        //        },
        //        OnChallenge = context =>
        //        {
        //            Console.WriteLine($"返回 401 挑战: {context.Error}, {context.ErrorDescription}");
        //            return Task.CompletedTask;
        //        }
        //    };
        //});

        services.AddAuthorization();
        return services;
    }
}
