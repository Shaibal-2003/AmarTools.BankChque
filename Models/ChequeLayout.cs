using System.ComponentModel.DataAnnotations;

namespace AmarTools.Bankcheck.Models;

public class ChequeLayout
{
    public int Id { get; set; }

    public int ChequeTemplateId { get; set; }
    public ChequeTemplate ChequeTemplate { get; set; } = null!;

    [Required, MaxLength(50)]
    public string FieldName { get; set; } = string.Empty;

    public float X { get; set; }
    public float Y { get; set; }
    public float FontSize { get; set; } = 10f;

    [MaxLength(20)]
    public string FontStyle { get; set; } = "Normal";

    [MaxLength(30)]
    public string FontColor { get; set; } = "#000000";

    public int MaxLength { get; set; } = 100;
}