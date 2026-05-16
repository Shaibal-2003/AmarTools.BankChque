using AmarTools.Bankcheck.Data;
using AmarTools.Bankcheck.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmarTools.Bankcheck.Controllers;

public class ChequeAdminController : Controller
{
    private readonly ChequeDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ChequeAdminController(ChequeDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.TemplateCount = await _db.ChequeTemplates.CountAsync();
        ViewBag.PrintedTotal = await _db.PrintedCheques.CountAsync();
        ViewBag.PrintedToday = await _db.PrintedCheques
            .CountAsync(p => p.PrintedAt.Date == DateTime.UtcNow.Date);

        var recent = await _db.PrintedCheques
            .Include(p => p.ChequeTemplate)
            .OrderByDescending(p => p.PrintedAt)
            .Take(5)
            .ToListAsync();

        return View(recent);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FIX: History action was missing — the view existed but no controller
    //      method matched the route, causing HTTP 404.
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> History(string? search, int page = 1, int pageSize = 20)
    {
        var query = _db.PrintedCheques
            .Include(p => p.ChequeTemplate)
            .AsQueryable();

        // Optional search by payee name or cheque number
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p =>
                p.PayeeName.ToLower().Contains(term) ||
                (p.ChequeNumber != null && p.ChequeNumber.ToLower().Contains(term)));
        }

        int total = await query.CountAsync();

        var records = await query
            .OrderByDescending(p => p.PrintedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Total = total;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.Search = search;

        return View(records);
    }

    public async Task<IActionResult> Templates()
    {
        var list = await _db.ChequeTemplates.OrderBy(t => t.BankName).ToListAsync();
        return View(list);
    }

    public IActionResult CreateTemplate() => View(new ChequeTemplate());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTemplate(ChequeTemplate model, IFormFile? chequeImage, IFormFile? logo)
    {
        if (!ModelState.IsValid) return View(model);

        model.ChequeImagePath = await SaveUpload(chequeImage, "cheques");
        model.LogoPath = await SaveUpload(logo, "logos");
        model.CreatedAt = DateTime.UtcNow;

        _db.ChequeTemplates.Add(model);
        await _db.SaveChangesAsync();

        // Default layouts
        var defaults = new[] { "Payee", "Amount", "AmountWords", "Date", "ChequeNo" };
        float[] xs = { 140, 660, 90, 660, 90 };
        float[] ys = { 185, 185, 155, 240, 55 };
        float[] sizes = { 11f, 11f, 10f, 10f, 9f };

        for (int i = 0; i < defaults.Length; i++)
        {
            _db.ChequeLayouts.Add(new ChequeLayout
            {
                ChequeTemplateId = model.Id,
                FieldName = defaults[i],
                X = xs[i],
                Y = ys[i],
                FontSize = sizes[i]
            });
        }

        await _db.SaveChangesAsync();

        TempData["Success"] = "Template created successfully.";
        return RedirectToAction(nameof(Templates));
    }

    public async Task<IActionResult> EditTemplate(int id)
    {
        var template = await _db.ChequeTemplates
            .Include(t => t.Layouts)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null) return NotFound();

        return View(template);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        var template = await _db.ChequeTemplates
            .Include(t => t.PrintedCheques)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null)
        {
            TempData["Error"] = "Template not found.";
            return RedirectToAction(nameof(Templates));
        }

        try
        {
            _db.ChequeTemplates.Remove(template);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Template '{template.BankName}' deleted successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error deleting template: {ex.Message}";
        }

        return RedirectToAction(nameof(Templates));
    }

    [HttpPost]
    public async Task<IActionResult> SaveLayout([FromBody] ChequeLayout layout)
    {
        if (layout == null || layout.Id <= 0)
            return BadRequest(new { success = false, message = "Invalid data" });

        var existing = await _db.ChequeLayouts.FindAsync(layout.Id);
        if (existing == null)
            return NotFound(new { success = false, message = "Layout not found" });

        existing.X = Math.Max(0, layout.X);
        existing.Y = Math.Max(0, layout.Y);
        existing.FontSize = Math.Max(6f, layout.FontSize);

        await _db.SaveChangesAsync();

        return Ok(new { success = true, message = "Layout position saved successfully." });
    }

    private async Task<string?> SaveUpload(IFormFile? file, string folder)
    {
        if (file == null || file.Length == 0) return null;

        var allowed = new[] { ".jpg", ".jpeg", ".png" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            throw new InvalidOperationException("Only JPG/PNG allowed.");

        var dir = Path.Combine(_env.WebRootPath, "images", folder);
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(dir, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/images/{folder}/{fileName}";
    }
}