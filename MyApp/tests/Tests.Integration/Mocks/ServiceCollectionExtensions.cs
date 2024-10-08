﻿namespace MyApp.Tests.Integration.Mocks;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection ReplaceWithMock<TService>(
        this IServiceCollection services,
        MockBag mockBag,
        MockBehavior mockBehavior = MockBehavior.Strict) 
        where TService : class
    {
        var mock = new Mock<TService>(mockBehavior);
        services.ReplaceService(mock.Object);
        mockBag.Add(mock);
        return services;
    }

    public static void ReplaceService<T>(this IServiceCollection services, T service) where T : class
    {
        var serviceType = typeof(T);
        var descriptor = services.FirstOrDefault(descriptor => descriptor.ServiceType == serviceType)
            ?? throw new Exception($"Unable to remove service of type {serviceType.Name} from service collection. No such service was found.");
        services.Remove(descriptor);
        services.AddSingleton(service);
    }
}
