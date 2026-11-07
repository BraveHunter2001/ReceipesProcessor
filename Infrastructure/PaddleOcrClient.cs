using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public class PaddleOcrClient
{
    private readonly HttpClient _httpClient;
    // Адрес вашего Python-сервиса (порт 8000 по умолчанию для FastAPI/Uvicorn)
    private readonly PaddleOcrSettings _settings;

    public PaddleOcrClient(HttpClient httpClient, IOptions<PaddleOcrSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;
    }

    /// <summary>
    /// Отправляет MemoryStream с изображением чека в микросервис и возвращает распознанный текст.
    /// </summary>
    public async Task<string> RecognizeReceiptAsync(MemoryStream imageStream, string fileName = "receipt.png")
    {
        // Сбрасываем позицию потока в начало перед чтением
        imageStream.Position = 0;

        using var content = new MultipartFormDataContent();
        // Упаковываем MemoryStream для передачи
        var streamContent = new StreamContent(imageStream);

        // Задаем MIME-тип контента (сервис принимает png, jpeg, jpg)
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        // Имя параметра должно быть строго "file", как указано в Python в функции recognize_receipt
        content.Add(streamContent, "file", fileName);

        // Выполняем асинхронный POST-запрос к микросервису
        string baseUrl = _settings.BaseUrl.TrimEnd('/');
        HttpResponseMessage response = await _httpClient.PostAsync($"{baseUrl}/ocr/receipt", content);

        if (!response.IsSuccessStatusCode)
        {
            string errorLog = await response.Content.ReadAsStringAsync();
            throw new Exception($"Микросервис вернул ошибку: {response.StatusCode}. Детали: {errorLog}");
        }

        // Читаем тело JSON-ответа
        string jsonResponse = await response.Content.ReadAsStringAsync();

        // Десериализуем структуру данных
        var ocrResult = JsonSerializer.Deserialize<OcrResponse>(jsonResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Собираем текст построчно
        var textLines = new List<string>();
        if (ocrResult?.Lines != null)
        {
            foreach (var line in ocrResult.Lines)
            {
                textLines.Add(line.Text);
            }
        }

        return string.Join(Environment.NewLine, textLines);
    }
}
public class OcrResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("lines")]
    public List<OcrLine>? Lines { get; set; }
}

public class OcrLine
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    // Координаты рамки текста на изображении: [[x1,y1], [x2,y2], [x3,y3], [x4,y4]]
    [JsonPropertyName("box")]
    public List<List<double>> Box { get; set; } = [];
}
