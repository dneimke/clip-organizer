using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ClipOrganizer.Api.DTOs;
using ClipOrganizer.Api.Models;

namespace ClipOrganizer.Api.Services;

public class SessionPlanService : ISessionPlanService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SessionPlanService> _logger;
    private readonly HttpClient _httpClient;

    public SessionPlanService(IConfiguration configuration, ILogger<SessionPlanService> logger, HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<SessionPlanDto> GenerateSessionPlanAsync(GenerateSessionPlanDto request, List<Clip> availableClips)
    {
        if (availableClips == null || !availableClips.Any())
        {
            throw new ArgumentException("No clips available for session planning", nameof(availableClips));
        }

        var apiKey = _configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Gemini API key not configured. Using fallback generation.");
            return GenerateFallbackPlan(request, availableClips);
        }

        try
        {
            // Build clip context for the prompt
            var clipContext = BuildClipContext(availableClips);
            var focusAreasText = request.FocusAreas != null && request.FocusAreas.Any()
                ? string.Join(", ", request.FocusAreas)
                : "General Training";

            var prompt = $@"You are an assistant that helps create field hockey training session plans. Based on the user's requirements, select the most relevant clips and create a session plan.

User Requirements:
- Session Duration: {request.DurationMinutes} minutes
- Focus Areas: {focusAreasText}

Available Clips (these clips have already been filtered to match the focus areas):
{clipContext}

Your task:
1. Select clips from the available clips above that best fit within the {request.DurationMinutes}-minute duration
2. Ensure the total duration of selected clips does not exceed {request.DurationMinutes} minutes
3. Prioritize clips that have tags matching the focus areas: {focusAreasText}
4. Generate a concise, descriptive title for this session plan (max 100 characters)
5. Generate a brief summary (2-3 sentences) describing what this session covers

Respond with a JSON object in this exact format:
{{
  ""title"": ""Session Title"",
  ""summary"": ""Session summary describing what this session covers."",
  ""selectedClipIds"": [1, 5, 12, 23]
}}

Rules:
- selectedClipIds: Array of clip IDs from the available clips above ONLY
- Total duration of selected clips must be <= {request.DurationMinutes} minutes
- Only select clips from the provided list - all clips have been pre-filtered to match the focus areas
- Prioritize clips that have multiple tags matching the focus areas
- Aim for variety in the selection while respecting the focus areas";

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
                        new { text = "You are a helpful assistant that creates field hockey training session plans. Always respond with valid JSON." }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 1000
                }
            };

            var requestContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            _httpClient.DefaultRequestHeaders.Clear();

            var response = await _httpClient.PostAsync(apiUrl, requestContent);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API returned error status {StatusCode}: {Error}", response.StatusCode, errorContent);
                throw new Exception($"Gemini API error (Status {response.StatusCode}): {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent);

            if (apiResponse?.Candidates == null || apiResponse.Candidates.Length == 0)
            {
                _logger.LogError("No candidates in Gemini API response: {Response}", responseContent);
                throw new Exception("No response from Gemini API");
            }

            var aiContent = apiResponse.Candidates[0].Content?.Parts?[0]?.Text;
            if (string.IsNullOrWhiteSpace(aiContent))
            {
                _logger.LogError("Empty content in Gemini API response: {Response}", responseContent);
                throw new Exception("Empty response from Gemini API");
            }

            var result = ParseAIResponse(aiContent, availableClips, request.DurationMinutes);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API. Using fallback generation.");
            return GenerateFallbackPlan(request, availableClips);
        }
    }

    private string BuildClipContext(List<Clip> clips)
    {
        var contextBuilder = new StringBuilder();
        var durationSeconds = 0;

        foreach (var clip in clips)
        {
            var tags = clip.Tags.Any() 
                ? string.Join(", ", clip.Tags.Select(t => $"{t.Value} ({t.Category})"))
                : "No tags";
            var durationMinutes = clip.Duration / 60.0;
            contextBuilder.AppendLine($"ID: {clip.Id} | Title: {clip.Title} | Duration: {durationMinutes:F1} min | Tags: {tags}");
            durationSeconds += clip.Duration;
        }

        contextBuilder.AppendLine($"\nTotal available clips: {clips.Count}");
        contextBuilder.AppendLine($"Total available duration: {durationSeconds / 60.0:F1} minutes");
        contextBuilder.AppendLine($"\nNote: All clips above have been pre-filtered to match the requested focus areas.");

        return contextBuilder.ToString();
    }

    private SessionPlanDto ParseAIResponse(string aiContent, List<Clip> availableClips, int maxDurationMinutes)
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
            var parsed = JsonSerializer.Deserialize<AIParsedPlanResponse>(jsonContent, options);

            if (parsed == null)
            {
                throw new Exception("Failed to parse AI response");
            }

            // Validate and filter selected clip IDs
            var selectedClipIds = new List<int>();
            var totalDuration = 0;

            foreach (var clipId in parsed.SelectedClipIds ?? new List<int>())
            {
                var clip = availableClips.FirstOrDefault(c => c.Id == clipId);
                if (clip != null)
                {
                    var clipDurationMinutes = clip.Duration / 60.0;
                    if (totalDuration + clipDurationMinutes <= maxDurationMinutes)
                    {
                        selectedClipIds.Add(clipId);
                        totalDuration += (int)Math.Ceiling(clipDurationMinutes);
                    }
                }
            }

            // If no valid clips selected, use fallback
            if (!selectedClipIds.Any())
            {
                return GenerateFallbackPlan(new GenerateSessionPlanDto 
                { 
                    DurationMinutes = maxDurationMinutes, 
                    FocusAreas = new List<string>() 
                }, availableClips);
            }

            return new SessionPlanDto
            {
                Title = parsed.Title ?? "Training Session Plan",
                Summary = parsed.Summary ?? "A curated selection of clips for training.",
                ClipIds = selectedClipIds,
                CreatedDate = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing AI response: {Content}", aiContent);
            return GenerateFallbackPlan(new GenerateSessionPlanDto 
            { 
                DurationMinutes = 60, 
                FocusAreas = new List<string>() 
            }, availableClips);
        }
    }

    private SessionPlanDto GenerateFallbackPlan(GenerateSessionPlanDto request, List<Clip> availableClips)
    {
        // Simple fallback: select clips that fit within duration, prioritizing clips with tags
        var selectedClips = new List<Clip>();
        var totalDuration = 0;
        var maxDurationSeconds = request.DurationMinutes * 60;

        // Filter clips by focus areas if provided
        var filteredClips = availableClips;
        if (request.FocusAreas != null && request.FocusAreas.Any())
        {
            filteredClips = availableClips.Where(c =>
                c.Tags.Any(t => request.FocusAreas.Any(fa =>
                    t.Value.Contains(fa, StringComparison.OrdinalIgnoreCase) ||
                    fa.Contains(t.Value, StringComparison.OrdinalIgnoreCase)
                ))
            ).ToList();
        }

        // If no clips match focus areas, use all clips
        if (!filteredClips.Any())
        {
            filteredClips = availableClips;
        }

        // Sort by number of tags (more tags = more relevant) and select clips that fit
        var sortedClips = filteredClips
            .OrderByDescending(c => c.Tags.Count)
            .ThenByDescending(c => c.Duration)
            .ToList();

        foreach (var clip in sortedClips)
        {
            if (totalDuration + clip.Duration <= maxDurationSeconds)
            {
                selectedClips.Add(clip);
                totalDuration += clip.Duration;
            }
        }

        var focusAreasText = request.FocusAreas != null && request.FocusAreas.Any()
            ? string.Join(", ", request.FocusAreas)
            : "General Training";

        return new SessionPlanDto
        {
            Title = $"{request.DurationMinutes}-Minute {focusAreasText} Session",
            Summary = $"A {request.DurationMinutes}-minute training session focusing on {focusAreasText}. Contains {selectedClips.Count} clips with a total duration of {totalDuration / 60} minutes.",
            ClipIds = selectedClips.Select(c => c.Id).ToList(),
            CreatedDate = DateTime.UtcNow
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

    private class AIParsedPlanResponse
    {
        public string? Title { get; set; }
        public string? Summary { get; set; }
        public List<int>? SelectedClipIds { get; set; }
    }
}

