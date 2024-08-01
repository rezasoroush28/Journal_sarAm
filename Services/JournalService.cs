using Microsoft.EntityFrameworkCore;
using TelegramBot.Models;
using TelegramBot;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using static OpenAIResponse;

public class JournalService : IJournalService
{
    private readonly IOpenAIService _openAiService;
    private readonly IRepository<Journal> _journalRepository;

    public JournalService(IRepository<Journal> journalRepository, IOpenAIService openAiService)
    {
        _journalRepository = journalRepository;
        _openAiService = openAiService;
    }

    public async Task<List<Journal>> GetAllJournalsByUserIdAsync(int userId)
    {
        var journals = _journalRepository.TableNoTracking.Where(x => x.UserId == userId).ToList();
        var notReadyJournals = journals.Where(j => j.EmotionalAnalysis == null).ToList();
        foreach (var journal in notReadyJournals)
        {
            try
            {
                var journalAnalysis = JsonSerializer.Deserialize<JournalAnalysis>(journal.RawJson);
                if (journalAnalysis != null)
                {
                    journal.EmotionalAnalysis = journalAnalysis.EmotionalAnalysis;
                    journal.JournalTopic = journalAnalysis.Topic;
                    journal.Polarity = journalAnalysis.Polarity;
                }

                this.UpdateJournalAsync(journal);
            }
            catch (Exception)
            {

            }
        }

        var newJournals = _journalRepository.TableNoTracking.Where(x => x.UserId == userId).ToListAsync();
        return await newJournals;
    }

    public async Task<Journal> GetJournalByIdAsync(int id)
    {
        return await _journalRepository.GetByIdAsync(id);
    }

    public async Task AddJournalAsync(Journal journal)
    {
        await _journalRepository.AddAsync(journal);
    }

    public async Task UpdateJournalAsync(Journal journal)
    {
        await _journalRepository.UpdateAsync(journal);
    }

    public async Task DeleteJournalAsync(int id)
    {
        var journal = await _journalRepository.GetByIdAsync(id);
        if (journal != null)
        {
            await _journalRepository.DeleteAsync(journal);
        }
    }
    public async Task<Journal> AnalyzeAndSaveJournalAsync(Journal journal)
    {
        var response = await _openAiService.GetJournalAnalysisAsync(journal.PersianJournal);

        // Assuming the response content is a JSON string containing the required fields
        var result = JsonSerializer.Deserialize<OpenAIResponse>(response);
        var contentJson = result.Choices.First().Message.Content;
        try
        {
            var journalAnalysis = JsonSerializer.Deserialize<JournalAnalysis>(contentJson);
            if (journalAnalysis != null)
            {
                journal.EmotionalAnalysis = journalAnalysis.EmotionalAnalysis;
                journal.JournalTopic = journalAnalysis.Topic;
                journal.Polarity = journalAnalysis.Polarity;
            }
        }

        catch (JsonException)
        {
            try
            {
                // Extract JSON content from the code block
                var startIndex = contentJson.IndexOf('{');
                var endIndex = contentJson.LastIndexOf('}');
                if (startIndex != -1 && endIndex != -1)
                {
                    var jsonContent = contentJson.Substring(startIndex, endIndex - startIndex + 1);

                    // Deserialize to JournalAnalysis object
                    var journalAnalysis = JsonSerializer.Deserialize<JournalAnalysis>(jsonContent);
                    if (journalAnalysis != null)
                    {
                        journal.EmotionalAnalysis = journalAnalysis.EmotionalAnalysis;
                        journal.JournalTopic = journalAnalysis.Topic;
                        journal.Polarity = journalAnalysis.Polarity;
                    }
                }
            }
            catch (Exception)
            {
                journal.RawJson = contentJson;
                
            }
            // If deserialization fails, store the raw JSON string in RawJson
            
        }


        //journal.Translation = result.Translation;
        await _journalRepository.UpdateAsync(journal);
        return journal;
    }
}


public class OpenAIResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("object")]
    public string Object { get; set; }

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; }

    [JsonPropertyName("choices")]
    public List<Choice> Choices { get; set; }

    public class Choice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("message")]
        public Message Message { get; set; }

        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; }
    }

    public class Message
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    public class Content
    {
        [JsonPropertyName("json")]
        public string Json { get; set; }
    }
}



public class JournalAnalysis
{
    [JsonPropertyName("emotional_analysis")]
    public string EmotionalAnalysis { get; set; }

    [JsonPropertyName("topic")]
    public string Topic { get; set; }

    [JsonPropertyName("polarity")]
    public double Polarity { get; set; }
}


