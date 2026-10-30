using ApnaBazaar.Application.Products;
using ApnaBazaar.Domain.Entities;
using ApnaBazaar.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ApnaBazaar.Infrastructure.Products;

public sealed class ProductService(AppDbContext db) : IProductService
{
    public async Task<ProductListResponseDto> ListAsync(int page, int pageSize, bool includeInactive, CancellationToken ct)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            throw new ValidationException("Invalid pagination.");
        var items = await db.Products.AsNoTracking()
            .Where(p => includeInactive || p.IsActive)
            .OrderBy(p => p.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new ProductResponseDto { Id = p.Id, SKU = p.SKU, Name = p.Name, PackSize = p.PackSize, Price = p.Price, IsActive = p.IsActive })
            .ToListAsync(ct);
        return new ProductListResponseDto { Items = items, Page = page, PageSize = pageSize };
    }

    public Task<ProductResponseDto?> GetAsync(int id, bool includeInactive, CancellationToken ct) =>
        db.Products.AsNoTracking().Where(p => p.Id == id && (includeInactive || p.IsActive))
            .Select(p => new ProductResponseDto { Id = p.Id, SKU = p.SKU, Name = p.Name, PackSize = p.PackSize, Price = p.Price, IsActive = p.IsActive })
            .SingleOrDefaultAsync(ct);

    public async Task<ProductResponseDto> CreateAsync(CreateProductRequestDto request, CancellationToken ct)
    {
        var product = new Product { SKU = request.SKU.Trim(), Name = request.Name.Trim(),
            PackSize = request.PackSize.Trim(), Price = request.Price, IsActive = request.IsActive };
        db.Products.Add(product);
        await SaveAsync(ct);
        return Map(product);
    }

    public async Task<ProductResponseDto?> UpdateAsync(int id, UpdateProductRequestDto request, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == id, ct);
        if (product is null) return null;
        product.SKU = request.SKU.Trim();
        product.Name = request.Name.Trim();
        product.PackSize = request.PackSize.Trim();
        product.Price = request.Price;
        product.IsActive = request.IsActive;
        await SaveAsync(ct);
        return Map(product);
    }

    public async Task<bool> DeactivateAsync(int id, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == id, ct);
        if (product is null) return false;
        product.IsActive = false;
        await SaveAsync(ct);
        return true;
    }

    private async Task SaveAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // The database index also protects simultaneous requests for the same SKU.
            throw new DuplicateSkuException();
        }
    }

    private static ProductResponseDto Map(Product p) => new ProductResponseDto { Id = p.Id, SKU = p.SKU, Name = p.Name, PackSize = p.PackSize, Price = p.Price, IsActive = p.IsActive };
}
