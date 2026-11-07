using Application.Models;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Application.Service;

public class ReceiptService(ApplicationContext context)
{

    public async Task<List<ReceiptsListItem>> GetList()
    {
        var receipts = await context.Receipts.Include(r => r.Items).OrderByDescending(r => r.Id).ToListAsync();
        return receipts.ConvertAll(r => new ReceiptsListItem(r));
    }
}
