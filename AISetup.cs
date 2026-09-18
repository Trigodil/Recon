using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using OpenAI;
using OpenAI.Responses;

public static class AiService
{
    public enum AiProvider
    {
        Claude,
        OpenAi
    }

    public static AiProvider CurrentProvider = AiProvider.Claude;

    private static readonly string personality =
    File.ReadAllText(Path.Combine("Instructions", "personality.md"));

    private static readonly string GeneralRule = "In your responses YOU NEVER EVER ask the user a question back, you are in the form of a helpful discord bot follow the conext and rules provided to you keep your replies very short unless explicitly asked, avoid using fancy formatting, using just normal text is encouraged.";

    private static readonly AnthropicClient client_ai = CreateClient();
    private static readonly Lazy<OpenAIClient> client_openai = new(CreateOpenAiClient);

    private static AnthropicClient CreateClient()
    {
        var json_api = File.ReadAllText("cred.json");

        var api_key = JsonDocument.Parse(json_api)
            .RootElement
            .GetProperty("ai_api")
            .GetProperty("Token")
            .GetString();

        Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", api_key);

        return new AnthropicClient();
    }

    private static OpenAIClient CreateOpenAiClient()
    {
        var json_api = File.ReadAllText("cred.json");

        var api_key = JsonDocument.Parse(json_api)
            .RootElement
            .GetProperty("openai_api")
            .GetProperty("Token")
            .GetString();

        return new OpenAIClient(api_key);
    }

    public static Task<string> AskAiAsync(
    string prompt,
    string commandContext,
    bool searchOnly = false)
    {
        return CurrentProvider switch
        {
            AiProvider.OpenAi => AskOpenAiAsync(prompt, commandContext, searchOnly),
            _ => AskClaudeAsync(prompt, commandContext, searchOnly)
        };
    }

    private static async Task<string> AskClaudeAsync(
    string prompt,
    string commandContext,
    bool searchOnly = false)
    {
        List<ToolUnion> tools = searchOnly
            ? [new WebSearchTool20260318 { MaxUses = 3 }]
            : [new WebSearchTool20260318 { MaxUses = 3 }, new WebFetchTool20260318 { MaxUses = 3 }];

        var msg = await client_ai.Messages.Create(new()
        {
            Model = "claude-sonnet-4-6",
            MaxTokens = 1024,
            System = Instructions(commandContext),
            Messages =
            [
                new()
                {
                    Role = Role.User,
                    Content = prompt
                }
            ],
            Tools = tools
        });

        var reply = "";

        foreach (var block in msg.Content)
        {
            if (block.TryPickText(out var textBlock))
            {
                reply += textBlock.Text;
            }
        }

        return reply;
    }

    private static readonly BinaryData webFetchParameters = BinaryData.FromString(
        "{\"type\":\"object\",\"properties\":{\"url\":{\"type\":\"string\",\"description\":\"The URL to fetch the text content of\"}},\"required\":[\"url\"]}");

    private static string Instructions(string commandContext) =>
        $"{personality}\n\n" +
        $"Command instructions:\n{commandContext}" +
        $"General Rule\n{GeneralRule}";

    private static void AddOpenAiTools(CreateResponseOptions options, bool searchOnly)
    {
        options.Tools.Add(new WebSearchTool());

        if (!searchOnly)
        {
            options.Tools.Add(ResponseTool.CreateFunctionTool(
                "fetch_url",
                webFetchParameters,
                null,
                "Fetches the text content of a specific web page URL."));
        }
    }

    private static async Task<string> AskOpenAiAsync(
    string prompt,
    string commandContext,
    bool searchOnly = false)
    {
        var responsesClient = client_openai.Value.GetResponsesClient();

        var options = new CreateResponseOptions
        {
            Model = "gpt-5.6-terra",
            MaxOutputTokenCount = 1024,
            Instructions = Instructions(commandContext)
        };

        options.InputItems.Add(ResponseItem.CreateUserMessageItem(prompt));
        AddOpenAiTools(options, searchOnly);

        var result = await responsesClient.CreateResponseAsync(options);

        var fetchCalls = result.Value.OutputItems.OfType<FunctionCallResponseItem>()
            .Where(call => call.FunctionName == "fetch_url").ToList();

        var loops = 0;

        while (fetchCalls.Count > 0 && loops < 3)
        {
            var followUp = new CreateResponseOptions
            {
                Model = "gpt-5.6-terra",
                MaxOutputTokenCount = 1024,
                Instructions = Instructions(commandContext),
                PreviousResponseId = result.Value.Id
            };

            AddOpenAiTools(followUp, searchOnly);

            foreach (var call in fetchCalls)
            {
                var args = JsonDocument.Parse(call.FunctionArguments).RootElement;
                var url = args.GetProperty("url").GetString() ?? "";
                var output = await WebFetch.FetchUrlAsync(url);

                followUp.InputItems.Add(ResponseItem.CreateFunctionCallOutputItem(call.CallId, output));
            }

            result = await responsesClient.CreateResponseAsync(followUp);

            fetchCalls = result.Value.OutputItems.OfType<FunctionCallResponseItem>()
                .Where(call => call.FunctionName == "fetch_url").ToList();

            loops++;
        }

        var reply = "";

        foreach (var item in result.Value.OutputItems)
        {
            if (item is MessageResponseItem messageItem)
            {
                foreach (var part in messageItem.Content)
                {
                    if (part.Text is not null)
                    {
                        reply += part.Text;
                    }
                }
            }
        }

        return reply;
    }
}

