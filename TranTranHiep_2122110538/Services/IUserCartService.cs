using Microsoft.AspNetCore.Http;
using TranTranHiep_2122110538.ViewModels;

namespace TranTranHiep_2122110538.Services;

public interface IUserCartService
{
    /// <summary>Giỏ hiệu lực: DB nếu đã đăng nhập, không thì session.</summary>
    Task<List<CartItemDto>> GetCartLinesAsync(HttpContext http);

    Task MergeSessionIntoDatabaseAsync(HttpContext http, int userId);

    Task ClearDatabaseCartAsync(int userId);
}
