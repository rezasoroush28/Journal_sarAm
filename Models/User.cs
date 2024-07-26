namespace TelegramBot.Models
{
    public record User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string TelegramId { get; set; }
    }
}
