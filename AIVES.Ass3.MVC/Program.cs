using System.Text.Json.Serialization;
using AIVES.Ass3.MVC.Infrastructure;
using AIVES.Business;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Controllers, Views và cấu hình JSON (enum bằng chữ, camelCase)
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Swagger cho Web API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// SignalR
builder.Services.AddSignalR();

// Cookie Authentication (.AIVES.Ass3)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = ".AIVES.Ass3";
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var userId = context.Principal?.GetUserId();
                if (userId == null)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync();
                    return;
                }

                var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                var activeRole = await authService.GetActiveRoleAsync(userId.Value);
                if (activeRole == null)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync();
                }
            },
            OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            }
        };
    });

// Business Layer + DataAccess
var cs = builder.Configuration.GetConnectionString("DefaultConnection")
         ?? "Server=.;Database=AIVESDb;Trusted_Connection=True;TrustServerCertificate=True";
var aiOptions = builder.Configuration.GetSection("Ai").Get<AiOptions>() ?? new AiOptions();
var ttsOptions = builder.Configuration.GetSection("Tts").Get<TtsOptions>() ?? new TtsOptions();

builder.Services.AddBusinessLayer(cs, aiOptions, ttsOptions);

// Hosted Service: tự khởi động VieNeu-TTS
builder.Services.AddHostedService<VieNeuLauncher>();

var app = builder.Build();

// Pipeline middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Swagger API Documentation
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AIVES API v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<LiveHub>(LiveHub.Path);

app.Run();
