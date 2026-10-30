using ApnaBazaar.Application.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ApnaBazaar.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(IProductService products) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProductListResponseDto>> List(CancellationToken ct,
        [Range(1, int.MaxValue)] int page = 1, [Range(1, 100)] int pageSize = 20,
        bool includeInactive = false)
    {
        if (includeInactive && !User.IsInRole("Admin")) return Forbid();
        if ((long)(page - 1) * pageSize > int.MaxValue)
            return Problem(statusCode: 400, title: "Page is too large.");
        return Ok(await products.ListAsync(page, pageSize, includeInactive, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductResponseDto>> GetById(int id, CancellationToken ct)
    {
        var product = await products.GetAsync(id, User.IsInRole("Admin"), ct);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductResponseDto>> Create(CreateProductRequestDto request, CancellationToken ct)
    {
        if (decimal.Round(request.Price, 2) != request.Price)
        {
            ModelState.AddModelError(nameof(request.Price), "Price must have at most two decimal places.");
            return ValidationProblem(ModelState);
        }

        var product = await products.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductResponseDto>> Update(int id, UpdateProductRequestDto request, CancellationToken ct)
    {
        if (decimal.Round(request.Price, 2) != request.Price)
        {
            ModelState.AddModelError(nameof(request.Price), "Price must have at most two decimal places.");
            return ValidationProblem(ModelState);
        }

        var product = await products.UpdateAsync(id, request, ct);
        if (product is null)
            return NotFound();

        return Ok(product);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        await products.DeactivateAsync(id, ct) ? NoContent() : NotFound();
}
