using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Library_Management_System.Service;
namespace Library_Management_System.Helper
{
    public static class ReportExportHelper
    {
        public static void ExportReportToCSV(string filePath, string reportTitle, List<string> headers, List<List<string>> rows)
        {
            try
            {
                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    writer.WriteLine(reportTitle);
                    writer.WriteLine($"Generated on: {DateTime.Now:MMMM dd, yyyy} at {DateTime.Now:hh:mm tt}");
                    writer.WriteLine();
                    writer.WriteLine(string.Join(",", headers.Select(h => EscapeCSV(h))));
                    foreach (var row in rows)
                    {
                        writer.WriteLine(string.Join(",", row.Select(cell => EscapeCSV(cell ?? ""))));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error exporting to CSV: {ex.Message}", ex);
            }
        }
        public static void ExportReportToHTML(string filePath, string reportTitle, List<string> headers, List<List<string>> rows, string additionalInfo = "")
        {
            try
            {
                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    writer.WriteLine("<!DOCTYPE html>");
                    writer.WriteLine("<html><head>");
                    writer.WriteLine("<meta charset='UTF-8'>");
                    writer.WriteLine($"<title>{reportTitle}</title>");
                    writer.WriteLine("<style>");
                    writer.WriteLine("body { font-family: Arial, sans-serif; margin: 20px; }");
                    writer.WriteLine("h1 { color: #800000; text-align: center; }");
                    writer.WriteLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
                    writer.WriteLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
                    writer.WriteLine("th { background-color: #f2f2f2; font-weight: bold; }");
                    writer.WriteLine("tr:nth-child(even) { background-color: #f9f9f9; }");
                    writer.WriteLine(".header { text-align: center; margin-bottom: 10px; color: #666; }");
                    writer.WriteLine(".info { margin: 10px 0; padding: 10px; background-color: #f0f0f0; }");
                    writer.WriteLine("</style>");
                    writer.WriteLine("</head><body>");
                    writer.WriteLine($"<h1>{reportTitle}</h1>");
                    writer.WriteLine($"<div class='header'>Generated on: {DateTime.Now:MMMM dd, yyyy} at {DateTime.Now:hh:mm tt}</div>");
                    if (!string.IsNullOrEmpty(additionalInfo))
                    {
                        writer.WriteLine($"<div class='info'>{additionalInfo}</div>");
                    }
                    writer.WriteLine("<table>");
                    writer.WriteLine("<tr>");
                    foreach (var header in headers)
                    {
                        writer.WriteLine($"<th>{EscapeHTML(header)}</th>");
                    }
                    writer.WriteLine("</tr>");
                    foreach (var row in rows)
                    {
                        writer.WriteLine("<tr>");
                        foreach (var cell in row)
                        {
                            writer.WriteLine($"<td>{EscapeHTML(cell ?? "")}</td>");
                        }
                        writer.WriteLine("</tr>");
                    }
                    writer.WriteLine("</table>");
                    writer.WriteLine($"<p style='margin-top: 20px; color: #666;'>Total Records: {rows.Count}</p>");
                    writer.WriteLine("</body></html>");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error exporting to HTML: {ex.Message}", ex);
            }
        }
        public static void ExportReportToExcel(string filePath, string reportTitle, List<string> headers, List<List<string>> rows)
        {
            try
            {
                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    writer.WriteLine("<?xml version=\"1.0\"?>");
                    writer.WriteLine("<?mso-application progid=\"Excel.Sheet\"?>");
                    writer.WriteLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
                    writer.WriteLine(" xmlns:o=\"urn:schemas-microsoft-com:office:office\"");
                    writer.WriteLine(" xmlns:x=\"urn:schemas-microsoft-com:office:excel\"");
                    writer.WriteLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\"");
                    writer.WriteLine(" xmlns:html=\"http://www.w3.org/TR/REC-html40\">");
                    writer.WriteLine("<Worksheet ss:Name=\"" + EscapeXML(reportTitle) + "\">");
                    writer.WriteLine("<Table>");
                    writer.WriteLine("<Row>");
                    foreach (var header in headers)
                    {
                        writer.WriteLine($"<Cell><Data ss:Type=\"String\">{EscapeXML(header)}</Data></Cell>");
                    }
                    writer.WriteLine("</Row>");
                    foreach (var row in rows)
                    {
                        writer.WriteLine("<Row>");
                        foreach (var cell in row)
                        {
                            writer.WriteLine($"<Cell><Data ss:Type=\"String\">{EscapeXML(cell ?? "")}</Data></Cell>");
                        }
                        writer.WriteLine("</Row>");
                    }
                    writer.WriteLine("</Table>");
                    writer.WriteLine("</Worksheet>");
                    writer.WriteLine("</Workbook>");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error exporting to Excel: {ex.Message}", ex);
            }
        }
        public static void ExportCirculationReport(ReportsService reportsService, string filePath, string format)
        {
            var headers = new List<string> { "Date", "Borrowings", "Returns" };
            var rows = new List<List<string>>();
            var data = reportsService.GetDailyCirculationData(DateTime.Now.AddDays(-30), DateTime.Now);
            foreach (var point in data)
            {
                rows.Add(new List<string> { point.Date, point.Borrowings.ToString(), point.Returns.ToString() });
            }
            var overdueBooks = reportsService.GetOverdueBooks();
            if (overdueBooks.Count > 0)
            {
                rows.Add(new List<string> { "", "", "" });
                rows.Add(new List<string> { "OVERDUE BOOKS", "", "" });
                rows.Add(new List<string> { "Book Title", "Member", "Days Overdue" });
                foreach (var book in overdueBooks.Take(50))
                {
                    rows.Add(new List<string> { book.BookTitle, book.MemberName, book.DaysOverdue.ToString() });
                }
            }
            ExportReport(filePath, format, "Circulation Report", headers, rows);
        }
        public static void ExportMemberReport(ReportsService reportsService, string filePath, string format)
        {
            var headers = new List<string> { "Member Type", "Count" };
            var rows = new List<List<string>>();
            var distribution = reportsService.GetMemberTypeDistribution();
            foreach (var dist in distribution)
            {
                rows.Add(new List<string> { dist.MemberType, dist.Count.ToString() });
            }
            var summary = reportsService.GetMemberActivitySummary();
            rows.Add(new List<string> { "", "" });
            rows.Add(new List<string> { "Total Members", summary.TotalMembers.ToString() });
            rows.Add(new List<string> { "Active Members", summary.ActiveMembers.ToString() });
            rows.Add(new List<string> { "New This Month", summary.NewMembersThisMonth.ToString() });
            ExportReport(filePath, format, "Member Report", headers, rows);
        }
        public static void ExportCollectionReport(ReportsService reportsService, string filePath, string format)
        {
            var headers = new List<string> { "Category", "Books", "Total Copies", "Available Copies" };
            var rows = new List<List<string>>();
            var byCategory = reportsService.GetCollectionByCategory();
            foreach (var category in byCategory)
            {
                rows.Add(new List<string> { 
                    category.CategoryName, 
                    category.BooksByCategory.ToString(), 
                    category.TotalCopies.ToString(), 
                    category.AvailableCopies.ToString() 
                });
            }
            var stats = reportsService.GetCollectionStatistics();
            rows.Add(new List<string> { "", "", "", "" });
            rows.Add(new List<string> { "SUMMARY", "", "", "" });
            rows.Add(new List<string> { "Total Books", stats.TotalBooks.ToString(), "", "" });
            rows.Add(new List<string> { "Total Copies", "", stats.TotalCopies.ToString(), "" });
            rows.Add(new List<string> { "Available", "", "", stats.AvailableCopies.ToString() });
            ExportReport(filePath, format, "Collection Report", headers, rows);
        }
        public static void ExportFinesReport(ReportsService reportsService, string filePath, string format)
        {
            var headers = new List<string> { "Member Name", "Member Number", "Total Fines", "Fine Count" };
            var rows = new List<List<string>>();
            var fineReport = reportsService.GetFineReportData();
            foreach (var member in fineReport.FinesByMember)
            {
                rows.Add(new List<string> { 
                    member.MemberName, 
                    member.MemberNumber, 
                    $"${member.TotalFines:F2}", 
                    member.FineCount.ToString() 
                });
            }
            rows.Add(new List<string> { "", "", "", "" });
            rows.Add(new List<string> { "SUMMARY", "", "", "" });
            rows.Add(new List<string> { "Total Unpaid", "", $"${fineReport.TotalUnpaidFines:F2}", fineReport.UnpaidFineCount.ToString() });
            rows.Add(new List<string> { "Total Paid", "", $"${fineReport.TotalPaidFines:F2}", fineReport.PaidFineCount.ToString() });
            rows.Add(new List<string> { "Monthly Revenue", "", $"${fineReport.MonthlyRevenue:F2}", "" });
            ExportReport(filePath, format, "Fines Report", headers, rows);
        }
        public static void ExportReportToPDF(string filePath, string reportTitle, List<string> headers, List<List<string>> rows, string additionalInfo = "")
        {
            // Note: For true PDF generation, install a PDF library like:
            // - iTextSharp (NuGet: iTextSharp)
            // - PdfSharp (NuGet: PdfSharp)
            // - QuestPDF (NuGet: QuestPDF)
            //
            // For now, this generates a PDF-friendly HTML that can be printed to PDF
            // or converted using a PDF printer driver
            
            try
            {
                // Generate PDF-friendly HTML with print styles
                string htmlPath = filePath.Replace(".pdf", ".html");
                ExportReportToPDFFriendlyHTML(htmlPath, reportTitle, headers, rows, additionalInfo);
                
                // Note: To convert HTML to PDF, you can:
                // 1. Use a library like wkhtmltopdf
                // 2. Use System.Windows.Forms.PrintDocument to print HTML
                // 3. Install iTextSharp/PdfSharp and implement proper PDF generation
                
                MessageBox.Show(
                    $"PDF-friendly HTML generated at:\n{htmlPath}\n\n" +
                    "You can print this HTML file to PDF using your browser's print function.\n\n" +
                    "For true PDF generation, install a PDF library (iTextSharp, PdfSharp, or QuestPDF).",
                    "PDF Export",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"Error exporting to PDF: {ex.Message}", ex);
            }
        }
        
        private static void ExportReportToPDFFriendlyHTML(string filePath, string reportTitle, List<string> headers, List<List<string>> rows, string additionalInfo = "")
        {
            try
            {
                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    writer.WriteLine("<!DOCTYPE html>");
                    writer.WriteLine("<html><head>");
                    writer.WriteLine("<meta charset='UTF-8'>");
                    writer.WriteLine($"<title>{reportTitle}</title>");
                    writer.WriteLine("<style>");
                    writer.WriteLine("@media print {");
                    writer.WriteLine("  @page { size: A4; margin: 1cm; }");
                    writer.WriteLine("  body { margin: 0; }");
                    writer.WriteLine("}");
                    writer.WriteLine("body { font-family: Arial, sans-serif; margin: 20px; }");
                    writer.WriteLine("h1 { color: #800000; text-align: center; page-break-after: avoid; }");
                    writer.WriteLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; page-break-inside: auto; }");
                    writer.WriteLine("tr { page-break-inside: avoid; page-break-after: auto; }");
                    writer.WriteLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
                    writer.WriteLine("th { background-color: #f2f2f2; font-weight: bold; }");
                    writer.WriteLine("tr:nth-child(even) { background-color: #f9f9f9; }");
                    writer.WriteLine(".header { text-align: center; margin-bottom: 10px; color: #666; }");
                    writer.WriteLine(".info { margin: 10px 0; padding: 10px; background-color: #f0f0f0; }");
                    writer.WriteLine(".footer { margin-top: 20px; color: #666; font-size: 10px; }");
                    writer.WriteLine("</style>");
                    writer.WriteLine("</head><body>");
                    writer.WriteLine($"<h1>{reportTitle}</h1>");
                    writer.WriteLine($"<div class='header'>Generated on: {DateTime.Now:MMMM dd, yyyy} at {DateTime.Now:hh:mm tt}</div>");
                    if (!string.IsNullOrEmpty(additionalInfo))
                    {
                        writer.WriteLine($"<div class='info'>{additionalInfo}</div>");
                    }
                    writer.WriteLine("<table>");
                    writer.WriteLine("<thead>");
                    writer.WriteLine("<tr>");
                    foreach (var header in headers)
                    {
                        writer.WriteLine($"<th>{EscapeHTML(header)}</th>");
                    }
                    writer.WriteLine("</tr>");
                    writer.WriteLine("</thead>");
                    writer.WriteLine("<tbody>");
                    foreach (var row in rows)
                    {
                        writer.WriteLine("<tr>");
                        foreach (var cell in row)
                        {
                            writer.WriteLine($"<td>{EscapeHTML(cell ?? "")}</td>");
                        }
                        writer.WriteLine("</tr>");
                    }
                    writer.WriteLine("</tbody>");
                    writer.WriteLine("</table>");
                    writer.WriteLine($"<div class='footer'>Total Records: {rows.Count}</div>");
                    writer.WriteLine("</body></html>");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error exporting to PDF-friendly HTML: {ex.Message}", ex);
            }
        }
        
        private static void ExportReport(string filePath, string format, string title, List<string> headers, List<List<string>> rows)
        {
            switch (format.ToUpper())
            {
                case "CSV":
                    ExportReportToCSV(filePath, title, headers, rows);
                    break;
                case "HTML":
                    ExportReportToHTML(filePath, title, headers, rows);
                    break;
                case "PDF":
                    ExportReportToPDF(filePath, title, headers, rows);
                    break;
                case "EXCEL":
                case "XLSX":
                    ExportReportToExcel(filePath, title, headers, rows);
                    break;
                default:
                    throw new ArgumentException($"Unsupported format: {format}");
            }
        }
        private static string EscapeCSV(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }
        private static string EscapeHTML(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }
        private static string EscapeXML(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }
}

