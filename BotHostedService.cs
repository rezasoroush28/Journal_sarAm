//using Microsoft.Extensions.Hosting;
//using Telegram.Bot;
//using Telegram.Bot.Polling;
//using Telegram.Bot.Types;
//using Telegram.Bot.Types.Enums;
//using Telegram.Bot.Types.ReplyMarkups;
//using TelegramBot.Models;

//public class BotHostedService : IHostedService
//{
//    private readonly ITelegramBotClient _botClient;
//    private CancellationTokenSource _cts;

//    public BotHostedService(ITelegramBotClient botClient)
//    {
//        _botClient = botClient;
//    }

//    public Task StartAsync(CancellationToken cancellationToken)
//    {
//        _cts = new CancellationTokenSource();
//        _botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() }, _cts.Token);
//        return Task.CompletedTask;
//    }

//    public Task StopAsync(CancellationToken cancellationToken)
//    {
//        _cts.Cancel();
//        return Task.CompletedTask;
//    }

//    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
//    {
//        if (update.Type != UpdateType.Message || update.Message!.Type != MessageType.Text)
//            return;

//        var chatId = update.Message.Chat.Id;
//        await botClient.SendTextMessageAsync(chatId, $"Your Telegram ID is: {chatId}", cancellationToken: cancellationToken);
//    }

//    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
//    {
//        Console.WriteLine(exception);
//        return Task.CompletedTask;
//    }
//}
