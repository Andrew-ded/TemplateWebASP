using Microsoft.Extensions.DependencyInjection;

namespace Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // TODO: Регистрация сервисов Application слоя
        // services.AddScoped<ISampleService, SampleService>();

        return services;
    }
}
