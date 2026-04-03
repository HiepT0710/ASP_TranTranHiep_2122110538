namespace TranTranHiep_2122110538.ViewModels;

public class CartItemDto
{
    public int FoodId { get; set; }
    public int Quantity { get; set; }
}

public class CartAddRequest
{
    public int FoodId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class CartUpdateRequest
{
    public int FoodId { get; set; }
    public int Quantity { get; set; }
}

public class CheckoutRequest
{
    public string? Address { get; set; }
    public string? Phone { get; set; }
}
