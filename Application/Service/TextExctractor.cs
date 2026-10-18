using Tesseract;

namespace Application.Service;

public class TextExctractor : IDisposable
{
    TesseractEngine engine;
    public TextExctractor(string path)
    {
        engine = new TesseractEngine(path, "rus+eng", EngineMode.Default);
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
