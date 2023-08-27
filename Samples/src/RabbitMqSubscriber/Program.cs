﻿using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMqSubscriber.Configurations;
using RabbitMqSubscriber.Subscribers;
using Serilog;
using Shared.RabbitMQ.EventBus;
using Shared.RabbitMQ.Events;
using Shared.RabbitMq.Extensions;
using Shared.RabbitMQ.Extensions;
using TibiaEnemyOtherCharactersFinder.Infrastructure.Configuration;

namespace RabbitMqSubscriber;

public class Program
{
    private static async Task Main(string[] args)
    {
        try
        {
            var host = CreateHostBuilder(args);

            Log.Information("Starting application");
            await host.StartAsync();

            var service = ActivatorUtilities.CreateInstance<TibiaSubscriber>(host.Services);

            service.Subscribe();

            await host.StopAsync();
            Log.Information("Ending application properly");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static IHost CreateHostBuilder(string [] args)
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                // config.Properties["reloadConfigOnChange"] = true;
                config
                    // .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    // .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables();
            })
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureContainer<ContainerBuilder>(builder =>
            {
                builder.RegisterModule<AutofacModule>();
                builder.RegisterEventSubscribers();
            })
            .ConfigureServices((context, services) =>
            {
                services
                    .AddSingleton<TibiaSubscriber>()
                    // .AddSingleton<IEventSubscriber, MergeTwoCharactersEventSubscriber>()
                    .AddSerilog(context.Configuration, Assembly.GetExecutingAssembly().GetName().Name)
                    .AddTibiaDbContext(context.Configuration)
                    .AddRabbitMqSubscriber(context.Configuration);
            })
            .UseSerilog()
            .Build();

        return host;
    }
}