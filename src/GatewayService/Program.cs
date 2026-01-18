using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// CORS: allow specific frontend origins
var corsPolicy = "AllowWebApp";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy, policy =>
        policy
            .WithOrigins(builder.Configuration["ClientApp"] ?? "http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
    );
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["IdentityServerUrl"];
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters.ValidateAudience = false;
        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.ValidIssuers = new[]
        {
            "http://identity-svc",
            "http://localhost:5000"
        };
    });



var app = builder.Build();

// Apply CORS before proxying
app.UseCors(corsPolicy);

app.MapReverseProxy();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
