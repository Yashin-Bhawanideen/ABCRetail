using ABC_Retail.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.AspNetCore.Http;

namespace ABC_Retail.Controllers
{
    public class CartController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public CartController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("AzureSQL");
        }

        public async Task<IActionResult> Index()
        {
            var cartItems = await GetCartItemsFromDatabase();
            return View(cartItems);
        }

        public async Task<IActionResult> Add(Guid id)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Please login to add items to cart.";
                return RedirectToAction("Login", "User");
            }

            try
            {
                await AddToCartInDatabase(Guid.Parse(userId), id);
                TempData["SuccessMessage"] = "Product added to cart successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to add product to cart: {ex.Message}";
                Console.WriteLine($"Add to cart error: {ex.Message}");
            }

            return RedirectToAction("Index", "Product");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(Guid cartItemId)
        {
            try
            {
                await RemoveFromCartInDatabase(cartItemId);
                TempData["SuccessMessage"] = "Item removed from cart successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to remove item from cart.";
                Console.WriteLine($"Remove from cart error: {ex.Message}");
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (!string.IsNullOrEmpty(userId))
            {
                try
                {
                    await ClearCartInDatabase(Guid.Parse(userId));
                    TempData["SuccessMessage"] = "Cart cleared successfully.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Failed to clear cart.";
                    Console.WriteLine($"Clear cart error: {ex.Message}");
                }
            }
            return RedirectToAction("Index");
        }

        private async Task<List<CartItem>> GetCartItemsFromDatabase()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return new List<CartItem>();
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var sql = @"
                        SELECT 
                            c.CartId,
                            c.Quantity,
                            c.AddedAt,
                            p.ProductId,
                            p.Name,
                            p.Price
                        FROM ShoppingCart c
                        INNER JOIN Products p ON c.ProductId = p.ProductId
                        WHERE c.UserId = @UserId
                        ORDER BY c.AddedAt DESC";

                    var cartItems = await connection.QueryAsync<CartItem>(sql, new { UserId = Guid.Parse(userId) });
                    return cartItems.ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting cart items: {ex.Message}");
                return new List<CartItem>();
            }
        }

        private async Task AddToCartInDatabase(Guid userId, Guid productId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Check if product exists in Products table
                var productExists = await connection.ExecuteScalarAsync<bool>(
                    "SELECT COUNT(1) FROM Products WHERE ProductId = @ProductId", 
                    new { ProductId = productId });

                if (!productExists)
                {
                    throw new Exception($"Product with ID {productId} not found");
                }

                // Check if item already exists in cart
                var existingItem = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT CartId FROM ShoppingCart WHERE UserId = @UserId AND ProductId = @ProductId",
                    new { UserId = userId, ProductId = productId });

                if (existingItem != null)
                {
                    // Update quantity
                    await connection.ExecuteAsync(
                        "UPDATE ShoppingCart SET Quantity = Quantity + 1 WHERE CartId = @CartId",
                        new { CartId = existingItem.CartId });
                }
                else
                {
                    // Add new item
                    await connection.ExecuteAsync(
                        @"INSERT INTO ShoppingCart (CartId, UserId, ProductId, Quantity, AddedAt)
                          VALUES (NEWID(), @UserId, @ProductId, 1, GETDATE())",
                        new { UserId = userId, ProductId = productId });
                }
            }
        }

        private async Task RemoveFromCartInDatabase(Guid cartItemId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.ExecuteAsync(
                    "DELETE FROM ShoppingCart WHERE CartId = @CartId",
                    new { CartId = cartItemId });
            }
        }

        private async Task ClearCartInDatabase(Guid userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.ExecuteAsync(
                    "DELETE FROM ShoppingCart WHERE UserId = @UserId",
                    new { UserId = userId });
            }
        }
    }
}