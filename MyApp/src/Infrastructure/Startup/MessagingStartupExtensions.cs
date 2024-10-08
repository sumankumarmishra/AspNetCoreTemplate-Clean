﻿using MassTransit;
using MyApp.Application;
using MyApp.Application.Infrastructure.Abstractions;
using MyApp.Infrastructure.Messaging;
using MyApp.Utilities.Settings;

namespace MyApp.Infrastructure.Startup;

public static class MessagingStartupExtensions
{
    public static IServiceCollection AddCustomMessaging(this IServiceCollection services, IConfiguration configuration)
        => services.AddCustomMessaging(configuration, x =>
        {
            x.AddConsumersFromNamespaceContaining<IApplicationAssemblyMarker>();
            x.AddConsumersFromNamespaceContaining<IInfrastructureAssemblyMarker>();
        });

    public static IServiceCollection AddCustomMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator> setupConsumers)
    {
        services.AddCustomSettings<MessagingSettings>(configuration);

        services.AddOptions<MassTransitHostOptions>()
            .Configure(options =>
            {
                options.WaitUntilStarted = true;
                options.StartTimeout = TimeSpan.FromSeconds(30);
                options.StopTimeout = TimeSpan.FromSeconds(30);
            });

        services.AddOptions<RabbitMqTransportOptions>()
            .BindConfiguration(MessagingSettings.SectionName);

        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            setupConsumers(x);

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });

            x.AddConfigureEndpointsCallback((name, cfg) =>
            {
                cfg.UseMessageRetry(r => r.Exponential(3, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(3)));
            });
        });

        services.AddScoped<IMessageProducer, MessageProducer>();

        return services;
    }
}
