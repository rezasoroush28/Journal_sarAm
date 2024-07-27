using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Models;

public class BotHostedService : IHostedService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IUserService _userService;
    private readonly IJournalService _journalService;
    private CancellationTokenSource _cts;

    public BotHostedService(ITelegramBotClient botClient, IUserService userService, IJournalService journalService)
    {
        _botClient = botClient;
        _userService = userService;
        _journalService = journalService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = new CancellationTokenSource();
        _botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() }, _cts.Token);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();
        return Task.CompletedTask;
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message!.Type != MessageType.Text)
            return;

        var chatId = update.Message.Chat.Id;
        var messageText = update.Message.Text;

        var user = await _userService.GetUserByTelegramIdAsync(chatId.ToString());
        if (messageText.Equals("/start", StringComparison.OrdinalIgnoreCase))
        {
            if (user == null)
            {
                await botClient.SendTextMessageAsync(chatId, "Welcome! Please tell me your name.");
            }
            else
            {
                await ShowMainMenu(botClient, chatId, cancellationToken);
            }
        }
        else if (user == null)
        {
            user = new TelegramBot.Models.User { TelegramId = chatId.ToString(), Name = messageText };
            await _userService.AddUserAsync(user);
            await ShowMainMenu(botClient, chatId, cancellationToken);
        }
        else if (messageText.Equals("Add Journal", StringComparison.OrdinalIgnoreCase))
        {
            await botClient.SendTextMessageAsync(chatId, "Please write your journal entry.");
        }
        else if (messageText.Equals("Show Journals", StringComparison.OrdinalIgnoreCase))
        {
            await ShowJournalsMenu(botClient, chatId, cancellationToken);
        }
        else
        {
            var journal = new Journal
            {
                PersianJournal = messageText,
                CreatedDate = DateTime.UtcNow,
                UserId = user.Id
            };
            await _journalService.AddJournalAsync(journal);
            await botClient.SendTextMessageAsync(chatId, "Journal added successfully.");
        }
    }

    private async Task ShowMainMenu(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "Add Journal", "Show Journals" }
        })
        {
            ResizeKeyboard = true
        };

        await botClient.SendTextMessageAsync(chatId, "Choose an option:", replyMarkup: keyboard, cancellationToken: cancellationToken);
    }

    private async Task ShowJournalsMenu(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "Positive Journals", "Negative Journals", "All Journals" }
        })
        {
            ResizeKeyboard = true
        };

        await botClient.SendTextMessageAsync(chatId, "Choose a journal type to view:", replyMarkup: keyboard, cancellationToken: cancellationToken);
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine(exception);
        return Task.CompletedTask;
    }
}
