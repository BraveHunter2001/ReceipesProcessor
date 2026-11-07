using Infrastructure;
using OpenCvSharp;
using Tesseract;

namespace Application.Service;

public class TextExctractor : IDisposable
{
    TesseractEngine engine;
    PaddleOcrClient paddleOcrClient;
    public TextExctractor(string path, PaddleOcrClient paddleOcrClient)
    {
        engine = new TesseractEngine(path, "rus+eng", EngineMode.Default);
        this.paddleOcrClient = paddleOcrClient;
    }

    public void Dispose()
    {
        engine.Dispose();
    }

    public async Task<string> Exctract(MemoryStream imageStream)
    {
        imageStream.Seek(0, SeekOrigin.Begin);
        byte[] imageBytes = imageStream.GetBuffer();


        // --- ЭТАП 1: ОБРАБОТКА В OPENCV ИЗ ПАМЯТИ ---
        // Загружаем Mat напрямую из массива байт
        using Mat src = Cv2.ImDecode(imageBytes, ImreadModes.Color);

        if (src.Empty())
            throw new Exception("Не удалось загрузить изображение из MemoryStream.");

        // 1. Конвертация в оттенки серого
        using Mat gray = new Mat();
        Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

        // 2. Увеличение размера (Cubic интерполяция для четкости)
        using Mat resized = new Mat();
        Cv2.Resize(gray, resized, new Size(), 2.0, 2.0, InterpolationFlags.Cubic);

        // 3. Размытие для удаления шумов
        using Mat blurred = new Mat();
        Cv2.GaussianBlur(resized, blurred, new Size(3, 3), 0);

        // 4. Адаптивная бинаризация
        using Mat binary = new Mat();
        Cv2.AdaptiveThreshold(
            blurred,
            binary,
            255,
            AdaptiveThresholdTypes.GaussianC,
            ThresholdTypes.Binary,
            15,
            10
        );

        // Кодируем обработанную матрицу обратно в байты (в формате PNG, чтобы не терять качество)
        byte[] processedBytes = binary.ImEncode(".png");

        MemoryStream ms = new MemoryStream(processedBytes);
        ms.Seek(0, SeekOrigin.Begin);
        return await paddleOcrClient.RecognizeReceiptAsync(ms);
    }

    private string TesseractProccess(byte[] processedBytes)
    {
        using var img = Pix.LoadFromMemory(processedBytes);
        using var page = engine.Process(img);

        return page.GetText();
    }
}
