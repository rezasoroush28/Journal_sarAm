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
            model = "gpt-4o",
            messages = new[]
            {
                new { role = "system", content = "Analyze the following Persian journal entry " +
                "and provide an  emotional analysis in persian , " +
                "topic in Persian, and a polarity score ranging from -1 to " +
                "1. Return the data as a JSON object with the keys: 'emotional_analysis', 'topic', and 'polarity' " +
                " Ensure that the response content only includes these three keys" },

                new { role = "user", content = prompt }
            }
        };

        var requestContent = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
        {
            Content = requestContent
        };

        // Ensure the Authorization header is correctly set
        requestMessage.Headers.Add("Authorization", $"Bearer sk-None-qY3Bx26rSGBzibuKkF0sT3BlbkFJQvOoeMLc63e2TMHQqmI2");

        var response = await _httpClient.SendAsync(requestMessage);
        response.EnsureSuccessStatusCode(); // Throws exception if the status code is not success
        var responseContent = await response.Content.ReadAsStringAsync();

        return responseContent;
    }
}
