using System.Collections.Concurrent;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Models;
using Telegram.Bot.Types;
using System.Text.Json;

public class TelegramBotUpdateHandler : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ConcurrentDictionary<long, bool> _waitingForJournal;
    private readonly ConcurrentDictionary<long, List<KeyValuePair<string, Journal>>> _userJournalCache;

    public TelegramBotUpdateHandler(ITelegramBotClient botClient, IServiceScopeFactory serviceScopeFactory)
    {
        _botClient = botClient;
        _serviceScopeFactory = serviceScopeFactory;
        _waitingForJournal = new ConcurrentDictionary<long, bool>();
        _userJournalCache = new ConcurrentDictionary<long, List<KeyValuePair<string, Journal>>>();
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
                    var newUser = new TelegramBot.Models.User
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

                        // Cache the new journal
                        if (!_userJournalCache.ContainsKey(chatId))
                        {
                            _userJournalCache[chatId] = new List<KeyValuePair<string, Journal>>();
                        }
                        _userJournalCache[chatId].Add(new KeyValuePair<string, Journal>(newJournal.JournalTopic, newJournal));

                        if (newJournal.EmotionalAnalysis != null)
                        {
                            var responseMessage = $"Your journal has been saved!\nTranslation: {newJournal.Translation}\nEmotional Analysis: {newJournal.EmotionalAnalysis}\nPolarity: {newJournal.Polarity}\nTopic: {newJournal.JournalTopic}";
                            await _botClient.SendTextMessageAsync(chatId, responseMessage, cancellationToken: cancellationToken);
                        }
                        else
                        {
                            var responseMessage = $"Your journal has been saved! : {newJournal.RawJson}";
                            await _botClient.SendTextMessageAsync(chatId, responseMessage, cancellationToken: cancellationToken);
                        }
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
                else if (messageText.Equals("Show My Journals", StringComparison.OrdinalIgnoreCase))
                {
                    // Fetch the user's journals
                    if (user != null)
                    {
                        // Load journals from the service and cache them
                        var journals = await journalService.GetAllJournalsByUserIdAsync(user.Id);
                        if (journals.Any())
                        {
                            _userJournalCache[chatId] = journals
                                .Where(j => !string.IsNullOrEmpty(j.JournalTopic))
                                .Select(j => new KeyValuePair<string, Journal>(j.JournalTopic, j))
                                .ToList();

                            await SendJournalTopics(chatId, cancellationToken);
                        }
                        else
                        {
                            await _botClient.SendTextMessageAsync(chatId, "You have no journals saved.", cancellationToken: cancellationToken);
                        }
                    }
                }
            }
        }
        else if (update.Type == UpdateType.CallbackQuery)
        {
            await HandleCallbackQueryAsync(botClient, update.CallbackQuery, cancellationToken);
        }
    }

    private async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var chatId = callbackQuery.Message.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        var data = callbackQuery.Data;

        // Delete the previous message to avoid clutter
        await _botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);

        if (data.StartsWith("journal_"))
        {
            var journalId = int.Parse(data.Split('_')[1]);

            // Retrieve the journal from the cache using the journalId
            if (_userJournalCache.TryGetValue(chatId, out var journalList))
            {
                var journal = journalList.FirstOrDefault(j => j.Value.Id == journalId).Value;
                if (journal != null)
                {
                    if (journal.EmotionalAnalysis != null)
                    {
                        var response = $"**Journal Entry**\n\n" +
                                   $"**Persian:**\n{journal.PersianJournal}\n\n" +
                                   $"**Emotional Analysis:**\n{journal.EmotionalAnalysis}\n\n" +
                                   $"**Topic:**\n{journal.JournalTopic}\n\n" +
                                   $"**Polarity:**\n{journal.Polarity}";

                        await _botClient.SendTextMessageAsync(chatId, response, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
                    }
                    else
                    {
                        var response = journal.RawJson;
                        await _botClient.SendTextMessageAsync(chatId, response, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);

                    }
                }
            }
        }
    }

    private async Task SendMainMenu(long chatId, CancellationToken cancellationToken)
    {
        var replyKeyboardMarkup = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "Add New Journal", "Show My Journals" }
        })
        {
            ResizeKeyboard = true
        };

        await _botClient.SendTextMessageAsync(chatId, "Choose an option:", replyMarkup: replyKeyboardMarkup, cancellationToken: cancellationToken);
    }

    private async Task SendJournalTopics(long chatId, CancellationToken cancellationToken)
    {
        if (_userJournalCache.TryGetValue(chatId, out var journalList))
        {
            if (journalList.Any())
            {
                var inlineKeyboardButtons = journalList.Select(journal =>
                    new InlineKeyboardButton[]
                    {
                        InlineKeyboardButton.WithCallbackData(journal.Key, $"journal_{journal.Value.Id}")
                    }
                ).ToArray();

                var inlineKeyboard = new InlineKeyboardMarkup(inlineKeyboardButtons);

                await _botClient.SendTextMessageAsync(chatId, "Select a journal topic:", replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
            }
            else
            {
                await _botClient.SendTextMessageAsync(chatId, "You have no journals saved.", cancellationToken: cancellationToken);
            }
        }
        else
        {
            await _botClient.SendTextMessageAsync(chatId, "You have no journals saved.", cancellationToken: cancellationToken);
        }
    }
        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine(exception);
        return Task.CompletedTask;
    }
}
