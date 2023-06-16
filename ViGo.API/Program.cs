using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text;
using ViGo.API.BackgroundTasks;
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

            if (!builder.Environment.IsProduction())
            {
                Console.OutputEncoding = Encoding.UTF8;
                loggerConfig = loggerConfig.WriteTo.Console();
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
                    );

            // Dependency Injection
            builder.Services.AddViGoDependencyInjection(builder.Environment);

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
                            Encoding.UTF8.GetBytes(ViGoConfiguration.Secret))
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

            builder.Services.AddHostedService<QueuedHostedServices>();
            builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

            // CORS
            builder.Services.AddCors(c =>
            {
                c.AddPolicy("AllowAll", options =>
                    options.AllowAnyOrigin()
                    .AllowAnyHeader().AllowAnyMethod());
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

            app.Run();
        }
    }
}