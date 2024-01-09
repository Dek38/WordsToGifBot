using System.Text;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Text.Json;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotTest
{
    internal class Program
    {
        private const string GIPHY_URL = "https://api.giphy.com/v1/gifs/translate?api_key=";
        private static TelegramBotClient bot = new TelegramBotClient(Configs.TELEGRAM_BOT_TOKEN);
        private readonly static HttpClient httpClient = new HttpClient();

        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Запущен бот " + bot.GetMeAsync().Result.FirstName);

                var cts = new CancellationTokenSource();
                var cancellationToken = cts.Token;
                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = [UpdateType.CallbackQuery, UpdateType.Message]
                };
                bot.StartReceiving(
                    HandleUpdateAsync,
                    HandleErrorAsync,
                    receiverOptions,
                    cancellationToken
                );
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));

            async Task requestMethod (Chat chatId, string message)  
            {
                await GiphyRequest(botClient, chatId, message, cancellationToken);
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(text: "Повторим прошлый запрос?", callbackData: message)
                    }
                });

                Message sentMessage = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Вот такой ответ",
                    replyMarkup: inlineKeyboard,
                    cancellationToken: cancellationToken);
            };

            if (update.Type == UpdateType.Message)
            {
                var message = update.Message;
                if (message != null)
                {
                    if (message.Text?.ToLower() == "/start")
                    {
                        await botClient.SendTextMessageAsync(message.Chat, $"Что ты хочешь увидеть, {(string.IsNullOrWhiteSpace(message.From?.FirstName) ? "пришелец" : message.From.FirstName)}?");
                    }
                    else if (message.Text != null)
                    {
                        await requestMethod(message.Chat, message.Text);
                    }
                }
            }
            else if (update.Type == UpdateType.CallbackQuery)
            {
                var message = update.CallbackQuery?.Data;
                if ((message != null) && (update.CallbackQuery?.Message?.Chat != null))
                {
                    await requestMethod(update.CallbackQuery.Message.Chat, message);
                }
            }
        }

        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }

        public static async Task GiphyRequest(ITelegramBotClient botClient, Chat chatId, string? searchPhrase, CancellationToken cancellationToken)
        {
            if ((searchPhrase == null) || (searchPhrase == ""))
            {
                return;
            }
            StringBuilder sb = new StringBuilder(GIPHY_URL + Configs.GIPHY_API_KEY + "&s=" + searchPhrase + "&weirdness=0");
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, sb.ToString());
            HttpResponseMessage? giphyHTTPResponse = null;
            try
            {
                request.Headers.Add("user-agent", "Other");
                giphyHTTPResponse = await httpClient.SendAsync(request);
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Не отправили реквест к GIPHY");
                await botClient.SendTextMessageAsync(chatId, "Что-то пошло не так, попробуем ещё раз?");
                return;
            }
            if ((giphyHTTPResponse != null) && giphyHTTPResponse.IsSuccessStatusCode)
            {
                try
                {
                    string str = await giphyHTTPResponse.Content.ReadAsStringAsync();
                    GiphyResponse? giphyDecodedResponse = JsonSerializer.Deserialize<GiphyResponse>(str);
                    string? url = giphyDecodedResponse?.data?.images?.original?.url;
                    Console.WriteLine($"URL = {url}");          
                    if (url != null)
                    {
                        await botClient.SendAnimationAsync(
                            chatId: chatId,
                            animation: InputFile.FromUri(url),
                            cancellationToken: cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("Не удалось извлечь данные из JSON");
                    await botClient.SendTextMessageAsync(chatId, "Что-то пошло не так, попробуем ещё раз?"); ;
                }
            }
            else if ((giphyHTTPResponse != null) && !giphyHTTPResponse.IsSuccessStatusCode)
            {
                await botClient.SendTextMessageAsync(chatId, "Сервер болеет, попозже приходи");
                Console.WriteLine($"Не удалось получить данные с сервера, статус {giphyHTTPResponse.StatusCode}");
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "Какой-то необычный у тебя запрос, сервер был обеcкуражен и ничего не выдал");
                Console.WriteLine("Не удалось получить данные с сервера");
            }
        }
    }
}
