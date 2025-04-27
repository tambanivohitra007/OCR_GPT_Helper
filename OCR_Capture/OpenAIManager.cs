using OpenAI;
using OpenAI.Chat;
using System;
using System.Text.Json;
using System.Threading.Tasks;

public class OpenAIManager : IDisposable
{
    private readonly OpenAIClient _openAIClient;

    public OpenAIManager(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("OpenAI API key cannot be null or empty.", nameof(apiKey));
        }

        _openAIClient = new OpenAIClient(apiKey);
    }

    public async Task<string> AskQuestionAsync(string questionText)
    {
        // Validate input text
        if (string.IsNullOrWhiteSpace(questionText))
        {
            return "No text was provided to ask the question.";
        }

        try
        {
            // Create the chat request
            var chatRequest = new ChatRequest(
                messages: new[] // Define the conversation messages
                {
                // System message to set the AI's role and instructions
                new OpenAI.Chat.Message(Role.System, "You are an AI assistant tasked with answering multiple-choice questions. Analyze the text carefully and pick the best answer. Provide the answer clearly, e.g., 'The correct answer is (C).'"),
                // User message containing the extracted question text
                new OpenAI.Chat.Message(Role.User, $"The following text is from a multiple-choice question. Please provide the correct answer:\n\n{questionText}")
                },
                // Use a standard, stable chat model name like "gpt-4o" or "gpt-3.5-turbo"
                model: "gpt-4o", // Or try "gpt-3.5-turbo" if gpt-4o is not available or too expensive

                temperature: 0.1, // Lower temperature for more focused answers
                maxTokens: 500    // Limit the response length
            );

            // Call the chat completions endpoint asynchronously
            // Ensure _openAIClient is correctly initialized and accessible
            var chatResponse = await _openAIClient.ChatEndpoint.GetCompletionAsync(chatRequest);

            // *** FURTHER REFINED CHECK FOR RESPONSE CONTENT TO HANDLE JsonElement AND NULL SAFELY ***
            // Safely access the message content using null-conditional operator
            var firstMessageContent = chatResponse?.FirstChoice?.Message?.Content;

            string finalContent = string.Empty;

            // Use pattern matching to safely determine the type and value of firstMessageContent
            if (firstMessageContent is string contentString && !string.IsNullOrWhiteSpace(contentString))
            {
                // If it's a non-null, non-whitespace string
                finalContent = contentString;
            }
            else if (firstMessageContent is JsonElement jsonElement)
            {
                // If it's a JsonElement, check if it's not a JSON null and can be converted to a string
                if (jsonElement.ValueKind != JsonValueKind.Null && jsonElement.ValueKind != JsonValueKind.Undefined)
                {
                    // Attempt to get the string value from the JsonElement
                    try
                    {
                        finalContent = jsonElement.GetString() ?? string.Empty;
                    }
                    catch (InvalidOperationException)
                    {
                        // Handle cases where GetString() is not valid for the ValueKind
                        // Or if the value is null even though ValueKind wasn't Null
                        finalContent = jsonElement.ToString() ?? string.Empty; // Fallback to ToString()
                    }

                }
            }
            // If firstMessageContent is null or other unexpected type, finalContent remains string.Empty

            // Now check if the finalContent string is not null or whitespace
            if (!string.IsNullOrWhiteSpace(finalContent))
            {
                // Return the trimmed content
                return finalContent.Trim();
            }
            else
            {
                // Handle cases where the API call was successful but returned no content,
                // null content (either C# null or JSON null via JsonElement), or whitespace content.
                // You might want to inspect chatResponse more here if needed for debugging
                return "OpenAI API returned empty, whitespace, or null content.";
            }
        }
        catch (Exception ex)
        {
            // Handle any other exceptions (network, deserialization, etc.)
            // Include the exception type for better diagnosis
            return $"An unexpected error occurred calling OpenAI API: {ex.GetType().Name} - {ex.Message}";
        }
    }
    public void Dispose()
    {
        _openAIClient?.Dispose();
    }
}
