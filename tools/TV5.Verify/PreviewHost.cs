using System.Security.Claims;
using System.Text.Encodings.Web;
using AIVES.Business.Interfaces;
using AIVES.Business.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TV5.Verify;

public static class PreviewHost
{
    public static WebApplication Build(string url, PreviewData? data = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(PreviewHost).Assembly.GetName().Name,
            EnvironmentName = "Development"
        });
        builder.WebHost.UseUrls(url);
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton(data ?? new PreviewData());
        builder.Services.AddSingleton<IAssignmentService, PreviewAssignments>();
        builder.Services.AddSingleton<ISubjectService, PreviewSubjects>();
        builder.Services.AddSingleton<ILanguageConfigService, PreviewLanguages>();
        builder.Services.AddScoped<IQuestionService>(sp => new QuestionService(
            sp.GetRequiredService<PreviewData>().Db.QuestionRepo, sp.GetRequiredService<IAssignmentService>()));
        builder.Services.AddAuthentication("TV5Preview").AddScheme<AuthenticationSchemeOptions, PreviewAuthentication>("TV5Preview", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddRazorPages().AddApplicationPart(typeof(AIVES.Ass2.Razor.Infrastructure.LiveHub).Assembly);
        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapRazorPages();
        app.MapGet("/Auth/AccessDenied", () => Results.Content("Không có quyền", "text/plain; charset=utf-8", statusCode: 403));
        app.MapGet("/Auth/Login", () => Results.Content("Preview: choose /__preview/login/admin or /__preview/login/an", "text/plain"));
        app.MapGet("/__preview/login/{role}", (string role, HttpContext context, PreviewData seed) =>
        {
            var id = role == "admin" ? seed.AdminId : role == "an" ? seed.AnId : seed.BinhId;
            context.Response.Cookies.Append("tv5-preview-user", id.ToString(), new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax });
            return Results.Redirect(role == "admin" ? "/Subjects" : "/MySubjects");
        });
        return app;
    }

    public static async Task Main(string[] args)
    {
        if (args.Contains("--examples"))
        {
            foreach (var (pool, students, perStudent) in new[] { (5, 3, 2), (8, 4, 3) })
            {
                Console.WriteLine($"Pool={pool}, students={students}, perStudent={perStudent}, seed=7");
                var sets = QuestionAllocator.Allocate(Enumerable.Range(1, pool).ToArray(), students, perStudent, new Random(7));
                for (var i = 0; i < sets.Count; i++) Console.WriteLine($"Student {i + 1}: {string.Join(", ", sets[i])}");
                Console.WriteLine("Usage: " + string.Join(", ", Enumerable.Range(1, pool).Select(id => $"Q{id}={sets.Sum(s => s.Contains(id) ? 1 : 0)}")));
            }
            return;
        }
        Console.WriteLine("TEMPORARY TV5 PREVIEW: fake TV2/TV3 dependencies; no SQL or production login.");
        var app = Build(args.FirstOrDefault() ?? "http://127.0.0.1:5085");
        Console.WriteLine("Open http://127.0.0.1:5085/__preview/login/admin or /__preview/login/an");
        await app.RunAsync();
    }
}

internal sealed class PreviewAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder, PreviewData data) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var raw = Request.Headers["X-TV5-Test-User"].FirstOrDefault() ?? Request.Cookies["tv5-preview-user"];
        if (!int.TryParse(raw, out var id)) return Task.FromResult(AuthenticateResult.NoResult());
        var account = data.Db.Accounts.SingleOrDefault(a => a.Id == id);
        if (account is not { IsActive: true }) return Task.FromResult(AuthenticateResult.NoResult());
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Role, account.Role), new Claim(ClaimTypes.Name, account.FullName) }, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Redirect("/Auth/Login");
        return Task.CompletedTask;
    }
    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.Redirect("/Auth/AccessDenied");
        return Task.CompletedTask;
    }
}
