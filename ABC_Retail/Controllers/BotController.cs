using ABC_Retail.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;

namespace ABC_Retail.Controllers
{
    public class BotController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public BotController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("AzureSQL");
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string question)
        {
            // Keyword bot logic
            var keywordAnswers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"order","Your order is currently being processed." },
                {"shipping","Your products are currently being shipped."},
                {"return", "Our return policy is related to any goods damaged, the incorrect goods were delivered and if you have changed your mind" },
                {"payment options","The current payment options are: credit/debit cards, EFT via (Ozow or PayFast)  " },
                {"payment", "The current payment options are: credit/debit cards, EFT via (Ozow or PayFast) " }
            };

            string answer = "Sorry, I didn't understand your question. Please try again.";

            foreach (var kvp in keywordAnswers)
            {
                if (question.Contains(kvp.Key, System.StringComparison.OrdinalIgnoreCase))
                {
                    answer = kvp.Value;
                    break;
                }
            }

            // Store Q&A in Azure SQL Database
            await StoreQAPairInDatabase(question, answer);

            ViewBag.Response = answer;
            ViewBag.Question = question;

            return View();
        }

        private async Task StoreQAPairInDatabase(string question, string answer)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Simple insert without category and created at
                    var sql = @"INSERT INTO FAQ (FAQId, Question, Answer) 
                               VALUES (NEWID(), @Question, @Answer)";

                    await connection.ExecuteAsync(sql, new
                    {
                        Question = question,
                        Answer = answer
                    });
                }
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error storing FAQ in database: {ex.Message}");
            }
        }

        // GET: Bot/FAQ - To view all stored FAQs
        public async Task<IActionResult> FAQ()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var sql = "SELECT * FROM FAQ";
                    var faqs = await connection.QueryAsync<QAMessage>(sql);

                    return View(faqs.ToList());
                }
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error retrieving FAQs: {ex.Message}");
                return View(new List<QAMessage>());
            }
        }

        // GET: Bot/AddFAQ - For manually adding FAQs
        public IActionResult AddFAQ()
        {
            return View();
        }

        // POST: Bot/AddFAQ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFAQ(QAMessage faq)
        {
            if (!ModelState.IsValid)
            {
                return View(faq);
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var sql = @"INSERT INTO FAQ (FAQId, Question, Answer) 
                               VALUES (NEWID(), @Question, @Answer)";

                    await connection.ExecuteAsync(sql, new
                    {
                        faq.Question,
                        faq.Answer
                    });

                    TempData["SuccessMessage"] = "FAQ added successfully!";
                    return RedirectToAction("FAQ");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while adding the FAQ. Please try again.");
                return View(faq);
            }
        }
    }
}