namespace ApnaBazaar.Application.Products;

public class ProductResponseDto
{
    public int Id { get; set; }
    public required string SKU { get; set; }
    public required string Name { get; set; }
    public required string PackSize { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}

public class ProductListResponseDto
{
    public required IReadOnlyList<ProductResponseDto> Items { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
