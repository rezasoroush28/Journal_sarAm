using System.Collections.Concurrent;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Models;
using Telegram.Bot.Types;

public class TelegramBotUpdateHandler : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ConcurrentDictionary<long, bool> _waitingForJournal;

    public TelegramBotUpdateHandler(ITelegramBotClient botClient, IServiceScopeFactory serviceScopeFactory)
    {
        _botClient = botClient;
        _serviceScopeFactory = serviceScopeFactory;
        _waitingForJournal = new ConcurrentDictionary<long, bool>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ReceiverOptions receiverOptions = new() { AllowedUpdates = Array.Empty<UpdateType>() };

        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            stoppingToken
        );

        await Task.CompletedTask;
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.Message && update.Message!.Type == MessageType.Text)
        {
            var chatId = update.Message.Chat.Id;
            var messageText = update.Message.Text;

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                var journalService = scope.ServiceProvider.GetRequiredService<IJournalService>();

                var user = await userService.GetUserByTelegramIdAsync(chatId.ToString());

                if (messageText.Equals("/start", StringComparison.OrdinalIgnoreCase))
                {
                    if (user == null)
                    {
                        await _botClient.SendTextMessageAsync(chatId, "Welcome! Please tell me your name.", cancellationToken: cancellationToken);
                        _waitingForJournal[chatId] = false; // Waiting for name
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(chatId, $"Hello {user.Name}, welcome back!", cancellationToken: cancellationToken);
                        await SendMainMenu(chatId, cancellationToken);
                    }
                }
                else if (_waitingForJournal.TryGetValue(chatId, out var waitingForName) && !waitingForName)
                {
                    // User is sending their name
                    var newUser = new User
                    {
                        TelegramId = chatId.ToString(),
                        Name = messageText
                    };
                    await userService.AddUserAsync(newUser);
                    _waitingForJournal.TryRemove(chatId, out _);

                    await _botClient.SendTextMessageAsync(chatId, $"Thank you, {newUser.Name}! Your name has been saved.", cancellationToken: cancellationToken);
                    await SendMainMenu(chatId, cancellationToken);
                }
                else if (_waitingForJournal.TryGetValue(chatId, out var waitingForJournal) && waitingForJournal)
                {
                    // User is sending their journal
                    if (user != null)
                    {
                        var newJournal = new Journal
                        {
                            PersianJournal = messageText,
                            UserId = user.Id,
                            CreatedDate = DateTime.UtcNow
                        };
                        await journalService.AddJournalAsync(newJournal);
                        _waitingForJournal.TryRemove(chatId, out _);

                        // Add call to OpenAI service to analyze and update the journal here
                        newJournal = await journalService.AnalyzeAndSaveJournalAsync(newJournal);

                        var responseMessage = $"Your journal has been saved!\nTranslation: {newJournal.Translation}\nEmotional Analysis: {newJournal.EmotionalAnalysis}\nPolarity: {newJournal.Polarity}\nTopic: {newJournal.JournalTopic}";
                        await _botClient.SendTextMessageAsync(chatId, responseMessage, cancellationToken: cancellationToken);
                    }
                }
                else if (messageText.Equals("Add New Journal", StringComparison.OrdinalIgnoreCase))
                {
                    // User wants to add a new journal
                    if (user != null)
                    {
                        await _botClient.SendTextMessageAsync(chatId, "Please send your journal entry.", cancellationToken: cancellationToken);
                        _waitingForJournal[chatId] = true; // Waiting for journal
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(chatId, "Please use /start to begin.", cancellationToken: cancellationToken);
                    }
                }
            }
        }
    }

    private async Task SendMainMenu(long chatId, CancellationToken cancellationToken)
    {
        var replyKeyboardMarkup = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "Add New Journal" }
        })
        {
            ResizeKeyboard = true
        };

        await _botClient.SendTextMessageAsync(chatId, "Choose an option:", replyMarkup: replyKeyboardMarkup, cancellationToken: cancellationToken);
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine(exception);
        return Task.CompletedTask;
    }
}
