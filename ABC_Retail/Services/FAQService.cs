using ABC_Retail.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ABC_Retail.Services
{
    public class FAQService
    {
        private readonly string _connectionString; // Changed to string, not SqlConnection

        public FAQService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("AzureSQL"); // Store connection string, not create connection
        }

        public async Task<List<QAMessage>> GetFAQAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString)) // Now this will work
                {
                    await connection.OpenAsync();
                    var sql = "SELECT * FROM FAQ";
                    var faqs = await connection.QueryAsync<QAMessage>(sql);
                    return faqs.ToList();
                }
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error retrieving FAQs: {ex.Message}");
                return new List<QAMessage>();
            }
        }

        public async Task AddFAQAsync(QAMessage qa)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"INSERT INTO FAQ (FAQId, Question, Answer) 
                           VALUES (NEWID(), @Question, @Answer)";
                    await connection.ExecuteAsync(sql, qa);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding FAQ: {ex.Message}");
                throw;
            }
        }
    }
}