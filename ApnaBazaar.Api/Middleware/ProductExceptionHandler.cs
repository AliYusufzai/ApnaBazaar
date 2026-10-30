using ApnaBazaar.Application.Products;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ApnaBazaar.Api.Middleware;

public sealed class ProductExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is not DuplicateSkuException) return false;
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = 409, Title = "A product with this SKU already exists."
        }, ct);
        return true;
    }
}
