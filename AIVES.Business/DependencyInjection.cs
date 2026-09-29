using AIVES.Business.Interfaces;
using AIVES.Business.Security;
using AIVES.Business.Services;
using AIVES.DataAccess;
using Microsoft.Extensions.DependencyInjection;

namespace AIVES.Business;

public static class DependencyInjection
{
    // Tầng Presentation chỉ gọi hàm này; nó không cần biết tầng DataAccess tồn tại
    public static IServiceCollection AddBusinessLayer(this IServiceCollection services, string connectionString)
    {
        services.AddDataAccessLayer(connectionString);

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ISubjectService, SubjectService>();
        services.AddScoped<IAssignmentService, AssignmentService>();
        services.AddScoped<ILanguageConfigService, LanguageConfigService>();

        return services;
    }
}
