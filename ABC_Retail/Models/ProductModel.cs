
namespace ABC_Retail.Models
{

   
    public class ProductModel
    {
        public Guid Id { get; set; }
        
       
        public string Name { get; set; }
       
        public double Price { get;set; }


       //for azure storage sql DB
       
        public string ImageUrl { get; set; }
      
        public string Description {  get; set; }

       
    }
}
/*
 References
Shaukat, A., 2016. Developing Models In ASP.NET MVC. [Online] 
Available at: https://www.c-sharpcorner.com/article/developing-models-in-Asp-Net-mvc2/
TutTeacher, 2025. Model in ASP.NET MVC. [Online] 
Available at: https://www.tutorialsteacher.com/mvc/mvc-model



 */