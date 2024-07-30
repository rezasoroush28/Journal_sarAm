using System.Text;
using System.Text.Json;

public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public OpenAIService(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public async Task<string> GetJournalAnalysisAsync(string prompt)
    {
        var request = new
        {
            model = "gpt-4",
            messages = new[]
            {
                new { role = "system", content = "Translate the following Persian journal to English, provide an emotional analysis in persian  , and extract a topic in Persian in JSON format" },
                new { role = "user", content = prompt }
            }
        };

        var requestContent = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
        {
            Headers = { { "Authorization", $"Bearer {_apiKey}" } },
            Content = requestContent
        };

        var response = await _httpClient.SendAsync(requestMessage);
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();

        return responseContent;
    }
}
