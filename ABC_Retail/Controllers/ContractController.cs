using ABC_Retail.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.AspNetCore.Http;

namespace ABC_Retail.Controllers
{
    public class ContractController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public ContractController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("AzureSQL");
        }

        public async Task<ActionResult> Index()
        {
            var contracts = await GetContractsFromDatabase();
            return View(contracts);
        }

        [HttpPost]
        public async Task<ActionResult> Upload(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    TempData["ErrorMessage"] = "Please select a file to upload.";
                    return RedirectToAction("Index");
                }

                // Validate file size (e.g., 10MB max)
                if (file.Length > 10 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "File size cannot exceed 10MB.";
                    return RedirectToAction("Index");
                }

                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "Please login to upload documents.";
                    return RedirectToAction("Login", "User");
                }

                // Upload file to database
                await UploadFileToDatabase(file, Guid.Parse(userId));
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = $"Error uploading document: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Download(Guid contractId)
        {
            try
            {
                var contract = await GetContractFromDatabase(contractId);
                if (contract?.DocumentData != null)
                {
                    return File(contract.DocumentData, contract.DocumentMimeType, contract.DocumentName);
                }
                
                TempData["ErrorMessage"] = "File not found.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Download error: {ex.Message}");
                TempData["ErrorMessage"] = "Error downloading file.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid contractId)
        {
            try
            {
                await DeleteContractFromDatabase(contractId);
                TempData["SuccessMessage"] = "Document deleted successfully.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Delete error: {ex.Message}");
                TempData["ErrorMessage"] = "Failed to delete document.";
            }
            return RedirectToAction("Index");
        }

        private async Task<List<FileModel>> GetContractsFromDatabase()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return new List<FileModel>();
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var sql = @"SELECT ContractId, DocumentName, FileSize, UploadedAt 
                               FROM Contracts 
                               WHERE UserId = @UserId 
                               ORDER BY UploadedAt DESC";

                    var contracts = await connection.QueryAsync<FileModel>(sql, new { UserId = Guid.Parse(userId) });
                    return contracts.ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting contracts: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return new List<FileModel>();
            }
        }

        private async Task UploadFileToDatabase(IFormFile file, Guid userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                var fileData = memoryStream.ToArray();

                var sql = @"INSERT INTO Contracts (ContractId, UserId, DocumentName, DocumentData, DocumentMimeType, FileSize, UploadedAt)
                           VALUES (NEWID(), @UserId, @DocumentName, @DocumentData, @DocumentMimeType, @FileSize, GETDATE())";

                await connection.ExecuteAsync(sql, new
                {
                    UserId = userId,
                    DocumentName = file.FileName,
                    DocumentData = fileData,
                    DocumentMimeType = file.ContentType,
                    FileSize = file.Length
                });
            }
        }

        private async Task<FileModel> GetContractFromDatabase(Guid contractId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"SELECT ContractId, DocumentName, DocumentData, DocumentMimeType, FileSize, UploadedAt
                           FROM Contracts 
                           WHERE ContractId = @ContractId";

                var contract = await connection.QueryFirstOrDefaultAsync<FileModel>(sql, new { ContractId = contractId });
                return contract;
            }
        }

        private async Task DeleteContractFromDatabase(Guid contractId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "DELETE FROM Contracts WHERE ContractId = @ContractId";
                await connection.ExecuteAsync(sql, new { ContractId = contractId });
            }
        }
    }
}