using Encuesta.Api;
using Encuesta.Api.Endpoints;
using Encuesta.Application;
using Encuesta.Application.Abstractions;
using Encuesta.Infrastructure;
using Encuesta.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication(builder.Configuration["MediatR:LicenseKey"]);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

builder.Services.Configure<JsonOptions>(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        if (builder.Environment.IsDevelopment())
        {
            o.MapInboundClaims = false;
            o.TokenValidationParameters = DevAuth.ValidationParameters(builder.Configuration);
            return;
        }

        builder.Configuration.GetSection("Authentication").Bind(o);
        o.TokenValidationParameters.RoleClaimType = "role";
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<EncuestaDbContext>().Database.Migrate();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

app.MapEncuestaEndpoints();
if (app.Environment.IsDevelopment())
    app.MapDevTokenEndpoint();

app.Run();

public partial class Program;
