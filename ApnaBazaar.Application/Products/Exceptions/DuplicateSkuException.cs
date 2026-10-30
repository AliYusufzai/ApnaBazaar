namespace ApnaBazaar.Application.Products;

public sealed class DuplicateSkuException : Exception
{
    public DuplicateSkuException() : base("A product with this SKU already exists.") { }
}
