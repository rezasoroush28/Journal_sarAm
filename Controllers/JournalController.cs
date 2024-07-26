using Microsoft.AspNetCore.Mvc;
using TelegramBot.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TelegramBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JournalController : ControllerBase
    {
        private readonly IJournalService _journalService;

        public JournalController(IJournalService journalService)
        {
            _journalService = journalService;
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<IEnumerable<Journal>>> GetAllUserJournals(int userId)
        {
            var journals = await _journalService.GetAllJournalsByUserIdAsync(userId);
            return Ok(journals);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Journal>> GetJournalById(int id)
        {
            var journal = await _journalService.GetJournalByIdAsync(id);
            if (journal == null)
            {
                return NotFound();
            }
            return Ok(journal);
        }

        [HttpPost]
        public async Task<ActionResult> AddJournal([FromBody] Journal journal)
        {
            await _journalService.AddJournalAsync(journal);
            return CreatedAtAction(nameof(GetJournalById), new { id = journal.Id }, journal);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateJournal(int id, [FromBody] Journal journal)
        {
            if (id != journal.Id)
            {
                return BadRequest();
            }

            var existingJournal = await _journalService.GetJournalByIdAsync(id);
            if (existingJournal == null)
            {
                return NotFound();
            }

            await _journalService.UpdateJournalAsync(journal);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteJournal(int id)
        {
            var journal = await _journalService.GetJournalByIdAsync(id);
            if (journal == null)
            {
                return NotFound();
            }

            await _journalService.DeleteJournalAsync(id);
            return NoContent();
        }
    }
}
