using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot;
using TelegramBotProject.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<BotConfiguration>(builder.Configuration.GetSection("BotConfiguration"));
builder.Services.AddSingleton<ITelegramBotClient>(provider =>
{
    var botConfig = provider.GetRequiredService<IOptions<BotConfiguration>>().Value;
    return new TelegramBotClient(botConfig.BotToken);
});

// Add services to the container.
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IJournalService, JournalService>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddControllers();
builder.Services.AddDbContext<JournalDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Telegram Bot Setup
var botClient = app.Services.GetRequiredService<ITelegramBotClient>();

var cts = new CancellationTokenSource();

ReceiverOptions receiverOptions = new() { AllowedUpdates = Array.Empty<UpdateType>() };

botClient.StartReceiving(
    async (botClient, update, cancellationToken) =>
    {
        if (update.Type == UpdateType.Message && update.Message!.Type == MessageType.Text)
        {
            var chatId = update.Message.Chat.Id;
            await botClient.SendTextMessageAsync(chatId, $"Your Telegram ID is: {chatId}", cancellationToken: cancellationToken);
        }
    },
    HandleErrorAsync,
    receiverOptions,
    cancellationToken: cts.Token
);

app.Run();

static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    Console.WriteLine(exception);
    return Task.CompletedTask;
}
