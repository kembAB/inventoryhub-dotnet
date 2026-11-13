using System.ComponentModel.DataAnnotations;

namespace InventoryHub.Models;

/// <summary>
/// One stocked product in the inventory.
/// Validation attributes are enforced automatically by [ApiController]:
/// an invalid request body returns HTTP 400 before the action runs.
/// </summary>
public class Product
{
    public int Id { get; set; }

    [Required]
    [StringLength(120, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(40, MinimumLength = 1)]
    public string Sku { get; set; } = string.Empty;

    [Required]
    [StringLength(60, MinimumLength = 1)]
    public string Category { get; set; } = string.Empty;

    [Range(0.01, 1_000_000)]
    public decimal Price { get; set; }

    [Range(0, 1_000_000)]
    public int StockQuantity { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
