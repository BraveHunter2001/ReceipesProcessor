using System.Text;
using System.Text.Json;
using Application.Configuration;
using Microsoft.Extensions.Options;

namespace Application.Service;

public class ReceiptTextService
{
    private readonly OllamaSettings ollamaSettings;
    private readonly IHttpClientFactory httpClientFactory;

    public ReceiptTextService(IOptions<OllamaSettings> ollamaOptions, IHttpClientFactory httpClientFactory)
    {
        ollamaSettings = ollamaOptions.Value;
        this.httpClientFactory = httpClientFactory;
    }

    const string JsonPromt = $@"Ты — умный парсер чеков. Твоя задача — проанализировать сырой текст распознанного чека и извлечь из него структуру. Текст чека может быть некрасивым или спутанным.
Ты должен вернуть ТОЛЬКО чистый JSON объект, без разметки ```json, без вступлений и объяснений.

Обязательная структура JSON:
{{
    ""total_sum"": float (итоговая сумма к оплате по чеку),
    ""currency"": string (код валюты, по умолчанию ""RUB""),
    ""items"": [
        {{
            ""name"": string (название товара или услуги),
            ""quantity"": float (количество товара, если не указано — 1),
            ""price"": float (цена за одну единицу товара),
            ""sum"": float (общая стоимость этой позиции)
        }}
    ]
}}
";

    public async Task<ReceiptDto> Process(string ocrText)
    {
        using var httpClient = httpClientFactory.CreateClient("Ollama");
        httpClient.Timeout = ollamaSettings.Timeout;

        var requestBody = new
        {
            model = ollamaSettings.Model,
            prompt = $"{JsonPromt}. Текст чека для парсинга: {ocrText}",
            stream = false,
            options = new { temperature = 0.0 },
            format = "json" // Флаг Ollama для строгого JSON
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        string baseUrl = string.IsNullOrWhiteSpace(ollamaSettings.BaseUrl)
            ? OllamaSettings.DefaultBaseUrl
            : ollamaSettings.BaseUrl.TrimEnd('/');
        HttpResponseMessage response = await httpClient.PostAsync($"{baseUrl}/api/generate", content);
        response.EnsureSuccessStatusCode();

        string responseString = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(responseString);
        string llmJsonOutput = doc.RootElement.GetProperty("response").GetString()?.Trim() ?? string.Empty;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<ReceiptDto>(llmJsonOutput, options) ?? throw new Exception("Error create recipt");
    }
}
