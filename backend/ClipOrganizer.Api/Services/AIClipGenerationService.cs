using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ClipOrganizer.Api.Models;
using ClipOrganizer.Api.DTOs;

namespace ClipOrganizer.Api.Services;

public class AIClipGenerationService : IAIClipGenerationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AIClipGenerationService> _logger;
    private readonly HttpClient _httpClient;

    public AIClipGenerationService(IConfiguration configuration, ILogger<AIClipGenerationService> logger, HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<AIClipGenerationResult> GenerateClipMetadataAsync(string userNotes, List<AvailableTag> availableTags)
    {
        var apiKey = _configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Gemini API key not configured. Using fallback generation.");
            return GenerateFallbackMetadata(userNotes, availableTags);
        }

        try
        {
            // Build tag context for the prompt
            var tagContext = BuildTagContext(availableTags);
            var categoryList = string.Join(", ", Enum.GetNames<TagCategory>());

            var prompt = $@"You are an assistant that helps categorize field hockey video clips. Based on the user's notes about a video clip, generate:
1. A concise, descriptive title (max 100 characters)
2. A detailed description (2-4 sentences)
3. Suggested tags from the available tags below (if they match)
4. Suggested new tags (if the concept isn't covered by existing tags)

User's notes: {userNotes}

Available tags by category:
{tagContext}

Valid tag categories: {categoryList}

Respond with a JSON object in this exact format:
{{
  ""title"": ""The generated title"",
  ""description"": ""The generated description"",
  ""suggestedTags"": [""tag1"", ""tag2"", ""tag3""],
  ""suggestedNewTags"": [
    {{""category"": ""SkillTactic"", ""value"": ""New Tag Name""}},
    {{""category"": ""FieldArea"", ""value"": ""Another Tag""}}
  ]
}}

Rules:
- suggestedTags: Array of tag values (strings) that match exactly from the available tags above. Only include tags that exist.
- suggestedNewTags: Array of objects with ""category"" and ""value"" properties. Only suggest new tags if the concept isn't covered by existing tags. Category must be one of: {categoryList}
- Only suggest tags (existing or new) that are relevant to the clip based on the user's notes.";

            var model = _configuration["Gemini:Model"] ?? "gemini-1.5-flash";
            var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

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
                systemInstruction = new
                {
                    parts = new[]
                    {
                        new { text = "You are a helpful assistant that generates metadata for field hockey video clips. Always respond with valid JSON." }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 500
                }
            };

            var requestContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            _httpClient.DefaultRequestHeaders.Clear();

            var response = await _httpClient.PostAsync(apiUrl, requestContent);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent);

            if (apiResponse?.Candidates == null || apiResponse.Candidates.Length == 0)
            {
                throw new Exception("No response from Gemini API");
            }

            var aiContent = apiResponse.Candidates[0].Content?.Parts?[0]?.Text;
            if (string.IsNullOrWhiteSpace(aiContent))
            {
                throw new Exception("Empty response from Gemini API");
            }

            var result = ParseAIResponse(aiContent, availableTags);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API. Using fallback generation.");
            return GenerateFallbackMetadata(userNotes, availableTags);
        }
    }

    private string BuildTagContext(List<AvailableTag> availableTags)
    {
        var groupedTags = availableTags.GroupBy(t => t.Category);
        var contextBuilder = new StringBuilder();

        foreach (var group in groupedTags)
        {
            contextBuilder.AppendLine($"{group.Key}:");
            foreach (var tag in group)
            {
                contextBuilder.AppendLine($"  - {tag.Value} (ID: {tag.Id})");
            }
        }

        return contextBuilder.ToString();
    }

    private AIClipGenerationResult ParseAIResponse(string aiContent, List<AvailableTag> availableTags)
    {
        try
        {
            // Try to extract JSON from the response (in case it's wrapped in markdown code blocks)
            var jsonStart = aiContent.IndexOf('{');
            var jsonEnd = aiContent.LastIndexOf('}') + 1;
            
            if (jsonStart < 0 || jsonEnd <= jsonStart)
            {
                throw new Exception("No JSON found in AI response");
            }

            var jsonContent = aiContent.Substring(jsonStart, jsonEnd - jsonStart);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var parsed = JsonSerializer.Deserialize<AIParsedResponse>(jsonContent, options);

            if (parsed == null)
            {
                throw new Exception("Failed to parse AI response");
            }

            // Match suggested tag values to tag IDs
            var suggestedTagIds = new List<int>();
            foreach (var suggestedTag in parsed.SuggestedTags ?? new List<string>())
            {
                var matchingTag = availableTags.FirstOrDefault(t => 
                    t.Value.Equals(suggestedTag, StringComparison.OrdinalIgnoreCase));
                
                if (matchingTag != null)
                {
                    suggestedTagIds.Add(matchingTag.Id);
                }
            }

            // Parse and validate suggested new tags
            var suggestedNewTags = new List<NewTagDto>();
            foreach (var newTag in parsed.SuggestedNewTags ?? new List<AIParsedNewTag>())
            {
                // Validate category is a valid enum value
                if (!string.IsNullOrWhiteSpace(newTag.Category) && 
                    !string.IsNullOrWhiteSpace(newTag.Value) &&
                    Enum.TryParse<TagCategory>(newTag.Category, ignoreCase: true, out _))
                {
                    suggestedNewTags.Add(new NewTagDto
                    {
                        Category = newTag.Category,
                        Value = newTag.Value.Trim()
                    });
                }
            }

            return new AIClipGenerationResult
            {
                Title = parsed.Title ?? string.Empty,
                Description = parsed.Description ?? string.Empty,
                SuggestedTagIds = suggestedTagIds,
                SuggestedNewTags = suggestedNewTags
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing AI response: {Content}", aiContent);
            throw;
        }
    }

    private AIClipGenerationResult GenerateFallbackMetadata(string userNotes, List<AvailableTag> availableTags)
    {
        // Simple fallback: use first sentence as title, notes as description
        var sentences = userNotes.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        var title = sentences.Length > 0 ? sentences[0].Trim() : "New Clip";
        if (title.Length > 100)
        {
            title = title.Substring(0, 97) + "...";
        }

        return new AIClipGenerationResult
        {
            Title = title,
            Description = userNotes,
            SuggestedTagIds = new List<int>(),
            SuggestedNewTags = new List<NewTagDto>()
        };
    }

    private class GeminiResponse
    {
        public GeminiCandidate[]? Candidates { get; set; }
    }

    private class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
    }

    private class GeminiContent
    {
        public GeminiPart[]? Parts { get; set; }
    }

    private class GeminiPart
    {
        public string? Text { get; set; }
    }

    private class AIParsedResponse
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public List<string>? SuggestedTags { get; set; }
        public List<AIParsedNewTag>? SuggestedNewTags { get; set; }
    }

    private class AIParsedNewTag
    {
        public string? Category { get; set; }
        public string? Value { get; set; }
    }
}

