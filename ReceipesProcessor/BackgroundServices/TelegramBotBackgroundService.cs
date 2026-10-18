using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

internal class TelegramBotBackgroundService(
    ITelegramBotClient client,
    IServiceScopeFactory scopeFactory,
    ILogger<TelegramBotBackgroundService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ReceiverOptions receiverOptions = new ReceiverOptions() { AllowedUpdates = [] };
        logger.LogInformation("Telegram bot start as background service..");

        client.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken
        );

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        S3StorageProvider storageProvider = scope.ServiceProvider.GetRequiredService<S3StorageProvider>();
        ApplicationContext applicationContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        if (update.Message is not { Photo: { } photos } message)
        {
            if (update.Message.Text is "/receipts")
            {
                var receipts = await applicationContext.Receipts
                    .Include(r => r.Items)
                    .Where(r=>r.Status == ReceiptStatus.Finished)
                    .ToListAsync();
                foreach (var outReceipt in receipts)
                {
                    await botClient.SendMessage(
                                   chatId: update.Message!.Chat.Id,
                                   text: FormatReceiptMessage(outReceipt),
                                   parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                   cancellationToken: cancellationToken
                               );
                }

                return;
            }


            await botClient.SendMessage(
                chatId: update.Message!.Chat.Id,
                text: $"Загрузите фото...",
                cancellationToken: cancellationToken
            );
            return;
        }

        var photo = photos.MaxBy(p => p.FileSize);
        var chatId = message.Chat.Id;
        var imageInfo = await botClient.GetFile(photo!.FileId);
        Message sentMessage = await botClient.SendMessage(
            chatId: chatId,
            text: $"Вы загрузили фото: {photo.FileId}",
            cancellationToken: cancellationToken
        );
        MemoryStream imageStream = new MemoryStream();
        await botClient.DownloadFile(imageInfo.FilePath!, imageStream, cancellationToken);


        string uploadedImageName = await storageProvider.UploadFileAsync(imageStream, $"{imageInfo.FileId}.png", "image/jpeg");

        Receipt receipt = new Receipt()
        {
            OwnerKey = chatId.ToString(),
            Status = ReceiptStatus.Created,
            ImageId = uploadedImageName
        };

        ReceiptHistory receiptHistory = new() { Receipt = receipt, Status = ReceiptStatus.Created, AtRecord = DateTime.UtcNow };

        await applicationContext.Receipts.AddAsync(receipt);
        await applicationContext.ReceiptHistories.AddAsync(receiptHistory);
        await applicationContext.SaveChangesAsync();

        await botClient.SendMessage(
          chatId: chatId,
          text: $"Чек создан #{receipt.Id}!",
          cancellationToken: cancellationToken
      );
    }

    Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Ошибка Telegram API:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }


    static string FormatReceiptMessage(Receipt receipt)
    {
        if (receipt == null) return "Чек пуст";

        var sb = new StringBuilder();

        // Заголовок
        sb.AppendLine($"<b>🧾 Ваш чек #{receipt.Id}</b>");
        sb.AppendLine("──────────────────────────");

        if (receipt.Items != null && receipt.Items.Count > 0)
        {
            for (int i = 0; i < receipt.Items.Count; i++)
            {
                var item = receipt.Items[i];

                // Название товара
                sb.AppendLine($"{i + 1}. <b>{item.Name}</b>");
                // Детали: Количество x Цена = Сумма позиции
                sb.AppendLine($"   {item.Quantity} шт. × {item.Price:F2} = {item.Sum:F2} {receipt.Currency}");
            }
            sb.AppendLine("──────────────────────────");
        }
        else
        {
            sb.AppendLine("В чеке нет товаров.\n──────────────────────────");
        }

        // Итоговая сумма
        sb.AppendLine($"<b>ИТОГО К ОПЛАТЕ:</b> {receipt.TotalSum:F2} {receipt.Currency}");

        return sb.ToString();
    }
}

