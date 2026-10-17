using Tesseract;

namespace ReceipesProcessor.Service;

internal class TextExctractor : IDisposable
{
    string tessdataPath = "./tessdata";
    TesseractEngine engine;
    public TextExctractor()
    {
        engine = new TesseractEngine(tessdataPath, "rus+eng", EngineMode.Default);
    }

    public void Dispose()
    {
        engine.Dispose();
    }

    public string Exctract(MemoryStream imageStream)
    {
        imageStream.Seek(0, SeekOrigin.Begin);
        var buffer = imageStream.GetBuffer();
        using var img = Pix.LoadFromMemory(buffer);
        using var page = engine.Process(img);

        return page.GetText();
    }
}
