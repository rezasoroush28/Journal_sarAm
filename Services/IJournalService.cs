using System.Collections.Generic;
using System.Threading.Tasks;
using TelegramBot.Models;

public interface IJournalService
{
    Task<IEnumerable<Journal>> GetAllJournalsAsync();
    Task<IEnumerable<Journal>> GetJournalsByUserIdAsync(int userId);
    Task AddJournalAsync(Journal journal);
    Task UpdateJournalAsync(Journal journal);
    Task DeleteJournalAsync(int id);
}
