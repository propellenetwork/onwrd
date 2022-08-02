﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Onwrd.EntityFrameworkCore.Internal;
using Onwrd.EntityFrameworkCore.Internal.Migrations;

namespace Onwrd.EntityFrameworkCore
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOutboxedDbContext<TContext>(
            this IServiceCollection serviceCollection,
            Action<IServiceProvider, DbContextOptionsBuilder> optionsAction,
            Action<OutboxingConfiguration> outboxingConfiguration,
            ServiceLifetime contextLifetime = ServiceLifetime.Scoped,
            ServiceLifetime optionsLifetime = ServiceLifetime.Scoped)
            where TContext : DbContext
        {
            // Core onwrd services
            var config = new OutboxingConfiguration();
            outboxingConfiguration(config);

            serviceCollection.AddSingleton(config);

            if (!serviceCollection.Any(x => x.ServiceType == config.OnwardProcessorType))
            {
                serviceCollection.AddTransient(typeof(IOnwardProcessor), config.OnwardProcessorType);
            }

            serviceCollection.AddTransient<SaveChangesInterceptor>();
            serviceCollection.AddTransient<OnConnectingInterceptor>();
            serviceCollection.AddSingleton<RunOnce>();
            serviceCollection.AddTransient<Startup>();

            void optionsActionOverride(IServiceProvider serviceProvider, DbContextOptionsBuilder builder)
            {
                optionsAction(serviceProvider, builder);
                builder.AddOutboxing();
                builder.AddInterceptors(
                    serviceProvider.GetRequiredService<SaveChangesInterceptor>(),
                    serviceProvider.GetRequiredService<OnConnectingInterceptor>());
            }

            // Migration services
            serviceCollection.AddDbContext<TContext>(
                optionsActionOverride,
                contextLifetime,
                optionsLifetime);

            void migrationOptionsActionOverride(IServiceProvider serviceProvider, DbContextOptionsBuilder builder)
            {
                optionsAction(serviceProvider, builder);
                builder.AddOutboxing();
                builder.ReplaceService<IMigrator, OnwrdMigrator>();
            }

            serviceCollection.AddDbContext<MigrationContext>(
                migrationOptionsActionOverride,
                ServiceLifetime.Transient,
                ServiceLifetime.Transient);

            return serviceCollection;
        }
    }
}
