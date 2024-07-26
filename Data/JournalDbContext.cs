using Microsoft.EntityFrameworkCore;
using TelegramBot.Models;

namespace TelegramBotProject.Data
{
    public class JournalDbContext : DbContext
    {
        public JournalDbContext(DbContextOptions<JournalDbContext> options)
            : base(options)
        {

        }
        public DbSet<User> Users { get; set; }
        public DbSet<Journal> Journals { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                 .HasKey(u => u.Id);
            modelBuilder.Entity<Journal>()
                .HasKey(j => j.Id);
            base.OnModelCreating(modelBuilder);
        }
    }
}
