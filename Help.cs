
using Discord;
using Discord.WebSocket;

public static class Help
{
    public static async Task SendHelp(SocketUserMessage userMessage)
    {
        var embed = new EmbedBuilder()
            .WithTitle("Help")
            .WithDescription("Here are the available commands:")
            .AddField("1. `!ask <your question>`", "Get an answer from the AI.")
            .AddField("2. `!help`", "Display this help message.")
            .AddField("3. `!ping`", "Check if the bot is responsive.")
            .AddField("4. `!info`", "Get information about the bot.")
            .AddField("5. `what`", "Reference a message and get an answer from the AI.")
            .AddField("6. `!change <openai|claude>`", "Switch which AI Recon uses (dev only).")
            .AddField("7. `!search <query>`", "Search the web via a search engine and answer, no direct link fetching.")
            .WithColor(Color.Blue)
            .Build();

        await userMessage.Channel.SendMessageAsync(embed: embed);
    }
}

