using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class CartItem
{
    public int CartItemId { get; set; }
    [Required]
    public int ProductId { get; set; }
    [Required]
    public int CustomerId { get; set; }
    [Required]
    public int Quantity { get; set; }
    [Column(TypeName = "decimal(4,4)")]
    public decimal? DiscountPercent { get; set; }
    public Customer Customer { get; set; }
    public Product Product { get; set; }
        
}