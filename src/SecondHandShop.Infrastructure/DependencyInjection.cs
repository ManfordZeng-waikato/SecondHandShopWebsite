using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Application.Abstractions.ImageProcessing;
using SecondHandShop.Application.Abstractions.Messaging;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Abstractions.Storage;
using SecondHandShop.Application.UseCases.Catalog;
using SecondHandShop.Application.UseCases.Inquiries;
using SecondHandShop.Infrastructure.Persistence;
using SecondHandShop.Infrastructure.Persistence.Repositories;
using SecondHandShop.Infrastructure.Services;

namespace SecondHandShop.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=SecondHandShopDb;Trusted_Connection=True;TrustServerCertificate=True;";
        var smtpOptions = SmtpEmailOptions.FromConfiguration(configuration);
        var r2Options = R2Options.FromConfiguration(configuration);
        var removeBgOptions = RemoveBgOptions.FromConfiguration(configuration);

        services.AddDbContext<SecondHandShopDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<SecondHandShopDbContext>());
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductImageRepository, ProductImageRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IInquiryRepository, InquiryRepository>();
        services.AddScoped<IAdminCatalogService, AdminCatalogService>();
        services.AddScoped<IInquiryService, InquiryService>();
        services.AddSingleton(r2Options);
        services.AddScoped<IObjectStorageService, R2ObjectStorageService>();
        services.AddSingleton(removeBgOptions);
        services.AddHttpClient<IBackgroundRemovalService, RemoveBgService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(removeBgOptions.TimeoutSeconds);
        });
        services.AddScoped<IClock, SystemClock>();
        services.AddSingleton(smtpOptions);
        services.AddScoped<NoOpEmailSender>();
        services.AddScoped<SmtpEmailSender>();
        services.AddScoped<IEmailSender>(provider =>
            smtpOptions.Enabled
                ? provider.GetRequiredService<SmtpEmailSender>()
                : provider.GetRequiredService<NoOpEmailSender>());

        return services;
    }
}
