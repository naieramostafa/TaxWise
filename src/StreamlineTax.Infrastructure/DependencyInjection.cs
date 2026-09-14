using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StreamlineTax.Application.Common.Interfaces;
using StreamlineTax.Infrastructure.Persistence;
using StreamlineTax.Infrastructure.Services;
using StreamlineTax.Infrastructure.Services.BackgroundJobs;

namespace StreamlineTax.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var ocrApiKey = configuration["OCR:ApiKey"]
            ?? configuration["OcrSpace:ApiKey"]
            ?? Environment.GetEnvironmentVariable("OCRSPACE_API_KEY");

        if (!string.IsNullOrEmpty(ocrApiKey))
        {
            services.AddHttpClient<IOcrService, OcrSpaceService>();
        }
        else
        {
            services.AddScoped<IOcrService, TesseractOcrService>();
        }
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=streamline_tax;Username=postgres;Password=postgres";

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, b => b.MigrationsAssembly("StreamlineTax.Api")));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        });

        services.AddHangfire(config =>
            config.UsePostgreSqlStorage(connectionString));

        services.AddHangfireServer();

        services.AddScoped<IFileStorageService, S3FileStorageService>();
        services.AddScoped<ITaxCalculationService, TaxCalculationService>();
        services.AddScoped<ReceiptProcessingJob>();
        services.AddScoped<IBackgroundJobService, BackgroundJobService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<ITaxPeriodService, TaxPeriodService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ITaxPeriodGuard, TaxPeriodGuard>();
        services.AddScoped<WeeklyCategorizationJob>();

        return services;
    }
}
