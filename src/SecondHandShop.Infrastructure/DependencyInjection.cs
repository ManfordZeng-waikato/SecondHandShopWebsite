using Amazon.Runtime;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Application.Abstractions.ImageProcessing;
using SecondHandShop.Application.Abstractions.Messaging;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Abstractions.Security;
using SecondHandShop.Application.Abstractions.Storage;
using SecondHandShop.Application.UseCases.Catalog;
using SecondHandShop.Application.UseCases.Customers;
using SecondHandShop.Application.UseCases.Inquiries;
using SecondHandShop.Application.UseCases.Sales;
using SecondHandShop.Infrastructure.Persistence;
using SecondHandShop.Infrastructure.Persistence.Repositories;
using SecondHandShop.Infrastructure.Services;

namespace SecondHandShop.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=SecondHandShopDb;Username=postgres;Password=postgres;";
        var smtpOptions = SmtpEmailOptions.FromConfiguration(configuration);
        var r2Options = R2Options.FromConfiguration(configuration);
        var removeBgOptions = RemoveBgOptions.FromConfiguration(configuration);
        var cloudflareTurnstileOptions = CloudflareTurnstileOptions.FromConfiguration(configuration);

        services.AddDbContext<SecondHandShopDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<SecondHandShopDbContext>());
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductImageRepository, ProductImageRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IInquiryRepository, InquiryRepository>();
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddScoped<IProductSaleRepository, ProductSaleRepository>();
        services.AddScoped<IAdminCatalogService, AdminCatalogService>();
        services.AddScoped<IAdminSaleService, AdminSaleService>();
        services.AddScoped<ICustomerResolutionService, CustomerResolutionService>();
        services.AddScoped<IAdminCustomerService, AdminCustomerService>();
        services.AddScoped<IInquiryService, InquiryService>();
        services.AddSingleton<IPasswordHasher, PasswordHasherService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton(r2Options);
        // AmazonS3Client is thread-safe and designed for singleton reuse — sharing the HTTP
        // connection pool avoids rebuilding TLS sessions on every presign/delete call.
        services.AddSingleton<IAmazonS3>(_ =>
        {
            r2Options.Validate();
            var credentials = new BasicAWSCredentials(r2Options.AccessKeyId, r2Options.SecretAccessKey);
            var config = new AmazonS3Config
            {
                ServiceURL = $"https://{r2Options.AccountId}.r2.cloudflarestorage.com",
                ForcePathStyle = true
            };
            return new AmazonS3Client(credentials, config);
        });
        services.AddScoped<IObjectStorageService, R2ObjectStorageService>();
        services.AddSingleton(removeBgOptions);
        services.AddHttpClient<IBackgroundRemovalService, RemoveBgService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(removeBgOptions.TimeoutSeconds);
        });
        services.AddScoped<IClock, SystemClock>();
        services.AddSingleton(smtpOptions);
        services.AddSingleton(cloudflareTurnstileOptions);
        services.AddHttpClient<ITurnstileValidator, TurnstileValidator>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddScoped<NoOpEmailSender>();
        services.AddScoped<SmtpEmailSender>();
        services.AddScoped<IEmailSender>(provider =>
            smtpOptions.Enabled
                ? provider.GetRequiredService<SmtpEmailSender>()
                : provider.GetRequiredService<NoOpEmailSender>());

        // Singleton signal bridges the HTTP request pipeline and the background dispatcher.
        services.AddSingleton<IInquiryDispatchSignal, InquiryDispatchSignal>();
        services.AddHostedService<InquiryEmailDispatcherService>();

        return services;
    }
}
