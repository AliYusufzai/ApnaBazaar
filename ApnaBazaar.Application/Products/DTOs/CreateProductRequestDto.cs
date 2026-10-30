using System.ComponentModel.DataAnnotations;

namespace ApnaBazaar.Application.Products;

public class CreateProductRequestDto
{
    [Required, MaxLength(20)]
    public required string SKU { get; set; }

    [Required, MaxLength(60)]
    public required string Name { get; set; }

    [Required, MaxLength(120)]
    public required string PackSize { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public required decimal Price { get; set; }

    public required bool IsActive { get; set; }
}
