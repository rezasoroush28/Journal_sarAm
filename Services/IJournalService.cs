using System.Collections.Generic;
using System.Threading.Tasks;
using TelegramBot.Models;

public interface IJournalService
{
    Task<List<Journal>> GetAllJournalsByUserIdAsync(int userId);
    Task<Journal> GetJournalByIdAsync(int id);
    Task AddJournalAsync(Journal journal);
    Task UpdateJournalAsync(Journal journal);
    Task DeleteJournalAsync(int id);
}
