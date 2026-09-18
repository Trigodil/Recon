using Discord;
using Discord.WebSocket;
using System.Text.Json;

public static class Commands
{
    private static readonly ulong roleId = LoadDevRoleId();
    private static readonly TimeSpan cooldownDuration = TimeSpan.FromMinutes(40);
    private static readonly Dictionary<ulong, DateTime> lastAiCall = new();

    private static ulong LoadDevRoleId()
    {
        var json_perms = File.ReadAllText("perms.json");

        return JsonDocument.Parse(json_perms)
            .RootElement
            .GetProperty("DevRoleId")
            .GetUInt64();
    }

    private static bool IsDev(SocketUserMessage userMessage)
    {
        return userMessage.Author is SocketGuildUser guildUser &&
            guildUser.Roles.Any(role => role.Id == roleId);
    }

    private static bool TryConsumeAiCooldown(SocketUserMessage userMessage, out TimeSpan remaining)
    {
        remaining = TimeSpan.Zero;

        if (IsDev(userMessage))
            return true;

        var userId = userMessage.Author.Id;
        var now = DateTime.UtcNow;

        if (lastAiCall.TryGetValue(userId, out var last))
        {
            var elapsed = now - last;

            if (elapsed < cooldownDuration)
            {
                remaining = cooldownDuration - elapsed;
                return false;
            }
        }

        lastAiCall[userId] = now;
        return true;
    }

    public static async Task Anwser(SocketUserMessage userMessage)
    {
        var content = userMessage.Content;

        var context = "The user is asking you a question, anwser breifly";

        if (!content.StartsWith("!ask"))
            return;

        var prompt = content[4..].Trim();

        if (prompt.Length == 0)
        {
            await userMessage.ReplyAsync("Ask me something mate, you can't just expect me to guess now can you?");
            return;
        }

        if (!TryConsumeAiCooldown(userMessage, out _))
        {
            await userMessage.AddReactionAsync(new Emoji("⏰"));
            return;
        }

        if (userMessage.Reference?.MessageId.IsSpecified == true)
        {
            var repliedMessageId = userMessage.Reference.MessageId.Value;

            var repliedTo = await userMessage.Channel.GetMessageAsync(repliedMessageId);

            if (repliedTo is not IUserMessage repliedUsermessage)
            {
                await userMessage.ReplyAsync("Could not load the replied message");
                return;
            }

            var content_reply = repliedUsermessage.Content;

            if (string.IsNullOrWhiteSpace(content_reply))
            {
                await userMessage.ReplyAsync("No message mate");
                return;
            }

            var prompt_ai =
                $"Context: {content_reply}\n\n" +
                $"User question: {prompt}";

            await userMessage.AddReactionAsync(new Emoji("✅"));
            var reply = await AiService.AskAiAsync(prompt_ai, context);

            await AnswerAsync(userMessage, reply);
            return;
        }

        await userMessage.AddReactionAsync(new Emoji("✅"));
        var normalReply = await AiService.AskAiAsync(prompt, context);

        await AnswerAsync(userMessage, normalReply);
    }

    public static async Task Search(SocketUserMessage userMessage)
    {
        var content = userMessage.Content;

        var context = "The user wants you to search the web for this and answer using what you find, cite your sources";

        if (!content.StartsWith("!search"))
            return;

        var query = content["!search".Length..].Trim();

        if (query.Length == 0)
        {
            await userMessage.ReplyAsync("Search for what mate, gotta give me something.");
            return;
        }

        if (!TryConsumeAiCooldown(userMessage, out _))
        {
            await userMessage.AddReactionAsync(new Emoji("⏰"));
            return;
        }

        await userMessage.AddReactionAsync(new Emoji("✅"));
        var reply = await AiService.AskAiAsync(query, context, searchOnly: true);

        await userMessage.ReplyAsync(reply);
    }

    public static async Task SwitchAi(SocketUserMessage userMessage)
    {
        var content = userMessage.Content;

        if (!content.StartsWith("!change"))
            return;

        if (!IsDev(userMessage))
        {
            await userMessage.ReplyAsync("Nice try mate, only the dev can do that.");
            return;
        }

        var arg = content["!change".Length..].Trim().ToLowerInvariant();

        AiService.AiProvider provider;
        string providerName;

        if (arg == "openai")
        {
            provider = AiService.AiProvider.OpenAi;
            providerName = "Openai";
        }
        else if (arg == "claude")
        {
            provider = AiService.AiProvider.Claude;
            providerName = "Claude";
        }
        else
        {
            await userMessage.ReplyAsync("Pick one mate, `!change openai` or `!change claude`.");
            return;
        }

        AiService.CurrentProvider = provider;

        await userMessage.ReplyAsync($"Recon switched to {providerName}");
    }

    public static async Task Ping(SocketUserMessage userMessage)
    {
        if (userMessage.Author is SocketGuildUser guildUser)
        {
            var context = "This is the programmer who made your discord program speaking, speak to him as a friend, permission to be unhinged is granted. If he asks who he is, tell him straight up: he's your creator, the one who programmed and built you. Make your replies to him unhinged, i.e. teasing, joking, sarcasm.";
            var content = userMessage.Content;
            var roles = guildUser.Roles;
            if (guildUser.Roles.Any(role => role.Id == roleId))
            {
                await userMessage.AddReactionAsync(new Emoji("✅"));
                var response = await AiService.AskAiAsync(content, context);
                await userMessage.ReplyAsync(response);
                return;
            }

        }
    }


    public static async Task WhatAsync(SocketUserMessage userMessage)
    {
        var context = "Explain what you think and the message content breifly";



        if (!string.Equals(
            userMessage.Content,
            "what",
            StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (userMessage.Reference?.MessageId.IsSpecified != true)
        {
            await userMessage.ReplyAsync(
                "No message referenced, I WAS SLEEPING HERE");
            return;
        }


        var referencedMessageId = userMessage.Reference.MessageId.Value;


        var referencedMessage =
            await userMessage.Channel.GetMessageAsync(referencedMessageId);






        if (referencedMessage is not IUserMessage referencedUserMessage)
        {
            await userMessage.ReplyAsync(
                "No message referenced, I WAS SLEEPING HERE BRUH");
            return;
        }

        if (referencedUserMessage.Author.IsBot)
        {
            await userMessage.ReplyAsync(
                "You can't trick me peasent NO, BAD discord user");
            return;
        }

        if (!TryConsumeAiCooldown(userMessage, out _))
        {
            await userMessage.AddReactionAsync(new Emoji("⏰"));
            return;
        }

        await userMessage.AddReactionAsync(new Emoji("✅"));

        await referencedUserMessage.ReplyAsync(
        await AiService.AskAiAsync(referencedUserMessage.Content, context)
        );
    }

    public static async Task ReportErrorAsync(SocketUserMessage userMessage, Exception ex)
    {
        try
        {
            await userMessage.AddReactionAsync(new Emoji("❌"));
        }
        catch
        {
        }

        if (userMessage.Channel is not SocketGuildChannel guildChannel)
            return;

        var errorText = ex.ToString();

        if (errorText.Length > 1800)
            errorText = errorText[..1800];

        await DmRoleAsync(guildChannel.Guild, roleId, $"Recon hit an error:\n```{errorText}```");
    }

    private static Task AnswerAsync(
        SocketUserMessage userMessage,
        string replyDiscord)
    {
        return userMessage.ReplyAsync(replyDiscord);
    }

    private static async Task DmRoleAsync(
        SocketGuild guild,
        ulong roleId,
        string text)
    {
        var role = guild.GetRole(roleId);

        if (role is null)
            return;

        foreach (var member in role.Members)
        {
            try
            {
                var dm = await member.CreateDMChannelAsync();
                await dm.SendMessageAsync(text);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"DM failed for {member.Username}: {ex.Message}");
            }
        }
    }


}

