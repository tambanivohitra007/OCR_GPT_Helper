using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public class GeminiManager : IDisposable
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private const string GeminiApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";

    public enum GeminiAssistAction
    {
        Answer,
        Explain,
        Translate,
        Enhance,
        Reply
    }

    public GeminiManager(string apiKey, string model = "gemini-2.0-flash")
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("Gemini API key cannot be null or empty.", nameof(apiKey));
        _apiKey = apiKey;
        _httpClient = new HttpClient();
        _model = string.IsNullOrWhiteSpace(model) ? "gemini-2.0-flash" : model;
    }

    public async Task<string> ProcessTextAsync(string text, GeminiAssistAction action)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "No text was provided.";

        string prompt = action switch
        {
            GeminiAssistAction.Answer => $"Based on the text below, please identify the correct answer for the multiple-choice question:\n\n{text}",
            GeminiAssistAction.Explain => $"Please provide a straightforward explanation of the following text:\n\n{text}",
            GeminiAssistAction.Translate => $"Please provide a professional translation of the following text into English:\n\n{text}",
            GeminiAssistAction.Enhance => $"Please review and enhance the following text for improved clarity and professionalism:\n\n{text}",
            GeminiAssistAction.Reply => $"Please draft a professional reply to this email:\n\n{text}",
            _ => $"Please process the following text:\n\n{text}"
        };

        // Gemini API expects a specific structure. Add minimal generationConfig for safety.
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.1,
                maxOutputTokens = 1024
            }
            // Optionally, add safetySettings here if needed
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        string url = $"{GeminiApiBaseUrl}{_model}:generateContent?key={_apiKey}";
        try
        {
            var response = await _httpClient.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Return the error details for easier debugging
                return $"Gemini API error {response.StatusCode}: {responseString}";
            }

            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;
            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var textResult = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                return !string.IsNullOrWhiteSpace(textResult) ? textResult.Trim() : "Gemini API returned empty content.";
            }
            return "Gemini API returned no candidates.";
        }
        catch (Exception ex)
        {
            return $"An unexpected error occurred calling Gemini API: {ex.GetType().Name} - {ex.Message}";
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
