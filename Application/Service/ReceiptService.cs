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
}
