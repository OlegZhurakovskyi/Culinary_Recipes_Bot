using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddHttpClient();
        services.AddSingleton<ITelegramBotClient>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            return new TelegramBotClient(configuration["TelegramBot:Token"]);
        });
        services.AddHostedService<CulinaryBot>();
    })
    .Build();

await host.RunAsync();
