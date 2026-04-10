using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.ViewModels;

namespace TranTranHiep_2122110538.Services;

public class InventoryService : IInventoryService
{
    private readonly AppDbContext _db;

    public InventoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(bool Ok, string? ErrorMessage)> TryDeductStockForLinesAsync(
        IReadOnlyList<CartItemDto> lines,
        CancellationToken ct = default)
    {
        foreach (var line in lines)
        {
            var n = await _db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE Foods SET StockQuantity = StockQuantity - {line.Quantity} WHERE Id = {line.FoodId} AND StockQuantity >= {line.Quantity}",
                ct);
            if (n != 1)
            {
                var name = await _db.Foods.AsNoTracking()
                    .Where(f => f.Id == line.FoodId)
                    .Select(f => f.Name)
                    .FirstOrDefaultAsync(ct);
                return (false, $"Không đủ tồn kho cho món \"{name ?? "?"}\" (hoặc món đã bị khóa).");
            }
        }

        return (true, null);
    }

    public async Task RestoreStockForOrderAsync(int orderId, CancellationToken ct = default)
    {
        var lines = await _db.OrderDetails.AsNoTracking()
            .Where(od => od.OrderId == orderId)
            .Select(od => new { od.FoodId, od.Quantity })
            .ToListAsync(ct);

        foreach (var line in lines)
        {
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE Foods SET StockQuantity = StockQuantity + {line.Quantity} WHERE Id = {line.FoodId}",
                ct);
        }
    }
}
