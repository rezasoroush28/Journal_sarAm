using System.Text;
using System.Text.Json;

public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _httpClient;

    public OpenAIService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetJournalAnalysisAsync(string prompt)
    {
        var request = new
        {
            model = "gpt-4",
            messages = new[]
            {
                new { role = "system", content = "Translate the following Persian journal to English, provide an emotional analysis in Persian, and extract a topic in Persian in JSON format" },
                new { role = "user", content = prompt }
            }
        };

        var requestContent = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("v1/chat/completions", requestContent);

        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();

        return responseContent;
    }
}
