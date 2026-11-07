using Application.Service;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Application.Configuration;

namespace ReceipesProcessor.BackgroundServices;

internal class ReceiptProccedBackgroundService(
    ILogger<ReceiptExctractTextBackgroundService> logger,
    IServiceScopeFactory scopeFactory,
    IOptions<ReceiptProcessingSettings> options
) : BackgroundService
{
    private readonly PeriodicTimer _timer = new(options.Value.PollingInterval);
    private readonly int batchSize = options.Value.BatchSize;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Start scanning Exctracted receipts");
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
        ApplicationContext applicationContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        ReceiptTextService receiptTextService = scope.ServiceProvider.GetRequiredService<ReceiptTextService>();

        List<Receipt> receipts = await applicationContext.Receipts.Where(r => r.Status == ReceiptStatus.TextExctracted)
             .OrderBy(r=>r.Id)
             .Take(batchSize)
             .ToListAsync(stoppingToken);
        if (receipts.Count == 0) return;

        logger.LogInformation($"Loaded batch size {receipts.Count}");

        List<ReceiptHistory> batchHistory = new();
        foreach (Receipt receipt in receipts)
        {
            var dto = await receiptTextService.Process(receipt.RawText!);

            receipt.Currency = dto.Currency;
            receipt.TotalSum = dto.TotalSum;
            receipt.Items = dto.Items.Select(i => new ReceiptItem()
            {
                Name = i.Name,
                Price = i.Price,
                Quantity = i.Quantity,
                Sum = i.Sum
            }).ToList();


            receipt.Status = ReceiptStatus.Finished;
            ReceiptHistory history = new ReceiptHistory() { Receipt = receipt, Status = ReceiptStatus.Finished, AtRecord = DateTime.UtcNow };
            batchHistory.Add(history);
            logger.LogInformation($"Text procced to json from receipt#{receipt.Id}");
        }

        await applicationContext.ReceiptHistories.AddRangeAsync(batchHistory);
        applicationContext.UpdateRange(receipts);
        await applicationContext.SaveChangesAsync(stoppingToken);
    }
}
