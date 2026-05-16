using System.ComponentModel.DataAnnotations;

namespace AmarTools.Bankcheck.Models;

public class PrintedCheque
{
    public int Id { get; set; }

    public int ChequeTemplateId { get; set; }

    // Made nullable to avoid nullability warnings
    public ChequeTemplate? ChequeTemplate { get; set; }

    [Required, MaxLength(200)]
    public string PayeeName { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string AmountInWords { get; set; } = string.Empty;

    public DateTime ChequeDate { get; set; }

    [MaxLength(100)]
    public string? ChequeNumber { get; set; }

    [MaxLength(100)]
    public string? MemoNote { get; set; }

    public DateTime PrintedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? PrintedBy { get; set; }
}