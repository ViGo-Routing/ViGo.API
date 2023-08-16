using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text;
using ViGo.API.CronJobs;
using ViGo.API.Middlewares;
using ViGo.API.SignalR;
using ViGo.Utilities.Configuration;

namespace ViGo.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Config log
            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext();

            string logOutputTemplate = "{NewLine}[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {CorrelationId} {Level:u3}] ({SourceContext}) {Username} {Message:lj}{NewLine}{Exception}{NewLine}";
            if (!builder.Environment.IsProduction())
            {
                Console.OutputEncoding = Encoding.UTF8;
                loggerConfig = loggerConfig.WriteTo.Console(outputTemplate: logOutputTemplate);
            } else
            {
                loggerConfig = loggerConfig.WriteTo.AzureApp(outputTemplate: logOutputTemplate);
            }

            var logger = loggerConfig.CreateLogger();
            builder.Logging.ClearProviders();
            //builder.Logging.AddConsole();
            builder.Logging.AddSerilog(logger);
            builder.Services.AddLogging();

            // Initialize Configuration
            ViGoConfiguration.Initialize(builder.Configuration);

            // Add services to the container.

            builder.Services.AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ReferenceLoopHandling =
                    Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                    options.SerializerSettings.Converters.Add(
                        new StringEnumConverter());
                }
                    )
                .ConfigureApiBehaviorOptions(opt =>
                    {
                        opt.InvalidModelStateResponseFactory = (context =>
                        {
                            var _logger = context.HttpContext
                            .RequestServices.GetRequiredService<ILogger<Program>>();
                            string message = context.ModelState.Values.SelectMany(x => x.Errors).First().ErrorMessage;

                            string errorMessage = JsonConvert.SerializeObject(
                            context.ModelState.Values.SelectMany(x => x.Errors));
                            _logger.LogError(new ArgumentException(errorMessage),
                                 "Bad Request error: {0}", message);

                            return new BadRequestObjectResult(message);
                        });
                    });

            // Dependency Injection
            builder.Services.AddViGoDependencyInjection(builder.Environment);

            builder.Services.RegisterCronJobs(builder.Environment);

            builder.Services.AddDateOnlyTimeOnlyStringConverters();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "ViGo API",
                    Version = "v1"
                });
                //c.CustomSchemaIds(x => x.Assembly.FullName + x.FullName);
                c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Description = "JWT Authentication header using the Bearer scheme.",
                    Name = "Authorization",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
                });
                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[]{}
                    }
                });

                var xmlFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFileName));
            });
            builder.Services.AddSwaggerGenNewtonsoftSupport();

            // Authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(options =>
                {
                    options.SaveToken = true;
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidAudience = ViGoConfiguration.ValidAudience,
                        ValidIssuer = ViGoConfiguration.ValidIssuer,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(ViGoConfiguration.Secret)),
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ClockSkew = TimeSpan.Zero
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                            {
                                context.Response.Headers.Add("Token-Expired", "true");
                            }
                            return Task.CompletedTask;
                        },

                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];

                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) &&
                                path.StartsWithSegments("/vigoGpsTrackingHub"))
                            {
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });
                //.AddPolicyScheme(JwtBearerDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme, options =>
                //{
                //    options.ForwardDefaultSelector = context =>
                //    {
                //        string authorization = context.Request.Headers[HeaderNames.Authorization];
                //        if (!string.IsNullOrEmpty(authorization) &&
                //            authorization.StartsWith("Bearer "))
                //        {
                //            string token = authorization.Substring("Bearer ".Length).Trim();
                //            JwtSecurityTokenHandler jwtHandler = new JwtSecurityTokenHandler();

                //            if (jwtHandler.CanReadToken(token))
                //            {
                //                if (jwtHandler.ReadJwtToken(token).Issuer.Equals("https://securetoken.google.com/" + ViGoConfiguration.FirebaseProjectId))
                //                {
                //                    return "Firebase_Bearer";
                //                } else
                //                {
                //                    return "API_Bearer";
                //                }
                //            }
                //        }
                //        return "API_Bearer";
                //    };
                //});
            builder.Services.AddAuthorization();

            // CORS
            builder.Services.AddCors(c =>
            {
                c.AddPolicy("AllowAll", options =>
                    options
                    .AllowAnyHeader().AllowAnyMethod()
                    .SetIsOriginAllowed(host => true)
                    .AllowCredentials());
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment())
            //{
                app.UseSwagger();
                app.UseSwaggerUI();
            //}

            app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseMiddleware<InitializeIdentityMiddleware>();
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseAuthorization();

            app.UseCors("AllowAll");

            app.MapControllers();

            app.MapHub<SignalRHub>("vigoHub");
            app.MapHub<GpsTrackingSystem>("vigoGpsTrackingHub");

            app.Run();
        }
    }
}