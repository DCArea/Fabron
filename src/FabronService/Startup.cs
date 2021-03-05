using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using AspNetCore.Authentication.ApiKey;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using FabronService.Commands;

namespace Fabron.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHealthChecks();
            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });
            var validApiKey = Configuration["ApiKey"];
            services.AddAuthentication(ApiKeyDefaults.AuthenticationScheme)
                .AddApiKeyInAuthorizationHeader(options =>
                {
                    options.Realm = "FabronService API";
                    options.KeyName = "token";
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
            services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Server", Version = "v1" });
            });
            services.AddHttpClient();

            services.RegisterJobCommandHandlers(typeof(RequestWebAPI).Assembly);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Server v1"));
            }

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health").AllowAnonymous();
                endpoints.MapControllers();
            });
        }
    }
}
