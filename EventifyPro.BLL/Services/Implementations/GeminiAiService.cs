namespace EventifyPro.BLL.Services.Implementations;

public class GeminiAiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly ISystemSettingService _systemSettingService;

    public GeminiAiService(HttpClient httpClient, ISystemSettingService systemSettingService)
    {
        _httpClient = httpClient;
        _systemSettingService = systemSettingService;
    }

    public async Task<string> GenerateEventDescriptionAsync(string title, string city, string category, CancellationToken cancellationToken = default)
    {
        var apiKey = await _systemSettingService.GetSettingValueAsync("GeminiApiKey", "", cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured in the system settings. Please contact the administrator.");
        }
        apiKey = apiKey.Trim();

        var prompt = $"Write a professional, rich, and engaging event description of more than 150 words (but strictly less than 1800 characters total) for an event. Details: Title: '{title}', City: '{city}', Category: '{category}'. Use rich paragraphs, bullet points for what to expect/agenda/highlights, and a final call to action. IMPORTANT: The total response length MUST be under 1800 characters. Do NOT output any markdown syntax, symbols or styling. Do not use asterisks (*) or double asterisks (**) for bolding or bullet points. Use standard plain text paragraphs, and for bullet points use simple dashes (-) followed by a space. Section headers should be in capital letters without markdown tags. Return ONLY the plain text description, without any markdown wrappers or additional text.";

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
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";
        
        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception($"Gemini API call failed: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);
        
        try
        {
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (text != null && text.Length > 2000)
            {
                text = text.Substring(0, 1997) + "...";
            }

            return text ?? string.Empty;
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to parse response from Gemini API.", ex);
        }
    }

}
