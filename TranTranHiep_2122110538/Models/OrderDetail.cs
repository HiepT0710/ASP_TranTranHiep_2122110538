using System.ComponentModel.DataAnnotations.Schema;

namespace TranTranHiep_2122110538.Models;

public class OrderDetail
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int FoodId { get; set; }
    public Food? Food { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
}
