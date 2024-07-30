using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using TelegramBot;
using TelegramBotProject.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<BotConfiguration>(builder.Configuration.GetSection("BotConfiguration"));

builder.Services.AddHttpClient<IOpenAIService, OpenAIService>(client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/");
});

// Add your API key here
string apiKey = builder.Configuration["OpenAI:ApiKey"];
builder.Services.AddSingleton(new OpenAIService(new HttpClient(), apiKey));


builder.Services.Configure<OpenAIConfiguration>(
    builder.Configuration.GetSection("OpenAI"));

builder.Services.AddSingleton<IOpenAIService, OpenAIService>();


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
