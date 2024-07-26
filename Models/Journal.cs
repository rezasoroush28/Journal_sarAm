namespace TelegramBot.Models
{
    public record Journal
    {
        public int Id { get; set; }
        public string PersianJournal { get; set; }
        public string EmotionalAnalysis { get; set; }
        public string Translation { get; set; }
        public double Polarity { get; set; }
        public string JournalTopic { get; set; }
        public DateTime CreatedDate { get; set; }
        public int UserId { get; set; }
    }
}
