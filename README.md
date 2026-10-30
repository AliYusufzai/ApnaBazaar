# ApnaBazaar

## Product module

Run the API with `dotnet run --project ApnaBazaar.Api --launch-profile https`.
OpenAPI is available at `https://localhost:7207/openapi/v1.json` in Development.

| Method | Route | Behavior |
|---|---|---|
| GET | /api/products?page=1&pageSize=20 | Active products, ordered by ID; maximum page size 100 |
| GET | /api/products/{id} | Active product or 404; Admin may retrieve inactive products |
| POST | /api/products | Admin creates a product; returns 201 and Location |
| PUT | /api/products/{id} | Admin replaces editable values, including active status |
| DELETE | /api/products/{id} | Admin deactivates; repeated calls return 204 |

Admins can request `includeInactive=true` on the list. Other callers receive 403.
Requests require SKU, name, pack size, price and isActive. Text is trimmed;
blank or oversized values and negative/over-precision prices are rejected.
Duplicate SKUs return 409, including concurrent inserts protected by the database index.
Inactive products retain their SKU.

The controller handles HTTP; Application contains DTOs and the service contract;
Infrastructure implements EF Core access. No generic repository or schema changes were needed.

### Authentication boundary

Public reads work immediately. Writes require an authenticated Admin.
Cookie authentication is registered to return 401/403 rather than redirecting.
There is no login or staff provisioning endpoint yet, so writes cannot be used through
HTTP until the planned Identity module is connected. Do not remove authorization to test writes.
When connecting sign-in, select the final cookie/token approach and implement its CSRF protections.
Concurrent staff edits currently use last-write-wins; row-version conflict handling is deferred.

