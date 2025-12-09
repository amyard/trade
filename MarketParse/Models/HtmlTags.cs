namespace MarketParse.Models
{
    public static class HtmlTags
    {
        // Basic formatting
        public const string BoldOpen = "<b>";
        public const string BoldClose = "</b>";

        public const string ItalicOpen = "<i>";
        public const string ItalicClose = "</i>";

        public const string UnderlineOpen = "<u>";
        public const string UnderlineClose = "</u>";

        public const string StrikethroughOpen = "<s>";
        public const string StrikethroughClose = "</s>";

        public const string CodeOpen = "<code>";
        public const string CodeClose = "</code>";

        public const string PreOpen = "<pre>";
        public const string PreClose = "</pre>";

        // Links
        public static string CreateLink(string url, string text) =>
            $"<a href=\"{url}\">{text}</a>";

        // Mentions
        public static string CreateUserMention(string userId, string text) =>
            $"<a href=\"tg://user?id={userId}\">{text}</a>";

        // Spacing
        public const string LineBreak = "\n";
        public const string DoubleLineBreak = "\n\n";
        public const string Tab = "\t";
        public const string Space = " ";

        // Special characters
        public const string LessThan = "&lt;";
        public const string GreaterThan = "&gt;";
        public const string Ampersand = "&amp;";
        public const string Quote = "&quot;";
        public const string Apostrophe = "&#39;";

        // Formatting helpers
        public static string Bold(string text) => $"{BoldOpen}{text}{BoldClose}";
        public static string Italic(string text) => $"{ItalicOpen}{text}{ItalicClose}";
        public static string Underline(string text) => $"{UnderlineOpen}{text}{UnderlineClose}";
        public static string Strikethrough(string text) => $"{StrikethroughOpen}{text}{StrikethroughClose}";
        public static string Code(string text) => $"{CodeOpen}{text}{CodeClose}";
        public static string Preformatted(string text) => $"{PreOpen}{text}{PreClose}";
    }
}
