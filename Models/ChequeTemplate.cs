using System.ComponentModel.DataAnnotations;

namespace AmarTools.Bankcheck.Models;

public class ChequeTemplate
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string BankName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string AccountNumber { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? LogoPath { get; set; }

    [MaxLength(200)]
    public string? ChequeImagePath { get; set; }

    public float PageWidth { get; set; } = 842f;
    public float PageHeight { get; set; } = 300f;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ChequeLayout> Layouts { get; set; } = new List<ChequeLayout>();
    public ICollection<PrintedCheque> PrintedCheques { get; set; } = new List<PrintedCheque>();
}