using Application.Service;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ReceipesProcessor.BackgroundServices;

internal class ReceiptExctractTextBackgroundService(
    ILogger<ReceiptExctractTextBackgroundService> logger,
    IServiceScopeFactory scopeFactory
) : BackgroundService
{
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(1));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Start scanning created receipts");
        while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoWorkAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError($"Ошибка выполнения задачи: {ex.Message}");
            }
        }
    }

    private async Task DoWorkAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        S3StorageProvider storageProvider = scope.ServiceProvider.GetRequiredService<S3StorageProvider>();
        ApplicationContext applicationContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        TextExctractor exctractor = scope.ServiceProvider.GetRequiredService<TextExctractor>();

        List<Receipt> receipts = await applicationContext.Receipts.Where(r => r.Status == ReceiptStatus.Created)
             .OrderBy(r=>r.Id)
             .Take(50)
             .ToListAsync(stoppingToken);
        if (receipts.Count == 0) return;

        logger.LogInformation($"Loaded batch size {receipts.Count}");

        List<ReceiptHistory> batchHistory = new();
        foreach (Receipt receipt in receipts)
        {
            var image = await storageProvider.GetFileAsync(receipt.ImageId!);
            using MemoryStream ms = new();
            await image.FileStream.CopyToAsync(ms);

            string text = exctractor.Exctract(ms);
            receipt.RawText = text;
            receipt.Status = ReceiptStatus.TextExctracted;
            ReceiptHistory history = new ReceiptHistory() { Receipt = receipt, Status = ReceiptStatus.TextExctracted, AtRecord = DateTime.UtcNow };
            batchHistory.Add(history);
            logger.LogInformation($"Text extracted from receipt#{receipt.Id}");
        }

        await applicationContext.ReceiptHistories.AddRangeAsync(batchHistory);
        applicationContext.UpdateRange(receipts);
        await applicationContext.SaveChangesAsync(stoppingToken);
    }
}
