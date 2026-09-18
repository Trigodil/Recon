# Recon

Recon is a C# Discord bot built with Discord.Net. This is essentially a learning project to learn the Discord.Net gateway model, wiring up multiple AI provider SDKs (Anthropic and OpenAI) behind one interface, tool use / function calling, and general bot ops (role gating, cooldowns, error handling etc).

## Personality

Helpful, calm, and powered by an unhealthy amount of coffee. Full personality and rules live in `Instructions/personality.md`.

## Commands

### !ask <question>

Ask the AI a question. Reply to another message with `!ask <question>` to give it that message as context.

### !search <query>

Ask the AI to search the web and answer using search results only. No direct URL fetching, search engine results only.

### !change <openai|claude>

Switch which AI provider Recon uses. Dev only.

### !help

Shows this list of commands as an embed.

### what / wht

Reply to a message with `what` (or `wht`) to get the AI's take on it.

### @Recon

Mention the bot directly. Dev only, replies in a more unhinged, friend to friend tone.

## Setup

Recon reads its config from three JSON files that are not checked into the repo. Copy the matching `.example.json` file for each, then fill in your own values:

1. `discord.example.json` to `discord.json`, add your Discord bot token:

```json
{
  "Discord": {
    "DiscordToken": "your-token-here"
  }
}
```

2. `cred.example.json` to `cred.json`, add your API keys:

```json
{
  "ai_api": {
    "Token": "your-anthropic-key"
  },
  "openai_api": {
    "Token": "your-openai-key"
  }
}
```

3. `perms.example.json` to `perms.json`, set your Discord role ID. This has to be found manually (enable Developer Mode in Discord, right click the role, Copy Role ID):

```json
{
  "DevRoleId": 0
}
```

4. Run the bot:

```powershell
dotnet run
```

## License

MIT. See [LICENSE](LICENSE).
