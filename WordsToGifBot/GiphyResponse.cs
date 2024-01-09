namespace TelegramBotTest
{
    internal class GiphyResponse
    {
        public Data? data { get; set; }
        public Meta? meta { get; set; }
    }

    public class Data
    {
        public string? url { get; set; }
        public Images? images { get; set; }
    }

    public class Images
    {
        public Original? original { get; set; }
    }

    public class Original
    {
        public string? url { get; set; }
    }

    public class Meta
    {
        public int status { get; set; }
        public string? msg { get; set; }
        public string? response_id { get; set; }
    }
}
