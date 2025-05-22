using OpenAI;
using OpenAI.Chat;
using System;
using System.Text.Json;
using System.Threading.Tasks;

public class OpenAIManager : IDisposable
{
    private readonly OpenAIClient _openAIClient;
    public enum OpenAIAssistAction
    {
        Answer,
        Explain,
        Translate,
        Enhance
    }
    public OpenAIManager(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("OpenAI API key cannot be null or empty.", nameof(apiKey));
        }

        _openAIClient = new OpenAIClient(apiKey);
    }

    public async Task<string> ProcessTextAsync(string text, OpenAIAssistAction action)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "No text was provided.";

        string systemPrompt = action switch
        {
            OpenAIAssistAction.Answer => "You are an AI assistant tasked with answering multiple-choice questions. Analyze the text carefully and pick the best answer. Provide the answer clearly, e.g., 'The correct answer is (C).' ",
            OpenAIAssistAction.Explain => "You are an AI assistant. Please explain the following text in simple terms.",
            OpenAIAssistAction.Translate => "You are an AI translator. Please translate the following text into English.",
            OpenAIAssistAction.Enhance => "You are an AI writing assistant. Please improve the clarity and style of the following text.",
            _ => "You are an AI assistant. Please process the following text."
        };

        string userPrompt = action switch
        {
            OpenAIAssistAction.Answer => $"The following text is from a multiple-choice question. Please provide the correct answer:\n\n{text}",
            OpenAIAssistAction.Explain => $"Please explain this text:\n\n{text}",
            OpenAIAssistAction.Translate => $"Please translate this text:\n\n{text}",
            OpenAIAssistAction.Enhance => $"Please enhance the clarity and style of this text:\n\n{text}",
            _ => text
        };

        try
        {
            var chatRequest = new ChatRequest(
                messages: new[]
                {
                new OpenAI.Chat.Message(Role.System, systemPrompt),
                new OpenAI.Chat.Message(Role.User, userPrompt)
                },
                model: "gpt-4o",
                temperature: 0.1,
                maxTokens: 500
            );

            var chatResponse = await _openAIClient.ChatEndpoint.GetCompletionAsync(chatRequest);
            var firstMessageContent = chatResponse?.FirstChoice?.Message?.Content;
            string finalContent = firstMessageContent switch
            {
                string s when !string.IsNullOrWhiteSpace(s) => s,
                JsonElement jsonElement when jsonElement.ValueKind != JsonValueKind.Null && jsonElement.ValueKind != JsonValueKind.Undefined => jsonElement.GetString() ?? jsonElement.ToString() ?? string.Empty,
                _ => string.Empty
            };

            return !string.IsNullOrWhiteSpace(finalContent)
                ? finalContent.Trim()
                : "OpenAI API returned empty, whitespace, or null content.";
        }
        catch (Exception ex)
        {
            return $"An unexpected error occurred calling OpenAI API: {ex.GetType().Name} - {ex.Message}";
        }
    }
    public void Dispose()
    {
        _openAIClient?.Dispose();
    }
}
