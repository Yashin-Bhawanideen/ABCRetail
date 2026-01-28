using System.Text.Json;

namespace ABC_Retail.Models
{
    public class QAMessage
    {
        public string Question { get; set; }
        public string Answer { get; set; }
    }

    //for sql DB
    public class FAQ
    {
        public Guid FAQId { get; set; }
        public string Question { get;set; }
        public string Answer { get; set; }
       
    }
    
}
