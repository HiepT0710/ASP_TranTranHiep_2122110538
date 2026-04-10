using TranTranHiep_2122110538.ViewModels;

namespace TranTranHiep_2122110538.Services;

public interface IInventoryService
{
    /// <summary>Trừ tồn theo từng dòng giỏ; gọi trong transaction.</summary>
    Task<(bool Ok, string? ErrorMessage)> TryDeductStockForLinesAsync(IReadOnlyList<CartItemDto> lines, CancellationToken ct = default);

    /// <summary>Hoàn tồn khi hủy đơn (theo chi tiết đơn đã lưu).</summary>
    Task RestoreStockForOrderAsync(int orderId, CancellationToken ct = default);
}
