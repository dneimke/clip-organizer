using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClipOrganizer.Api.Services;

public class AIQueryService : IAIQueryService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AIQueryService> _logger;
    private readonly HttpClient _httpClient;

    public AIQueryService(IConfiguration configuration, ILogger<AIQueryService> logger, HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<QueryParseResult> ParseQueryAsync(string userQuery, QueryContext context)
    {
        var apiKey = _configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Gemini API key not configured. Returning empty query parse result.");
            return new QueryParseResult
            {
                InterpretedQuery = "AI query parsing unavailable (API key not configured)"
            };
        }

        try
        {
            // First, try to discover available models
            var availableModel = await DiscoverAvailableModelAsync(apiKey);
            if (string.IsNullOrWhiteSpace(availableModel))
            {
                _logger.LogWarning("No available Gemini models found. Returning empty result.");
                return new QueryParseResult
                {
                    InterpretedQuery = "No available Gemini models found. Please check your API key permissions."
                };
            }
            // Build tag context for the prompt
            var tagContext = BuildTagContext(context.AvailableTags);
            var subfolderList = context.AvailableSubfolders.Any() 
                ? string.Join(", ", context.AvailableSubfolders)
                : "None";

            var prompt = $@"You are an assistant that parses natural language queries about field hockey video clips into structured filter parameters.

User query: ""{userQuery}""

Available tags by category:
{tagContext}

Available subfolders: {subfolderList}

You MUST respond with ONLY a valid JSON object, no other text. Use this exact structure:
{{
  ""searchTerm"": ""keywords to search in titles/descriptions (or null if none)"",
  ""tagIds"": [1, 2, 3],
  ""subfolders"": [""subfolder1"", ""subfolder2""],
  ""sortBy"": ""dateAdded"" or ""title"" or null,
  ""sortOrder"": ""asc"" or ""desc"" or null,
  ""unclassifiedOnly"": true or false,
  ""interpretedQuery"": ""A brief explanation of what filters were applied""
}}

CRITICAL: Respond with ONLY the JSON object, no markdown, no code blocks, no explanations, no other text. Just the raw JSON.

Rules:
- tagIds: Array of tag IDs that match tags mentioned in the query. Match tags by their values (case-insensitive). Only include tags that exist in the available tags list above.
- subfolders: Array of subfolder names mentioned in the query (e.g., ""YouTube"", or specific folder names). Match case-insensitively.
- searchTerm: Extract keywords from the query that should be searched in clip titles/descriptions. IMPORTANT: Ignore generic terms that refer to video clips themselves such as ""clips"", ""videos"", ""drills"", ""footage"", ""recordings"", ""content"", ""media"". These are synonyms for clips and should NOT be used as search terms. Only extract meaningful keywords that would appear in clip titles/descriptions (e.g., skill names, player names, specific techniques). Use null if no specific keywords remain after filtering.
- sortBy: Set to ""dateAdded"" if query mentions ""recent"", ""new"", ""latest"", ""old"", ""oldest"", or ""date"". Set to ""title"" if query mentions ""alphabetical"", ""name"", or ""title"". Use null otherwise.
- sortOrder: Set to ""desc"" for ""recent"", ""new"", ""latest"". Set to ""asc"" for ""old"", ""oldest"", ""alphabetical"". Use null if sortBy is null.
- unclassifiedOnly: Set to true if query asks for ""unclassified"", ""untagged"", ""uncategorized"", or ""missing tags"" clips.
- interpretedQuery: A brief human-readable explanation of what the query was understood to mean.

Examples:
- ""Show me clips with successful PC attacks"" → tagIds: [PC Attack tag ID, Success tag ID], searchTerm: null, interpretedQuery: ""Clips tagged with PC Attack and Success""
- ""Find videos in the midfield area"" → tagIds: [Midfield tag ID], searchTerm: null, interpretedQuery: ""Clips tagged with Midfield""
- ""Goal keeper drills"" → tagIds: [Goal Keeper tag ID], searchTerm: null (ignore ""drills"" as it's a synonym for clips), interpretedQuery: ""Clips tagged with Goal Keeper""
- ""What unclassified clips do I have?"" → unclassifiedOnly: true, searchTerm: null, interpretedQuery: ""Unclassified clips only""
- ""Show me recent clips with flick skills"" → tagIds: [Flick tag ID], searchTerm: null, sortBy: ""dateAdded"", sortOrder: ""desc"", interpretedQuery: ""Recent clips tagged with Flick, sorted by date added (newest first)""
- ""Find clips from YouTube folder"" → subfolders: [""YouTube""], searchTerm: null, interpretedQuery: ""Clips from YouTube subfolder""
- ""Show me defender clips from last month"" → tagIds: [Defender tag ID], searchTerm: null, interpretedQuery: ""Clips tagged with Defender"" (note: date filtering not supported, so ignore temporal references)
- ""Find backhand tip clips"" → searchTerm: ""backhand tip"", tagIds: [], interpretedQuery: ""Clips with 'backhand tip' in title or description"" (""clips"" is ignored as generic term)
- ""Goal keeper training videos"" → tagIds: [Goal Keeper tag ID], searchTerm: ""training"", interpretedQuery: ""Clips tagged with Goal Keeper and searching for 'training'"" (""videos"" is ignored, but ""training"" is a meaningful keyword)";

            // Use the discovered model or fallback to configured/default
            var model = availableModel ?? (_configuration["Gemini:Model"] ?? "gemini-1.5-flash");
            // Try v1beta first (most models available there), then v1
            var apiVersions = new[] { "v1beta", "v1" };
            Exception? lastException = null;
            
            foreach (var apiVersion in apiVersions)
            {
                try
                {
                    var apiUrl = $"https://generativelanguage.googleapis.com/{apiVersion}/models/{model}:generateContent?key={apiKey}";
                    _logger.LogDebug("Trying Gemini API with model: {Model}, version: {Version}", model, apiVersion);
                    
                    return await CallGeminiAPI(apiUrl, prompt, context, apiVersion);
                }
                catch (Exception ex) when (ex.Message.Contains("404") || ex.Message.Contains("NOT_FOUND") || ex.Message.Contains("400") || ex.Message.Contains("INVALID_ARGUMENT"))
                {
                    _logger.LogWarning("Model {Model} failed in {Version}: {Error}, trying next version", model, apiVersion, ex.Message);
                    lastException = ex;
                    continue;
                }
            }
            
            // If all versions failed, throw the last exception
            if (lastException != null)
            {
                throw new Exception($"Model {model} failed in all API versions. Last error: {lastException.Message}", lastException);
            }
            
            throw new Exception("Failed to call Gemini API");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API for query parsing. Returning empty result.");
            return new QueryParseResult
            {
                InterpretedQuery = $"Error parsing query: {ex.Message}"
            };
        }
    }

    private async Task<QueryParseResult> CallGeminiAPI(string apiUrl, string prompt, QueryContext context, string apiVersion)
    {
        // v1beta supports systemInstruction, but v1 does not
        // For v1, we need to include the instruction in the prompt itself
        var systemInstructionText = "You are a JSON-only API. You parse natural language queries about video clips and return ONLY valid JSON objects. Never include markdown, code blocks, explanations, or any other text. Just pure JSON.";
        var fullPrompt = apiVersion == "v1beta" 
            ? prompt 
            : $"{systemInstructionText}\n\n{prompt}";

        object requestBody;
        if (apiVersion == "v1beta")
        {
            requestBody = new
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
                        new { text = $"{systemInstructionText} You MUST respond with ONLY valid JSON, no markdown code blocks, no explanations, no other text." }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.3,
                    maxOutputTokens = 2048
                }
            };
        }
        else
        {
            requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = fullPrompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.3,
                    maxOutputTokens = 2048
                }
            };
        }

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
            _logger.LogError("Gemini API error: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
            throw new Exception($"Gemini API returned {response.StatusCode}: {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Gemini API response: {Response}", responseContent);
        
        var apiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        if (apiResponse?.Candidates == null || apiResponse.Candidates.Length == 0)
        {
            _logger.LogError("No candidates in Gemini API response. Response: {Response}", responseContent);
            throw new Exception("No response from Gemini API");
        }

        var candidate = apiResponse.Candidates[0];
        var finishReason = candidate.FinishReason;
        
        // Check if response was cut off
        if (finishReason == "MAX_TOKENS")
        {
            _logger.LogWarning("Response hit MAX_TOKENS limit. Consider increasing maxOutputTokens. Response: {Response}", responseContent);
        }
        
        var aiContent = candidate.Content?.Parts?[0]?.Text;
        if (string.IsNullOrWhiteSpace(aiContent))
        {
            _logger.LogError("Empty content in Gemini API response. FinishReason: {FinishReason}, Response: {Response}", finishReason, responseContent);
            throw new Exception($"Empty response from Gemini API. FinishReason: {finishReason}");
        }
        
        _logger.LogDebug("Extracted AI content: {Content}", aiContent);

        var result = ParseAIResponse(aiContent, context);

        return result;
    }

    private async Task<string?> DiscoverAvailableModelAsync(string apiKey)
    {
        try
        {
            // Try to list models from v1beta API
            var listModelsUrl = $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}";
            _logger.LogDebug("Discovering available Gemini models...");
            
            _httpClient.DefaultRequestHeaders.Clear();
            var response = await _httpClient.GetAsync(listModelsUrl);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("ListModels response: {Response}", responseContent);
                
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                
                // Try to parse as object with Models property
                var modelsResponse = JsonSerializer.Deserialize<ModelsListResponse>(responseContent, options);
                List<ModelInfo>? models = modelsResponse?.Models;
                
                // If that didn't work, try parsing as direct array
                if (models == null || !models.Any())
                {
                    try
                    {
                        models = JsonSerializer.Deserialize<List<ModelInfo>>(responseContent, options);
                    }
                    catch
                    {
                        // Ignore, we'll log below
                    }
                }
                
                if (models != null && models.Any())
                {
                    _logger.LogDebug("Found {Count} models in response", models.Count);
                    
                    // Look for models that support generateContent
                    var supportedModel = models
                        .FirstOrDefault(m => m.SupportedGenerationMethods != null && 
                                            m.SupportedGenerationMethods.Any(method => 
                                                method.Contains("generateContent", StringComparison.OrdinalIgnoreCase)));
                    
                    if (supportedModel != null)
                    {
                        var modelName = ExtractModelName(supportedModel.Name);
                        _logger.LogInformation("Discovered available model: {Model}", modelName);
                        return modelName;
                    }
                    
                    // If no model explicitly supports generateContent, try the first one
                    var firstModel = models.FirstOrDefault();
                    if (firstModel != null)
                    {
                        var modelName = ExtractModelName(firstModel.Name);
                        _logger.LogInformation("Using first available model: {Model} (generateContent support not verified)", modelName);
                        return modelName;
                    }
                }
                else
                {
                    _logger.LogWarning("ListModels returned success but no models found. Response: {Response}", responseContent);
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to list models: {StatusCode} - {Error}", response.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error discovering available models, will use configured/default model");
        }
        
        return null;
    }

    private QueryParseResult ParseTextResponse(string textContent, QueryContext context)
    {
        // Fallback: try to extract information from text response
        // This is a simple fallback - in a real scenario, you might want to use regex or NLP
        var result = new QueryParseResult
        {
            InterpretedQuery = "AI response was not in expected JSON format. Attempting to parse text response."
        };
        
        var textLower = textContent.ToLowerInvariant();
        
        // Try to find tag mentions in the text
        foreach (var tag in context.AvailableTags)
        {
            if (textLower.Contains(tag.Value.ToLowerInvariant()))
            {
                result.TagIds.Add(tag.Id);
            }
        }
        
        // Try to find subfolder mentions
        foreach (var subfolder in context.AvailableSubfolders)
        {
            if (textLower.Contains(subfolder.ToLowerInvariant()))
            {
                result.Subfolders.Add(subfolder);
            }
        }
        
        // Check for unclassified mentions
        if (textLower.Contains("unclassified") || textLower.Contains("untagged") || textLower.Contains("uncategorized"))
        {
            result.UnclassifiedOnly = true;
        }
        
        return result;
    }

    private string ExtractModelName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;
            
        // Remove "models/" prefix if present
        return name.Replace("models/", "").Trim();
    }

    private string BuildTagContext(List<AvailableTag> availableTags)
    {
        if (availableTags == null || !availableTags.Any())
        {
            return "No tags available.";
        }

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

    private QueryParseResult ParseAIResponse(string aiContent, QueryContext context)
    {
        try
        {
            _logger.LogDebug("Parsing AI response content: {Content}", aiContent);
            
            // Try to extract JSON from the response (in case it's wrapped in markdown code blocks)
            // First, try to find JSON in markdown code blocks
            var jsonContent = aiContent;
            var codeBlockStart = aiContent.IndexOf("```json");
            if (codeBlockStart >= 0)
            {
                var codeBlockEnd = aiContent.IndexOf("```", codeBlockStart + 7);
                if (codeBlockEnd > codeBlockStart)
                {
                    jsonContent = aiContent.Substring(codeBlockStart + 7, codeBlockEnd - codeBlockStart - 7).Trim();
                }
            }
            else
            {
                // Try regular code blocks
                codeBlockStart = aiContent.IndexOf("```");
                if (codeBlockStart >= 0)
                {
                    var codeBlockEnd = aiContent.IndexOf("```", codeBlockStart + 3);
                    if (codeBlockEnd > codeBlockStart)
                    {
                        jsonContent = aiContent.Substring(codeBlockStart + 3, codeBlockEnd - codeBlockStart - 3).Trim();
                    }
                }
                else
                {
                    // Extract JSON between first { and last }
                    var jsonStart = aiContent.IndexOf('{');
                    var jsonEnd = aiContent.LastIndexOf('}') + 1;
                    
                    if (jsonStart < 0 || jsonEnd <= jsonStart)
                    {
                        _logger.LogWarning("No JSON found in AI response. Content: {Content}", aiContent);
                        // Try to parse as plain text and extract information manually
                        return ParseTextResponse(aiContent, context);
                    }
                    
                    jsonContent = aiContent.Substring(jsonStart, jsonEnd - jsonStart);
                }
            }
            
            _logger.LogDebug("Extracted JSON content: {JsonContent}", jsonContent);
            
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var parsed = JsonSerializer.Deserialize<AIParsedQueryResponse>(jsonContent, options);

            if (parsed == null)
            {
                throw new Exception("Failed to parse AI response");
            }

            // Match tag IDs
            var tagIds = new List<int>();
            if (parsed.TagIds != null && parsed.TagIds.Any())
            {
                // Validate that tag IDs exist in context
                var validTagIds = context.AvailableTags.Select(t => t.Id).ToList();
                foreach (var tagId in parsed.TagIds)
                {
                    if (validTagIds.Contains(tagId))
                    {
                        tagIds.Add(tagId);
                    }
                }
            }

            // Match subfolders (case-insensitive)
            var subfolders = new List<string>();
            if (parsed.Subfolders != null && parsed.Subfolders.Any())
            {
                foreach (var subfolder in parsed.Subfolders)
                {
                    var matchingSubfolder = context.AvailableSubfolders.FirstOrDefault(s => 
                        s.Equals(subfolder, StringComparison.OrdinalIgnoreCase));
                    if (matchingSubfolder != null)
                    {
                        subfolders.Add(matchingSubfolder);
                    }
                }
            }

            // Validate sortBy
            var sortBy = parsed.SortBy;
            if (!string.IsNullOrWhiteSpace(sortBy) && sortBy != "dateAdded" && sortBy != "title")
            {
                sortBy = null;
            }

            // Validate sortOrder
            var sortOrder = parsed.SortOrder;
            if (!string.IsNullOrWhiteSpace(sortOrder) && sortOrder != "asc" && sortOrder != "desc")
            {
                sortOrder = null;
            }

            // If sortBy is null, sortOrder should also be null
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                sortOrder = null;
            }

            return new QueryParseResult
            {
                SearchTerm = string.IsNullOrWhiteSpace(parsed.SearchTerm) ? null : parsed.SearchTerm.Trim(),
                TagIds = tagIds,
                Subfolders = subfolders,
                SortBy = sortBy,
                SortOrder = sortOrder,
                UnclassifiedOnly = parsed.UnclassifiedOnly ?? false,
                InterpretedQuery = parsed.InterpretedQuery ?? "Query parsed successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing AI response: {Content}", aiContent);
            throw;
        }
    }

    private class GeminiResponse
    {
        public GeminiCandidate[]? Candidates { get; set; }
    }

    private class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
        public string? FinishReason { get; set; }
        public int Index { get; set; }
    }

    private class GeminiContent
    {
        public GeminiPart[]? Parts { get; set; }
    }

    private class GeminiPart
    {
        public string? Text { get; set; }
    }

    private class AIParsedQueryResponse
    {
        public string? SearchTerm { get; set; }
        public List<int>? TagIds { get; set; }
        public List<string>? Subfolders { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }
        public bool? UnclassifiedOnly { get; set; }
        public string? InterpretedQuery { get; set; }
    }

    private class ModelsListResponse
    {
        public List<ModelInfo>? Models { get; set; }
    }

    private class ModelInfo
    {
        public string? Name { get; set; }
        public List<string>? SupportedGenerationMethods { get; set; }
    }
}

