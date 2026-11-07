using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using AIVES.Business.Security;
using AIVES.Business.Services;
using AIVES.DataAccess;
using Microsoft.Extensions.DependencyInjection;

namespace AIVES.Business;

public static class DependencyInjection
{
    // Tầng Web chỉ gọi một dòng hàm này; nó không biết DataAccess tồn tại
    public static IServiceCollection AddBusinessLayer(
        this IServiceCollection services,
        string connectionString,
        AiOptions? ai = null,
        TtsOptions? tts = null)
    {
        services.AddDataAccessLayer(connectionString);

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ISubjectService, SubjectService>();
        services.AddScoped<IAssignmentService, AssignmentService>();
        services.AddScoped<ILanguageConfigService, LanguageConfigService>();
        services.AddScoped<IQuestionService, QuestionService>();
        services.AddScoped<IExamService, ExamService>();

        // Ass3
        services.AddSingleton(ai ?? new AiOptions());
        services.AddSingleton(tts ?? new TtsOptions());
        services.AddSingleton<ITextToSpeech, VieNeuTextToSpeech>();
        services.AddSingleton<IFollowUpGenerator, FollowUpGenerator>();
        services.AddScoped<IInterviewService, InterviewService>();
        services.AddScoped<IVoiceService, VoiceService>();

        return services;
    }
}
