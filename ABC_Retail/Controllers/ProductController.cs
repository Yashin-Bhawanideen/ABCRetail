using ABC_Retail.Models;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using static System.Net.WebRequestMethods;

namespace ABC_Retail.Controllers
{
    public class ProductController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public ProductController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("AzureSQL");
        }

        public async Task<IActionResult> Index()
        {
            var products = await GetProductsFromDatabase();
            return View(products);
        }

        public IActionResult AddToCart(Guid id)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Please login to add items to cart.";
                return RedirectToAction("Login", "User");
            }

            return RedirectToAction("Add", "Cart", new { id = id });
        }

        private async Task<List<ProductModel>> GetProductsFromDatabase()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    // Get products from Azure SQL Database - only ImageUrl
                    var sql = @"SELECT ProductId as Id, Name, Price, ImageUrl FROM Products";
                    var products = await connection.QueryAsync<ProductModel>(sql);
                    
                    return products.ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting products from database: {ex.Message}");
                return await GetLocalProducts();
            }
        }

        // GET: Product/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductModel product)
        {
            if (!ModelState.IsValid)
            {
                return View(product);
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    // Insert product into database - simplified without image data
                    var sql = @"INSERT INTO Products (ProductId, Name, Price, ImageUrl)
                               VALUES (NEWID(), @Name, @Price, @ImageUrl)";

                    await connection.ExecuteAsync(sql, new
                    {
                        product.Name,
                        product.Price,
                        product.ImageUrl
                    });

                    TempData["SuccessMessage"] = "Product added successfully!";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while adding the product. Please try again.");
                return View(product);
            }
        }

        // Local fallback product list
        private async Task<List<ProductModel>> GetLocalProducts()
        {
            return new List<ProductModel>()
            {
                new ProductModel { 
                    Id = Guid.NewGuid(), 
                    Name = "Blue Hoodie (M)", 
                    Price = 350, 
                    ImageUrl = "https://media.istockphoto.com/id/821818600/photo/blank-blue-sweatshirt-mockup.jpg?s=612x612&w=0&k=20&c=qKB6LB2naZHVWIua-bwzpvr78uVkQamROWtFxmsyMnQ="
                },
                new ProductModel { 
                    Id = Guid.NewGuid(), 
                    Name = "Green Sweater (W)", 
                    Price = 160, 
                    ImageUrl = "https://media.istockphoto.com/id/900848884/photo/classic-dark-green-woman-sweater-isolated-on-white.jpg?s=612x612&w=0&k=20&c=Ke8eLSfnqTFUF0MXGkfBIdsQGBeanDxuXSW2EtbHJgo="
                },
                new ProductModel { 
                    Id = Guid.NewGuid(), 
                    Name = "Flower Dress (W)", 
                    Price = 400, 
                    ImageUrl = "https://media.istockphoto.com/id/178851955/photo/flowery-evase-bateau-yellow-dress.jpg?s=612x612&w=0&k=20&c=EOJGCGC6dmFt0IQvbxq3PthCmNXO1flOpjYWC4KkcyQ="
                },
                new ProductModel { 
                    Id = Guid.NewGuid(), 
                    Name = "Trench Coat (M)", 
                    Price = 1000, 
                    ImageUrl = "https://media.istockphoto.com/id/1309835079/photo/blank-blazer-mockup-front-view.jpg?s=612x612&w=0&k=20&c=w2hqqWk4t5k-X3_nJGH6yQHcST3tHjJXMLi3Qvc89D8="
                },
                new ProductModel { 
                    Id = Guid.NewGuid(), 
                    Name = "Black Heels (W)", 
                    Price = 950, 
                    ImageUrl = "https://media.istockphoto.com/id/1002026996/photo/black-high-heel-female-shoes.jpg?s=612x612&w=0&k=20&c=IF6XWbW8wnYSrH3P1swqM36L2Yofab6Jrqo8xDk5Kxs="
                },
                new ProductModel { 
                    Id = Guid.NewGuid(), 
                    Name = "Red Jordans (M)", 
                    Price = 850, 
                    ImageUrl = "https://media.istockphoto.com/id/1193158365/photo/sneakers-shoe-isolated.jpg?s=612x612&w=0&k=20&c=uk3SnV-VX06FPUjv9RtZEACRb0rWAxc7EXXfozwqGng="
                }
            };
        }
    }
}