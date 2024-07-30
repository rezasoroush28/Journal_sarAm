using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using TelegramBot;
using TelegramBotProject.Data;

var builder = WebApplication.CreateBuilder(args);

// Configure Bot settings
builder.Services.Configure<BotConfiguration>(builder.Configuration.GetSection("BotConfiguration"));
builder.Services.AddSingleton<ITelegramBotClient>(provider =>
{
    var botConfig = provider.GetRequiredService<IOptions<BotConfiguration>>().Value;
    return new TelegramBotClient(botConfig.BotToken);
});

// Configure OpenAI settings
builder.Services.Configure<OpenAIConfiguration>(builder.Configuration.GetSection("OpenAI"));

// Register OpenAIService with HttpClient
builder.Services.AddHttpClient<IOpenAIService, OpenAIService>((sp, client) =>
{
    var config = sp.GetRequiredService<IOptions<OpenAIConfiguration>>().Value;
    client.BaseAddress = new Uri("https://api.openai.com/");
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");
});

// Add services to the container.
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IJournalService, JournalService>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddControllers();
builder.Services.AddDbContext<JournalDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register the TelegramBotHandler
builder.Services.AddHostedService<TelegramBotUpdateHandler>();

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

app.Run();
