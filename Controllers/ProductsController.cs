using InventoryHub.Data;
using InventoryHub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryHub.Controllers;

/// <summary>
/// Full CRUD REST API for products under /api/products.
/// [ApiController] gives automatic model validation: invalid bodies
/// (missing name, negative price, ...) are rejected with HTTP 400.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProductsController(AppDbContext db) => _db = db;

    // GET /api/products
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll()
        => await _db.Products.OrderBy(p => p.Id).ToListAsync();

    // GET /api/products/3
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Product>> GetById(int id)
    {
        var product = await _db.Products.FindAsync(id);
        return product is null ? NotFound() : product;
    }

    // GET /api/products/low-stock?threshold=5
    // Products whose stock is below the threshold (default 5).
    [HttpGet("low-stock")]
    public async Task<ActionResult<IEnumerable<Product>>> GetLowStock([FromQuery] int threshold = 5)
        => await _db.Products
            .Where(p => p.StockQuantity < threshold)
            .OrderBy(p => p.StockQuantity)
            .ToListAsync();

    // POST /api/products
    [HttpPost]
    public async Task<ActionResult<Product>> Create(Product product)
    {
        if (await _db.Products.AnyAsync(p => p.Sku == product.Sku))
        {
            return Conflict(new { message = $"A product with SKU '{product.Sku}' already exists." });
        }

        product.Id = 0; // the database generates the id
        product.CreatedAt = DateTime.UtcNow;
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    // PUT /api/products/3  (full update)
    [HttpPut("{id:int}")]
    public async Task<ActionResult<Product>> Update(int id, Product input)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return NotFound();

        product.Name = input.Name;
        product.Sku = input.Sku;
        product.Category = input.Category;
        product.Price = input.Price;
        product.StockQuantity = input.StockQuantity;
        await _db.SaveChangesAsync();

        return product;
    }

    // PATCH /api/products/3/stock  { "stockQuantity": 25 }
    [HttpPatch("{id:int}/stock")]
    public async Task<ActionResult<Product>> UpdateStock(int id, StockUpdateRequest request)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return NotFound();

        product.StockQuantity = request.StockQuantity;
        await _db.SaveChangesAsync();

        return product;
    }

    // DELETE /api/products/3
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return NotFound();

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
