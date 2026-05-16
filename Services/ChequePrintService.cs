using AmarTools.Bankcheck.Data;
using AmarTools.Bankcheck.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.EntityFrameworkCore;

namespace AmarTools.Bankcheck.Services;

public class ChequePrintRequest
{
    public int TemplateId { get; set; }
    public string PayeeName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ChequeDate { get; set; }
    public string? ChequeNumber { get; set; }
    public string? MemoNote { get; set; }
    public string? PrintedBy { get; set; }
}

public class ChequePrintService
{
    private readonly ChequeDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ChequePrintService(ChequeDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<byte[]> GeneratePdfAsync(ChequePrintRequest request)
    {
        var template = await _db.ChequeTemplates
            .Include(t => t.Layouts)
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.IsActive)
            ?? throw new InvalidOperationException("Template not found or inactive.");

        var amountWords = ConvertToWords(request.Amount);

        var record = new PrintedCheque
        {
            ChequeTemplateId = template.Id,
            PayeeName = request.PayeeName,
            Amount = request.Amount,
            AmountInWords = amountWords,
            ChequeDate = request.ChequeDate,
            ChequeNumber = request.ChequeNumber,
            MemoNote = request.MemoNote,
            PrintedAt = DateTime.UtcNow,
            PrintedBy = request.PrintedBy ?? "System"
        };

        _db.PrintedCheques.Add(record);
        await _db.SaveChangesAsync();

        return BuildPdf(template, request, amountWords);
    }

    private byte[] BuildPdf(ChequeTemplate template, ChequePrintRequest req, string amountWords)
    {
        // FIX: Do NOT use 'using' here — iTextSharp's doc.Close() will dispose
        // the stream, making ms.ToArray() return empty bytes.
        var ms = new MemoryStream();
        var pageSize = new Rectangle(template.PageWidth, template.PageHeight);
        var doc = new Document(pageSize, 0, 0, 0, 0);
        var writer = PdfWriter.GetInstance(doc, ms);

        // FIX: Prevent iTextSharp from closing the underlying MemoryStream
        // when doc.Close() is called, so ms.ToArray() can still read the bytes.
        writer.CloseStream = false;

        doc.Open();

        var cb = writer.DirectContent;

        // Background Image
        if (!string.IsNullOrEmpty(template.ChequeImagePath))
        {
            var relativePath = template.ChequeImagePath
                .TrimStart('/', '\\')
                .Replace('/', Path.DirectorySeparatorChar);
            var imgPath = Path.Combine(_env.WebRootPath, relativePath);
            if (File.Exists(imgPath))
            {
                var bg = Image.GetInstance(imgPath);
                bg.SetAbsolutePosition(0, 0);
                bg.ScaleAbsolute(template.PageWidth, template.PageHeight);
                doc.Add(bg);
            }
        }

        var fieldValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Payee"] = req.PayeeName ?? "",
            ["Amount"] = $"BDT {req.Amount:N2}",
            ["AmountWords"] = amountWords,
            ["Date"] = req.ChequeDate.ToString("dd/MM/yyyy"),
            ["ChequeNo"] = req.ChequeNumber ?? "",
            ["Memo"] = req.MemoNote ?? ""
        };

        var baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.EMBEDDED);

        foreach (var layout in template.Layouts)
        {
            if (!fieldValues.TryGetValue(layout.FieldName, out var value) || string.IsNullOrWhiteSpace(value))
                continue;

            var color = HexToColor(layout.FontColor);

            cb.BeginText();
            cb.SetFontAndSize(baseFont, layout.FontSize);
            cb.SetColorFill(color);

            // PDF coordinate system has origin at bottom-left, so invert Y
            float baselineOffset = layout.FontSize * 0.72f;
            float pdfY = template.PageHeight - layout.Y - baselineOffset;

            cb.SetTextMatrix(layout.X, pdfY);
            cb.ShowText(value.Trim());
            cb.EndText();
        }

        doc.Close();
        return ms.ToArray();
    }

    private static BaseColor HexToColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            int r = Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);
            return new BaseColor(r, g, b);
        }
        return BaseColor.BLACK;
    }

    public static string ConvertToWords(decimal number)
    {
        if (number == 0) return "Zero Taka Only";
        long intPart = (long)Math.Floor(number);
        int decPart = (int)Math.Round((number - intPart) * 100);
        string words = NumberToWords(intPart) + " Taka";
        if (decPart > 0)
            words += " and " + NumberToWords(decPart) + " Paisa";
        words += " Only";
        return words;
    }

    private static readonly string[] Ones = { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
    private static readonly string[] Tens = { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

    private static string NumberToWords(long n)
    {
        if (n == 0) return "Zero";
        if (n < 0) return "Minus " + NumberToWords(-n);
        string result = "";
        if (n >= 1000000000) { result += NumberToWords(n / 1000000000) + " Arab "; n %= 1000000000; }
        if (n >= 10000000) { result += NumberToWords(n / 10000000) + " Crore "; n %= 10000000; }
        if (n >= 100000) { result += NumberToWords(n / 100000) + " Lakh "; n %= 100000; }
        if (n >= 1000) { result += NumberToWords(n / 1000) + " Thousand "; n %= 1000; }
        if (n >= 100) { result += Ones[n / 100] + " Hundred "; n %= 100; }
        if (n >= 20) { result += Tens[n / 10] + " "; n %= 10; }
        if (n > 0) result += Ones[n] + " ";
        return result.Trim();
    }
}