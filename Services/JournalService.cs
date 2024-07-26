using Microsoft.EntityFrameworkCore;
using TelegramBot.Models;
using TelegramBot;
using System.Collections.Generic;
using System.Threading.Tasks;

public class JournalService : IJournalService
{
    private readonly IRepository<Journal> _journalRepository;

    public JournalService(IRepository<Journal> journalRepository)
    {
        _journalRepository = journalRepository;
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
}
