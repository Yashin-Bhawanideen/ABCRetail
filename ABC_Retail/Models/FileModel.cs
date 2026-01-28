namespace ABC_Retail.Models
{

   
    public class FileModel
    {
         public Guid ContractId { get; set; }
    public Guid UserId { get; set; }
    public string DocumentName { get; set; }
    public byte[] DocumentData { get; set; }
    public string DocumentMimeType { get; set; }
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    
    // Helper property for display
    public string FileSizeDisplay => FileSize > 1024 * 1024 
        ? $"{(FileSize / (1024.0 * 1024.0)):0.00} MB" 
        : $"{(FileSize / 1024.0):0.00} KB";
    }
}
