using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bots.Extensions.Polling;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Newtonsoft.Json;
using System.Text;

public class CulinaryBot : IHostedService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public CulinaryBot(ITelegramBotClient botClient, IConfiguration configuration, HttpClient httpClient)
    {
        _botClient = botClient;
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new UpdateType[] { }
        };

        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken
        );

        Console.WriteLine("Bot started...");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Bot stopped...");
        return Task.CompletedTask;
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.Message && update.Message.Type == MessageType.Text)
        {
            var chatId = update.Message.Chat.Id;
            var messageText = update.Message.Text;

            if (messageText.StartsWith("/start"))
            {
                await _botClient.SendTextMessageAsync(chatId, "Привіт! Я кулінарний бот, введи команду /help , щоб дізнатися про команди");
            }
            else if (messageText.StartsWith("/help"))
            {
                await HandleHelpCommand(chatId);
            }
            else if (messageText.StartsWith("/search"))
            {
                var query = messageText.Replace("/search", "Apple").Trim();
                await HandleSearchRecipes(chatId, query);
            }
            else if (messageText.StartsWith("/random"))
            {
                await HandleRandomRecipe(chatId);
            }
            else if (messageText.StartsWith("/joke"))
            {
                await HandleRandomJoke(chatId);
            }
            else if (messageText.StartsWith("/fact"))
            {
                await HandleRandomFact(chatId);
            }
            else if (messageText.StartsWith("/analyze"))
            {
                var ingredients = messageText.Replace("/analyze", "").Trim();
                await HandleAnalyzeRecipe(chatId, ingredients);
            }
        }
    }

    private async Task HandleHelpCommand(long chatId)
    {
        var helpMessage = @"
Команди:
- /start: Почати роботу з ботом
- /help: Показати цей список команд
- /search <назва>: Знайти рецепти за назвою
- /random: Отримати випадковий рецепт
- /joke: Отримати випадковий жарт про їжу
- /fact: Отримати випадковий факт про їжу
- /analyze <інгредієнти>: Проаналізувати рецепт
";
        await _botClient.SendTextMessageAsync(chatId, helpMessage);
    }

    private async Task HandleSearchRecipes(long chatId, string query)
    {
        var response = await _httpClient.GetStringAsync($"{_configuration["TelegramBot:ApiBaseUrl"]}/Recipes/search?query={query}");
        var recipes = JsonConvert.DeserializeObject<List<Recipe>>(response);

        if (recipes != null && recipes.Any())
        {
            var formattedResponse = new StringBuilder();

            foreach (var recipe in recipes)
            {
                formattedResponse.AppendLine($"*ID:* {recipe.Id}")
                                 .AppendLine($"*Title:* {recipe.Title}")
                                 .AppendLine($"*Image:* {recipe.Image}")
                                 .AppendLine($"*Source URL:* {recipe.SourceUrl}")
                                 .AppendLine()
                                 .AppendLine("---")
                                 .AppendLine();
            }

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: formattedResponse.ToString(),
                parseMode: ParseMode.Markdown
            );
        }
        else
        {
            await _botClient.SendTextMessageAsync(chatId, "Не знайдено жодного рецепту за вашим запитом.");
        }
    }

    private async Task HandleRandomRecipe(long chatId)
    {
        var response = await _httpClient.GetStringAsync($"{_configuration["TelegramBot:ApiBaseUrl"]}/Recipes/random");
        var recipe = JsonConvert.DeserializeObject<Recipe>(response);

        if (recipe != null)
        {
            var formattedResponse = new StringBuilder();

            formattedResponse.AppendLine($"*ID:* {recipe.Id}")
                             .AppendLine($"*Title:* {recipe.Title}")
                             .AppendLine($"*Image:* {recipe.Image}")
                             .AppendLine($"*Source URL:* {recipe.SourceUrl}")
                             .AppendLine();

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: formattedResponse.ToString(),
                parseMode: ParseMode.Markdown
            );
        }
        else
        {
            await _botClient.SendTextMessageAsync(chatId, "Не вдалося отримати рандомний рецепт. Спробуйте ще раз пізніше.");
        }
    }

    private async Task HandleRandomJoke(long chatId)
    {
        var response = await _httpClient.GetStringAsync($"{_configuration["TelegramBot:ApiBaseUrl"]}/Recipes/joke");
        await _botClient.SendTextMessageAsync(chatId, response);
    }

    private async Task HandleRandomFact(long chatId)
    {
        var response = await _httpClient.GetStringAsync($"{_configuration["TelegramBot:ApiBaseUrl"]}/Recipes/fact");
        await _botClient.SendTextMessageAsync(chatId, response);
    }

    private async Task HandleAnalyzeRecipe(long chatId, string ingredients)
    {
        var content = new StringContent($"{{\"ingredients\": \"{ingredients}\"}}", System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{_configuration["TelegramBot:ApiBaseUrl"]}/Recipes/analyze", content);
        var result = await response.Content.ReadAsStringAsync();
        await _botClient.SendTextMessageAsync(chatId, result);
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine(exception.Message);
        return Task.CompletedTask;
    }

   
}
