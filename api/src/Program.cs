using Boardly.Api.Binders;
using Boardly.Api.Converters;
using Boardly.Api.Entities;
using Boardly.Api.Exceptions;
using Boardly.Api.Hubs;
using Boardly.Api.OpenAPI;
using Boardly.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Boardly.Api;
public class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .WriteTo.Console()
            .CreateBootstrapLogger();

        Serilog.ILogger logger = Log.ForContext("SourceContext", typeof(Program).FullName);
        try
        {
            logger.Debug("Building web application...");
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            
            builder.Configuration
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            builder.Services.AddSerilog((services, lc) => lc
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services));

            builder.Services.AddSignalR();
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy
                        .SetIsOriginAllowed(_ => true) 
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });
            builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
            builder.Services.AddSingleton<MongoDbProvider>();
            builder.Services.AddHostedService<MongoDbMigrationService>();
            builder.Services.AddSingleton<UserService>();
            builder.Services.AddSingleton<BoardService>();
            builder.Services.AddSingleton<SwimlaneService>();
            builder.Services.AddSingleton<ListService>();
            builder.Services.AddSingleton<TokenService>();

            builder.Services.AddControllers(options =>
            {
                options.ModelBinderProviders.Insert(0, new ObjectIdModelBinderProvider());
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new ObjectIdJsonConverter());
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter(allowIntegerValues: false)
                );
            });

            builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]
                            ?? throw new NullReferenceException("Jwt key must be provided!")))
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            if (string.IsNullOrEmpty(accessToken))
                                accessToken = context.Request.Cookies["access_token"];
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) &&
                                (path.StartsWithSegments("/hubs")))
                            {
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });
            builder.Services.AddOpenApi(options =>
            {
                options.AddSchemaTransformer<ObjectIdSchemaTransformer>();
                options.AddDocumentTransformer<BaseInfoDocumentTransformer>();
                options.AddDocumentTransformer<BearerSecurityDocumentTransformer>();
                options.AddSchemaTransformer<EnumAsStringSchemaTransformer>();
            });
            builder.Services.AddProblemDetails();
            builder.Services.AddExceptionHandler<ExceptionHandler>();

            WebApplication app = builder.Build();
            logger.Debug("Web application built successfully");
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference(options =>
                {
                    options.AddHttpAuthentication("BearerAuth", scheme => {});
                });
            }
            app.UseCors();
            app.UseExceptionHandler();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.MapHub<ChatHub>("/hubs/chathub");
            app.MapHub<UnauthChatHub>("/hubs/unauthchathub");

            app.Lifetime.ApplicationStarted.Register(() =>
            {
                logger.Information("Application version {Version}", Assembly.GetExecutingAssembly()?.GetName().Version);
                if (app.Environment.IsDevelopment())
                {
                    logger.Information("OpenAPI available at {Urls}", string.Join(", ", app.Urls.Select(x => $"{x}/openapi/v1.json")));
                    logger.Information("Scalar available at {Urls}", string.Join(", ", app.Urls.Select(x => $"{x}/scalar")));
                }
            });
            await app.RunAsync();
        }
        catch (Exception ex)
        {
            logger.Fatal(ex, "Application startup failed");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}