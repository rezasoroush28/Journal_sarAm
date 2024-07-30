public interface IOpenAIService
{
    Task<string> GetJournalAnalysisAsync(string persianJournal);
}