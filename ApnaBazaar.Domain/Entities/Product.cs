namespace ApnaBazaar.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public required string SKU { get; set; }
    public required string Name { get; set; }
    public required string PackSize { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
}
