using AmarTools.Bankcheck.Data;
using AmarTools.Bankcheck.Models;
using AmarTools.Bankcheck.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmarTools.Bankcheck.Controllers;

public class ChequeController : Controller
{
    private readonly ChequeDbContext _db;
    private readonly ChequePrintService _printService;

    public ChequeController(ChequeDbContext db, ChequePrintService printService)
    {
        _db = db;
        _printService = printService;
    }

    public async Task<IActionResult> Print()
    {
        var templates = await _db.ChequeTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.BankName)
            .ToListAsync();

        ViewBag.Templates = templates;

        // Pass real statistics for Live Preview section
        ViewBag.PrintedTotal = await _db.PrintedCheques.CountAsync();
        ViewBag.PrintedToday = await _db.PrintedCheques
            .CountAsync(p => p.PrintedAt.Date == DateTime.UtcNow.Date);

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Print(
        int templateId,
        string payeeName,
        decimal amount,
        string chequeDate,
        string? chequeNumber,
        string? memoNote)
    {
        // FIX: DateTime.TryParse returns Kind=Local or Kind=Unspecified.
        // PostgreSQL (Npgsql) strictly requires Kind=Utc for "timestamp with time zone" columns.
        // DateTime.SpecifyKind(..., Utc) corrects the Kind without changing the date/time value.
        if (!DateTime.TryParse(chequeDate, out var parsedDate))
            parsedDate = DateTime.UtcNow;
        else
            parsedDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);

        try
        {
            var req = new ChequePrintRequest
            {
                TemplateId = templateId,
                PayeeName = string.IsNullOrWhiteSpace(payeeName) ? "" : payeeName.Trim(),
                Amount = amount,
                ChequeDate = parsedDate,
                ChequeNumber = chequeNumber?.Trim(),
                MemoNote = memoNote?.Trim(),
                PrintedBy = User.Identity?.Name ?? "System"
            };

            var pdfBytes = await _printService.GeneratePdfAsync(req);

            // Store for Success page (if you use it later)
            TempData["LastPayee"] = req.PayeeName;
            TempData["LastAmount"] = req.Amount.ToString("N2");
            TempData["LastDate"] = req.ChequeDate.ToString("dd/MM/yyyy");

            // Download PDF directly
            return File(pdfBytes, "application/pdf",
                $"Cheque_{req.PayeeName.Replace(" ", "_")}_{req.ChequeDate:yyyyMMdd}.pdf");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error generating cheque: {ex.Message}");

            // Reload templates and stats on error
            var templates = await _db.ChequeTemplates
                .Where(t => t.IsActive)
                .OrderBy(t => t.BankName)
                .ToListAsync();

            ViewBag.Templates = templates;
            ViewBag.PrintedTotal = await _db.PrintedCheques.CountAsync();
            ViewBag.PrintedToday = await _db.PrintedCheques
                .CountAsync(p => p.PrintedAt.Date == DateTime.UtcNow.Date);

            return View();
        }
    }

    public IActionResult Success()
    {
        ViewBag.Payee = TempData["LastPayee"] ?? "N/A";
        ViewBag.Amount = TempData["LastAmount"] ?? "0.00";
        ViewBag.Date = TempData["LastDate"] ?? DateTime.UtcNow.ToString("dd/MM/yyyy");
        return View();
    }

    [HttpGet]
    public IActionResult AmountWords(decimal amount)
    {
        var words = ChequePrintService.ConvertToWords(Math.Max(0, amount));
        return Json(new { words });
    }
}