using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReceipesProcessor.Service;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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
        TextExctractor exctractor = scope.ServiceProvider.GetRequiredService<TextExctractor>();
        ReceiptTextService receiptService = scope.ServiceProvider.GetRequiredService<ReceiptTextService>();

        if (update.Message is not { Photo: { } photos } message)
            return;

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
        string raw = exctractor.Exctract(imageStream);

        var receipt = await receiptService.Process(raw);
        var print = FormatReceiptMessage(receipt);

        Message sentMessage2 = await botClient.SendMessage(
          chatId: chatId,
          text: print,
          parseMode: ParseMode.Html,
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


    static string FormatReceiptMessage(ReceiptDto receipt)
    {
        if (receipt == null) return "Чек пуст";

        var sb = new StringBuilder();

        // Заголовок
        sb.AppendLine("<b>🧾 Ваш чек</b>");
        sb.AppendLine("──────────────────────────");

        if (receipt.Items != null && receipt.Items.Length > 0)
        {
            for (int i = 0; i < receipt.Items.Length; i++)
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
 
