using System.Threading.Tasks;
using AspNetCore.Authentication.ApiKey;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FabronService.Hosting;

public static class SecurityConfigureExtensions
{
    public static WebApplicationBuilder ConfigureSecurity(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddApiKeyAuth(builder.Configuration["ApiKey"]!)
            .AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });
        return builder;
    }

    public static WebApplication UseSecurity(this WebApplication app)
    {
        app.UseAuthentication()
            .UseAuthorization();
        return app;
    }

    private static IServiceCollection AddApiKeyAuth(this IServiceCollection services, string validApiKey)
    {
        services.AddAuthentication(ApiKeyDefaults.AuthenticationScheme)
            .AddApiKeyInAuthorizationHeader(options =>
            {
                options.Realm = "FabronService API";
                options.KeyName = "token";
                options.IgnoreAuthenticationIfAllowAnonymous = true;
                options.Events = new ApiKeyEvents
                {
                    OnValidateKey = ctx =>
                    {
                        if (ctx.ApiKey == validApiKey)
                        {
                            ctx.ValidationSucceeded("debug");
                        }
                        else
                        {
                            ctx.ValidationFailed();
                        }
                        return Task.CompletedTask;
                    }
                };
            });
        return services;
    }
}
