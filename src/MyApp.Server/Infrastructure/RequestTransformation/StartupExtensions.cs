﻿using System.Reflection;
using MyApp.Server.Utilities;
using MediatR;

namespace MyApp.Server.Infrastructure.RequestTransformation;

public static class StartupExtensions
{
    public static IServiceCollection AddCustomRequestTransformers(this IServiceCollection services)
    {
        services.AddTransformers();

        services.Decorate<ISender, SenderTransformationDecorator>();

        return services;
    }

    private static void AddTransformers(this IServiceCollection services)
    {
        var transformers = Assembly.GetExecutingAssembly()
                .DefinedTypes
                .Where(t => t.IsClass && t.ImplementedInterfaces.Any(i => i.Name.Equals(typeof(IRequestTransformer<>).Name)))
                .Select(t => new
                {
                    Implementation = t,
                    Interface = t.ImplementedInterfaces.FirstOrDefault() ?? throw new InvalidOperationException($"Could not find interface for transformer {t.Name}!"),
                })
                .ToArray();

        foreach (var transformer in transformers)
        {
            services.AddSingleton(transformer.Interface, transformer.Implementation);
        }
    }
}
