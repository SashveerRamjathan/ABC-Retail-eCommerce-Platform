using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Models.QuestPDF
{
    // Class representing an invoice document for PDF generation using QuestPDF
    public class InvoiceDocument : IDocument
    {
        public InvoiceModel Model { get; }

        // Constructor to initialize the invoice model
        public InvoiceDocument(InvoiceModel model)
        {
            Model = model;
        }

        // Method to compose the document structure
        public void Compose(IDocumentContainer container)
        {
            container
                .Page(page =>
                {
                    page.Margin(50); // Set page margins

                    page.Header().Element(ComposeHeader); // Compose the header
                    page.Content().Element(ComposeContent); // Compose the content

                    // Compose the footer with page numbers
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });
        }

        // Method to get document metadata
        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        // Method to compose the header section
        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column
                        .Item().Text($"Invoice #{Model.InvoiceNumber}")
                        .FontSize(20).SemiBold().FontColor(Colors.Orange.Medium); // Invoice number

                    column.Item().Text(text =>
                    {
                        text.Span("Issue date: ").SemiBold();
                        text.Span($"{Model.IssueDate:d}"); // Issue date
                    });

                    column.Item().Text(text =>
                    {
                        text.Span("Issue time: ").SemiBold();
                        text.Span($"{Model.IssueDate:t}"); // Issue time
                    });

                });

                row.ConstantItem(135).Image(Model.LogoImage); // Company logo
            });
        }

        // Method to compose the table of invoice items
        private void ComposeTable(IContainer container)
        {
            var headerStyle = TextStyle.Default.SemiBold(); // Header text style

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(25); // Index column
                    columns.RelativeColumn(3); // Product name column
                    columns.RelativeColumn(); // Unit price column
                    columns.RelativeColumn(); // Quantity column
                    columns.RelativeColumn(); // Subtotal column
                });

                table.Header(header =>
                {
                    header.Cell().Text("#");
                    header.Cell().Text("Product").Style(headerStyle);
                    header.Cell().AlignRight().Text("Unit price").Style(headerStyle);
                    header.Cell().AlignRight().Text("Quantity").Style(headerStyle);
                    header.Cell().AlignRight().Text("Subtotal").Style(headerStyle);

                    header.Cell().ColumnSpan(5).PaddingTop(5).BorderBottom(1).BorderColor(Colors.Black); // Header bottom border
                });

                // Add each item to the table
                foreach (var item in Model.Items)
                {
                    var index = Model.Items.IndexOf(item) + 1;

                    table.Cell().Element(CellStyle).Text($"{index}");
                    table.Cell().Element(CellStyle).Text(item.ProductName);

                    string formattedPrice = item.Price.ToString("C", new CultureInfo("en-ZA"));

                    table.Cell().Element(CellStyle).AlignRight().Text($"{formattedPrice}");
                    table.Cell().Element(CellStyle).AlignRight().Text($"{item.Quantity}");

                    decimal total = item.Price * item.Quantity;
                    string formattedTotal = total.ToString("C", new CultureInfo("en-ZA"));

                    table.Cell().Element(CellStyle).AlignRight().Text($"{formattedTotal}");

                    // Style for each cell
                    static IContainer CellStyle(IContainer container) => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                }
            });
        }

        // Method to compose the comments section
        private void ComposeComments(IContainer container)
        {
            container.ShowEntire().Background(Colors.Grey.Lighten3).Padding(10).Column(column =>
            {
                column.Spacing(5);
                column.Item().Text("Comments").FontSize(14).SemiBold(); // Comments title
                column.Item().Text(Model.Comments); // Comments content
            });
        }

        // Method to compose the main content of the invoice
        private void ComposeContent(IContainer container)
        {
            container.PaddingVertical(40).Column(column =>
            {
                column.Spacing(20);

                // Add address components for sender and recipient
                column.Item().Row(row =>
                {
                    row.RelativeItem().Component(new AddressComponent("From", Model.ABCRetailAddress));
                    row.ConstantItem(50);
                    row.RelativeItem().Component(new AddressComponent("For", Model.ShippingAddress));
                });

                column.Item().Element(ComposeTable); // Add the table of items

                var totalCost = Model.Items.Sum(x => x.Price * x.Quantity); // Calculate total cost

                var formattedTotal = totalCost.ToString("C", new CultureInfo("en-ZA"));

                column.Item().PaddingRight(5).AlignRight().Text($"Total: {formattedTotal}").SemiBold(); // Display total cost

                // Add comments section if there are any comments
                if (!string.IsNullOrWhiteSpace(Model.Comments))
                {
                    column.Item().PaddingTop(25).Element(ComposeComments);
                }
            });
        }
    }
}
