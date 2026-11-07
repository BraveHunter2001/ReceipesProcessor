using Application.Models;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Application.Service;

public class ReceiptService(ApplicationContext context, S3StorageProvider storageProvider)
{

    public async Task<ReceiptsListItem> CreateFromImageAsync(
        Stream imageStream,
        string contentType,
        CancellationToken cancellationToken)
    {
        var extension = contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".img"
        };
        var imageId = $"web/{Guid.NewGuid():N}{extension}";

        await storageProvider.UploadFileAsync(imageStream, imageId, contentType);

        var receipt = new Receipt
        {
            OwnerKey = "web",
            Status = ReceiptStatus.Created,
            ImageId = imageId
        };
        context.Receipts.Add(receipt);
        context.ReceiptHistories.Add(new ReceiptHistory
        {
            Receipt = receipt,
            Status = ReceiptStatus.Created,
            AtRecord = DateTime.UtcNow
        });
        await context.SaveChangesAsync(cancellationToken);

        return new ReceiptsListItem(receipt);
    }

    public async Task<List<ReceiptsListItem>> GetList()
    {
        var receipts = await context.Receipts.Include(r => r.Items).OrderByDescending(r => r.Id).ToListAsync();
        return receipts.ConvertAll(r => new ReceiptsListItem(r));
    }

    public async Task<ReceiptDetails?> GetDetailsAsync(int id, CancellationToken cancellationToken)
    {
        var receipt = await context.Receipts
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (receipt is null)
        {
            return null;
        }

        var history = await context.ReceiptHistories
            .Where(item => item.ReceiptId == id)
            .OrderByDescending(item => item.AtRecord)
            .ToListAsync(cancellationToken);

        return new ReceiptDetails(receipt, history);
    }

    public async Task<(Stream FileStream, string ContentType)> GetImageAsync(int id)
    {
        var imageId = await context.Receipts
            .Where(item => item.Id == id)
            .Select(item => item.ImageId)
            .SingleOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(imageId))
        {
            throw new FileNotFoundException($"Изображение для чека #{id} не найдено.");
        }

        return await storageProvider.GetFileAsync(imageId);
    }
}
