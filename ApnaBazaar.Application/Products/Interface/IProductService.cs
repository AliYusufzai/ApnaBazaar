namespace ApnaBazaar.Application.Products;

public interface IProductService
{
    Task<ProductListResponseDto> ListAsync(int page, int pageSize, bool includeInactive, CancellationToken ct);
    Task<ProductResponseDto?> GetAsync(int id, bool includeInactive, CancellationToken ct);
    Task<ProductResponseDto> CreateAsync(CreateProductRequestDto request, CancellationToken ct);
    Task<ProductResponseDto?> UpdateAsync(int id, UpdateProductRequestDto request, CancellationToken ct);
    Task<bool> DeactivateAsync(int id, CancellationToken ct);
}
