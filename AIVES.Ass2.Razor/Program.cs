using System.Security.Claims;
using AIVES.Ass2.Razor.Infrastructure;
using AIVES.Business;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Thiếu ConnectionStrings:DefaultConnection.");

builder.Services.AddBusinessLayer(connectionString);
builder.Services.AddSignalR();
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Auth/Login");
    options.Conventions.AllowAnonymousToPage("/Auth/Register");
    options.Conventions.AllowAnonymousToPage("/Auth/AccessDenied");
    options.Conventions.AllowAnonymousToPage("/Error");
    options.Conventions.AuthorizeFolder("/Accounts", "AdminOnly");
    options.Conventions.AuthorizeFolder("/Subjects", "AdminOnly");
    options.Conventions.AuthorizeFolder("/MySubjects", "LecturerOnly");
    options.Conventions.AuthorizeFolder("/Exams", "Staff");
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = ".AIVES.Ass2";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ReturnUrlParameter = "ReturnUrl";
        options.Events.OnValidatePrincipal = async context =>
        {
            var rawId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(rawId, out var accountId) || accountId <= 0)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            var auth = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
            var role = await auth.GetActiveRoleAsync(accountId);
            // Yêu cầu đăng nhập lại nếu tài khoản bị khóa, về Pending hoặc đổi vai trò.
            // Cookie cũ không được tiếp tục giữ quyền đã bị thu hồi.
            if (role is null || context.Principal?.FindFirstValue(ClaimTypes.Role) != role)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(Roles.Admin));
    options.AddPolicy("LecturerOnly", policy => policy.RequireRole(Roles.Lecturer));
    options.AddPolicy("Staff", policy => policy.RequireRole(Roles.Admin, Roles.Lecturer));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.MapHub<LiveHub>(LiveHub.Path);
app.Run();
