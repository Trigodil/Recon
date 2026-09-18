using System.Text.RegularExpressions;

public static class WebFetch
{
    private static readonly HttpClient client = new();

    public static async Task<string> FetchUrlAsync(string url)
    {
        try
        {
            var html = await client.GetStringAsync(url);

            var text = Regex.Replace(html, "<script[^>]*>[\\s\\S]*?</script>", " ", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "<style[^>]*>[\\s\\S]*?</style>", " ", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "<[^>]+>", " ");
            text = Regex.Replace(text, "\\s+", " ").Trim();

            return text.Length > 4000 ? text[..4000] : text;
        }
        catch (Exception ex)
        {
            return $"Failed to fetch {url}: {ex.Message}";
        }
    }
}
