using Microsoft.EntityFrameworkCore;
using TelegramBot.Models;
using TelegramBot;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;

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
        return await _journalRepository.TableNoTracking.Where(x => x.UserId == userId).ToListAsync();
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
        journal.Translation = result.Translation;
        journal.EmotionalAnalysis = result.EmotionalAnalysis;
        journal.Polarity = result.Polarity;
        journal.JournalTopic = result.Topic;
        await _journalRepository.UpdateAsync(journal);
        return journal;
    }
}

public class OpenAIResponse
{
    public string Translation { get; set; }
    public string EmotionalAnalysis { get; set; }
    public double Polarity { get; set; }
    public string Topic { get; set; }
}

