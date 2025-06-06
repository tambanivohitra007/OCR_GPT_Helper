using OpenAI;
using OpenAI.Chat;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public class OpenAIManager : IDisposable
{
    private readonly OpenAIClient _openAIClient;
    private readonly GeminiManager _geminiManager;
    private readonly bool _hasGemini;
    public enum OpenAIAssistAction
    {
        Answer,
        Explain,
        Translate,
        Enhance,
        Reply // Added for email reply assistance
    }
    public OpenAIManager(string apiKey, string geminiApiKey = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("OpenAI API key cannot be null or empty.", nameof(apiKey));
        }
        _openAIClient = new OpenAIClient(apiKey);

        if (!string.IsNullOrWhiteSpace(geminiApiKey))
        {
            _geminiManager = new GeminiManager(geminiApiKey);
            _hasGemini = true;
        }
        else
        {
            _hasGemini = false;
        }
    }

    public async Task<string> ProcessTextAsync(string text, OpenAIAssistAction action)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "No text was provided.";

        string systemPrompt = action switch
        {
            OpenAIAssistAction.Answer => "As an AI assistant, your function is to accurately identify the correct answer to a multiple-choice question based exclusively on the provided text. Analyze the source material to determine the most logical and factually supported option. State the correct option clearly.",
            OpenAIAssistAction.Explain => "As an AI assistant, your function is to provide a clear and objective explanation of the following text. Break down complex concepts into simpler terms and focus on conveying the core meaning of the material without introducing external information or personal interpretation.",
            OpenAIAssistAction.Translate => "As an AI language specialist, your function is to accurately translate the provided text into standard English. Your translation should be faithful to the original's meaning, tone, and context.",
            OpenAIAssistAction.Enhance => "As an AI writing assistant, your function is to refine the following text. Your goal is to improve its clarity, conciseness, and overall professionalism while preserving the original author's intended meaning. Do not introduce new concepts or alter the core message.",
            OpenAIAssistAction.Reply => "As an AI communication assistant, your function is to draft a professional and contextually appropriate reply to the following email. The tone should be courteous and the content should directly address the points raised in the original message.",
            _ => "As an AI assistant, your function is to process the following text according to the user's request. Adhere strictly to the provided information and avoid making assumptions or generating speculative content."
        };

        string userPrompt = action switch
        {
            OpenAIAssistAction.Answer => $"Based on the text below, please identify the correct answer for the multiple-choice question:\n\n{text}",
            OpenAIAssistAction.Explain => $"Please provide a straightforward explanation of the following text:\n\n{text}",
            OpenAIAssistAction.Translate => $"Please provide a professional translation of the following text into English:\n\n{text}",
            OpenAIAssistAction.Enhance => $"Please review and enhance the following text for improved clarity and professionalism:\n\n{text}",
            OpenAIAssistAction.Reply => $"Please draft a professional reply to this email:\n\n{text}",
            _ => $"Please process the following text:\n\n{text}"
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
                maxTokens: 5000
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
        catch (Exception)
        {
            // Fallback to Gemini if available
            if (_hasGemini)
            {
                var geminiAction = (GeminiManager.GeminiAssistAction)Enum.Parse(typeof(GeminiManager.GeminiAssistAction), action.ToString());
                return await _geminiManager.ProcessTextAsync(text, geminiAction);
            }
            throw;
        }
    }
    public void Dispose()
    {
        _openAIClient?.Dispose();
        _geminiManager?.Dispose();
    }
}
