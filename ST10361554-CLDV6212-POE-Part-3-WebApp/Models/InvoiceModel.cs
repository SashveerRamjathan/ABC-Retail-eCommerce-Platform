using QuestPDF.Infrastructure;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Models
{
    // Class representing the model for an invoice
    public class InvoiceModel
    {
        public int InvoiceNumber { get; set; } // Unique number identifying the invoice
        public DateTime IssueDate { get; set; } // Date when the invoice was issued

        public Address ShippingAddress { get; set; } // Address where the invoice is to be shipped
        public Address ABCRetailAddress { get; set; } // Address of the ABC Retail company

        public List<InvoiceItem> Items { get; set; } // List of items included in the invoice
        public string Comments { get; set; } // Additional comments or notes for the invoice

        public Image LogoImage { get; set; } // Company logo image to be included in the invoice
    }
}
