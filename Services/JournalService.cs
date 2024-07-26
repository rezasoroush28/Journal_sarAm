using Microsoft.EntityFrameworkCore;
using TelegramBot.Models;

public class JournalService : IJournalService
{
    private readonly IRepository<Journal> _journalRepository;

    public JournalService(IRepository<Journal> journalRepository)
    {
        _journalRepository = journalRepository;
    }

    public async Task<IEnumerable<Journal>> GetAllJournalsAsync()
    {
        return await _journalRepository.Table.ToListAsync();
    }

    public async Task<IEnumerable<Journal>> GetJournalsByUserIdAsync(int userId)
    {
        return await _journalRepository.Table
                                        .Where(j => j.UserId == userId)
                                        .ToListAsync();
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
}
