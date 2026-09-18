using System.Runtime.CompilerServices;
using Discord;
using Discord.WebSocket;
using System.Text.Json;
using Discord.Commands;
using Anthropic;
using Anthropic.Models.Messages;
using Discord.Rest;

// Creating the ai

var json_api = File.ReadAllText("cred.json");
var api_key = JsonDocument.Parse(json_api)
    .RootElement
    .GetProperty("ai_api")
    .GetProperty("Token")
    .GetString();

Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", api_key);


//---------------------------------------------------------------------------------   

var client = new DiscordSocketClient(new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.Guilds |
    GatewayIntents.GuildMembers |
    GatewayIntents.MessageContent |
    GatewayIntents.GuildMessages
});
client.Log += log =>
{
    Console.WriteLine(log);
    return Task.CompletedTask;
};


client.Ready += () =>
{
    Console.WriteLine("Connected to discord");
    return Task.CompletedTask;
};

// All da functions
async Task MessageRecieved(SocketMessage message)
{
    if (message is not SocketUserMessage userMessage)
        return;

    Console.WriteLine($"Message received: {userMessage.Content}");

    if (userMessage.Author.IsBot)
        return;

    var content = userMessage.Content;

    try
    {
        if (content.StartsWith("!ask"))
        {
            await Commands.Anwser(userMessage);
            return;
        }

        if (content.StartsWith("!change"))
        {
            await Commands.SwitchAi(userMessage);
            return;
        }

        if (content.StartsWith("!search"))
        {
            await Commands.Search(userMessage);
            return;
        }

        if (content.StartsWith("!help"))
        {
            await Help.SendHelp(userMessage);
            return;
        }
        if (userMessage.Content.Trim().Equals(
             "what",
             StringComparison.OrdinalIgnoreCase) ||
         userMessage.Content.Trim().Equals(
             "wht",
             StringComparison.OrdinalIgnoreCase))
        {
            await Commands.WhatAsync(userMessage);
            return;
        }

        if (userMessage.MentionedUsers.Any(user => user.Id == client.CurrentUser.Id))
        {
            await Commands.Ping(userMessage);
            return;
        }
    }
    catch (Exception ex)
    {
        await Commands.ReportErrorAsync(userMessage, ex);
    }
}


client.MessageReceived += message =>
{
    _ = Task.Run(() => MessageRecieved(message));
    return Task.CompletedTask;
};

// parse the json files
var json = File.ReadAllText("discord.json");
var token = JsonDocument.Parse(json)
    .RootElement
    .GetProperty("Discord")
    .GetProperty("DiscordToken")
    .GetString();

await client.LoginAsync(TokenType.Bot, token);
await client.StartAsync();
await Task.Delay(-1);
