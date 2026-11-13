using System.ComponentModel.DataAnnotations;

namespace InventoryHub.Models;

/// <summary>Request body for PATCH /api/products/{id}/stock.</summary>
public class StockUpdateRequest
{
    [Range(0, 1_000_000)]
    public int StockQuantity { get; set; }
}
