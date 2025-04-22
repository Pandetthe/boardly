using Boardly.Backend.Entities;
using Boardly.Backend.Services;
using Boardly.Backend.Services.OpenAPI;
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

namespace Boardly.Backend;

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

            builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
            builder.Services.AddSingleton<MongoDbProvider>();
            builder.Services.AddHostedService<MongoDbMigrationService>();
            builder.Services.AddSingleton<UserService>();
            builder.Services.AddSingleton<BoardService>();

            builder.Services.AddControllers();
            builder.Services.ConfigureHttpJsonOptions(options =>
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, false)));

            builder.Services.AddSingleton<TokenService>();
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
                });
            builder.Services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer<DocumentInfoTransformer>();
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            });

            WebApplication app = builder.Build();
            logger.Debug("Web application built successfully");

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference(options =>
                {
                    options.WithHttpBearerAuthentication(new HttpBearerOptions());
                });
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Lifetime.ApplicationStarted.Register(() => {
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
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}