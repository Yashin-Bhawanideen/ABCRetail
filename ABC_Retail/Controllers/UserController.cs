using ABC_Retail.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.AspNetCore.Http;

namespace ABC_Retail.Controllers
{
    public class UserController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public UserController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("AzureSQL");
        }

        // GET: User/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: User/Register - KEEP THIS EXACTLY AS IS (since it's working)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string Username, string Email, string Password, string ConfirmPassword, string FirstName, string LastName)
        {
            try
            {
                Console.WriteLine($"Registration attempt: {Username}, {Email}");

                // Basic validation
                if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
                {
                    TempData["ErrorMessage"] = "Please fill in all required fields.";
                    return View();
                }

                if (Password != ConfirmPassword)
                {
                    TempData["ErrorMessage"] = "Passwords do not match.";
                    return View();
                }

                if (Password.Length < 6)
                {
                    TempData["ErrorMessage"] = "Password must be at least 6 characters.";
                    return View();
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    Console.WriteLine("Opening database connection...");
                    await connection.OpenAsync();

                    // Check if user already exists
                    var checkUserSql = "SELECT COUNT(*) FROM Users WHERE Username = @Username OR Email = @Email";
                    var userExists = await connection.ExecuteScalarAsync<int>(checkUserSql, new { Username, Email });
                    
                    Console.WriteLine($"User exists check: {userExists}");

                    if (userExists > 0)
                    {
                        TempData["ErrorMessage"] = "Username or email already exists.";
                        return View();
                    }

                    // Insert new user
                    var insertSql = @"INSERT INTO Users (UserId, Username, Email, PasswordHash, FirstName, LastName, CreatedAt, IsActive)
                                    VALUES (NEWID(), @Username, @Email, @PasswordHash, @FirstName, @LastName, GETDATE(), 1)";
                    
                    var result = await connection.ExecuteAsync(insertSql, new 
                    { 
                        Username, 
                        Email, 
                        PasswordHash = Password,
                        FirstName,
                        LastName
                    });

                    Console.WriteLine($"Insert result: {result} rows affected");

                    TempData["SuccessMessage"] = "Registration successful! Please login.";
                    return RedirectToAction("Login");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"REGISTRATION ERROR: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = $"Registration failed: {ex.Message}";
                return View();
            }
        }

        // GET: User/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: User/Login - ONLY THIS METHOD IS UPDATED
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string Username, string Password)
        {
            try
            {
                Console.WriteLine($"=== LOGIN ATTEMPT ===");
                Console.WriteLine($"Username: {Username}");
                Console.WriteLine($"Password length: {Password?.Length}");

                if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    TempData["ErrorMessage"] = "Please enter username and password.";
                    return View();
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    Console.WriteLine("Database connection opened");

                    // Get user by username
                    var getUserSql = "SELECT * FROM Users WHERE Username = @Username AND IsActive = 1";
                    var user = await connection.QueryFirstOrDefaultAsync<User>(getUserSql, new { Username });

                    Console.WriteLine($"User found: {user != null}");
                    
                    if (user != null)
                    {
                        Console.WriteLine($"Stored PasswordHash: '{user.PasswordHash}'");
                        Console.WriteLine($"Entered Password: '{Password}'");
                        Console.WriteLine($"Passwords match: {Password == user.PasswordHash}");
                    }

                    if (user != null && Password == user.PasswordHash)
                    {
                        Console.WriteLine("LOGIN SUCCESSFUL!");
                        
                        // Update last login
                        var updateSql = "UPDATE Users SET LastLogin = GETDATE() WHERE UserId = @UserId";
                        await connection.ExecuteAsync(updateSql, new { user.UserId });

                        // Store user in session
                        HttpContext.Session.SetString("UserId", user.UserId.ToString());
                        HttpContext.Session.SetString("Username", user.Username);
                        HttpContext.Session.SetString("Email", user.Email);

                        TempData["SuccessMessage"] = $"Welcome back, {user.Username}!";
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        Console.WriteLine("LOGIN FAILED - Invalid credentials");
                        TempData["ErrorMessage"] = "Invalid username or password.";
                        return View();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== LOGIN ERROR ===");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "Login failed. Please try again.";
                return View();
            }
        }

        // POST: User/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        // DEBUG METHOD: Check what's in the database (optional)
        public async Task<IActionResult> DebugUsers()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var users = await connection.QueryAsync<User>("SELECT * FROM Users");
                    
                    var result = "Users in database:\n";
                    foreach (var user in users)
                    {
                        result += $"Username: '{user.Username}', PasswordHash: '{user.PasswordHash}'\n";
                    }
                    
                    return Content(result);
                }
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}");
            }
        }
    }
}