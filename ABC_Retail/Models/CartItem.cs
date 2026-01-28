

namespace ABC_Retail.Models
{
    public class CartItem
    {
        public Guid CartId { get; set; }
        public Guid UserId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime AddedAt { get; set; }

        // Product details
        public string Name { get; set; }
        public decimal Price { get; set; }
        //public string ImageUrl { get; set; }

        // Calculated property
        public decimal TotalPrice => Price * Quantity;
    }
}
