# Bazaar-Style Shopping App — Backend Design and Chat Handoff

## Context for the next chat

- We are moving on from the DigitalWallet learning project to a new project inspired by **Bazaar Technologies' shopping app**.
- The user wants to reproduce its shopping workflows, not build the previously suggested independent-distributor SaaS.
- Initial audience: households and businesses purchasing products from our platform.
- This document is **our proposed design**, not Bazaar's internal architecture or database.
- The user wants to agree on the backend and database design before coding.
- The user is learning C#/.NET. Explain each concept, what the code does, and why it is needed. Work in small steps; do not produce large unexplained implementations.
- No new shopping application has been created. This document is saved in the existing DigitalWallet workspace solely for handoff.
- The architecture and defaults below are proposals, not confirmed decisions. Do not treat them as already implemented or approved.

## 1. Authentication and authorization

Yes, authentication is necessary: customers have private addresses and orders, while staff can modify products, stock, and deliveries. Product browsing can remain public.

Proposed account capabilities:

- Registration and account verification.
- Login and logout.
- Password reset.
- Refresh tokens and revocation.
- Customer and staff permissions.

Use ASP.NET Core Identity for account/password management rather than rebuilding account-security mechanisms. JWTs can authenticate API requests.

| Role | Access |
|---|---|
| Customer | Own profile, addresses, cart, and orders |
| Admin | Products, prices, stock, orders, and staff management |
| DeliveryAgent | Assigned deliveries and the information required to fulfil them |

Public registration always creates a Customer. A user must not be able to choose Admin during registration.

Authentication answers **who the caller is**. Authorization checks **whether that caller may perform this action on this particular record**. A valid login alone must not permit reading another customer's order.

## 2. Backend structure

Start with one ASP.NET Core backend application and one SQL Server database, with related features grouped into modules.

| Project | Responsibility |
|---|---|
| API | HTTP endpoints, authentication/authorization enforcement, responses |
| Application | Business operations such as checkout and cancellation |
| Domain | Entities, enums, and core business rules |
| Infrastructure | Database access, email, storage, external integrations |

For this version, we operate one store/platform. Households and businesses are customers, not automatically tenants with separate catalogues. Do not add TenantId everywhere unless the scope changes to hosting independent stores.

The user's original interest included multi-tenancy, but the selected shopping-app scope does not yet establish a multi-store tenancy requirement. Revisit this explicitly if needed rather than silently changing the product.

## 3. Backend modules

| Module | Responsibilities |
|---|---|
| Authentication | Registration, verification, login, logout, password reset, refresh tokens |
| Authorization | Customer and staff permissions and record ownership |
| Customer profiles | Customer details and optional business name |
| Addresses and service areas | Saved addresses and supported delivery areas |
| Catalogue | Categories, products, images, pack sizes, prices |
| Inventory | Physical stock, reservations, adjustments |
| Cart | Products and quantities the customer intends to buy |
| Checkout and orders | Price/stock validation and order creation |
| Payments | Payment status and cash collection; online payments later |
| Delivery | Assignment, progress, failed attempts |
| Notifications | Order confirmations and status updates |
| Audit and logging | Staff action history and operational diagnostics |

## 4. Proposed database tables

Each table has an Id unless stated otherwise. Foreign keys connect related records. This is a conceptual schema, not finalized EF entities or migrations; exact types, lengths, indexes, and state transitions must be settled during detailed design.

### Accounts

| Table | Important fields | Purpose |
|---|---|---|
| Users | Identity fields, FullName, CustomerType, BusinessName?, IsActive, CreatedAtUtc | Customer and staff accounts |
| Roles / UserRoles | Identity-managed roles and assignments | Permissions |
| RefreshTokens | UserId, TokenHash, ExpiresAtUtc, RevokedAtUtc | Login renewal and revocation |

CustomerType can be Household or Business. Initially it describes the customer, without company teams or credit accounts. Identity may require supporting tables beyond this simplified list.

### Addresses and delivery coverage

| Table | Important fields | Purpose |
|---|---|---|
| ServiceAreas | Name, City, DeliveryFee, IsActive | Areas we deliver to |
| Addresses | UserId, ServiceAreaId, RecipientName, Phone, AddressLine, Landmark?, IsDefault | Saved customer addresses |

The server checks service-area availability during checkout.

### Catalogue

| Table | Important fields | Purpose |
|---|---|---|
| Categories | Name, Slug, IsActive | Product grouping |
| Products | CategoryId, SKU, Name, Description, Brand?, PackSize, Price, IsActive, RowVersion | Purchasable items |
| ProductImages | ProductId, StorageKey, SortOrder | Product photographs |

Each product represents one purchasable SKU. A 1-litre milk carton and a pack of twelve cartons are separate products.

Initially sell fixed packs with whole-number quantities. Loose goods priced by actual delivered weight require additional rules.

### Inventory

| Table | Important fields | Purpose |
|---|---|---|
| Inventory | ProductId (unique), OnHandQuantity, ReservedQuantity, RowVersion | Current stock |
| StockMovements | ProductId, OrderId?, OnHandChange, ReservedChange, Reason, ActorUserId?, CreatedAtUtc | Stock change history |

Start with one stock location.

```text
Available stock = On-hand stock - Reserved stock
```

Example: 100 cartons physically present, with 8 reserved for orders, leaves 92 available to sell.

- At checkout: reserve the ordered quantity.
- At dispatch: reduce both physical and reserved quantities.
- Cancellation before dispatch: release the reservation.
- Returned dispatched goods: restore stock only after staff confirms the quantity received and its saleability.

### Cart

| Table | Important fields | Purpose |
|---|---|---|
| Carts | UserId (unique), UpdatedAtUtc | One current cart per customer |
| CartItems | CartId, ProductId, Quantity | Selected products |

A cart does not reserve stock or guarantee a price. Checkout validates both again.

### Orders

| Table | Important fields | Purpose |
|---|---|---|
| Orders | OrderNumber, UserId, Status, Subtotal, DeliveryFee, Total, Currency, IdempotencyKey, RequestHash, CreatedAtUtc, RowVersion | Purchase record |
| OrderItems | OrderId, ProductId, ProductName, SKU, PackSize, UnitPrice, Quantity, LineTotal | Purchased items at checkout prices |
| OrderStatusHistory | OrderId, FromStatus?, ToStatus, ChangedByUserId?, Reason?, CreatedAtUtc | Order progress history |

Orders also store a snapshot of the delivery address and recipient details.

Product names, pack sizes, prices, and addresses are copied into the order because subsequent edits must not alter historical purchases. Product references remain useful, but order displays and totals use saved order values.

### Payments and delivery

| Table | Important fields | Purpose |
|---|---|---|
| Payments | OrderId, Method, Status, Amount, ProviderReference?, CollectedByUserId?, PaidAtUtc? | Payment records |
| Deliveries | OrderId (unique), AssignedAgentId?, Status, DispatchedAtUtc?, DeliveredAtUtc?, RowVersion | One shipment per order initially |
| DeliveryAttempts | DeliveryId, Outcome, FailureReason?, AttemptedAtUtc | Successful/failed delivery attempts |

Order status and payment status are separate. A dispatched cash-on-delivery order can still be unpaid.

### Operational records

| Table | Important fields | Purpose |
|---|---|---|
| AuditLogs | ActorUserId?, Action, EntityType, EntityId, OldValues?, NewValues?, CreatedAtUtc | Staff change history |
| OutboxMessages | Type, Payload, CreatedAtUtc, ProcessedAtUtc?, Attempts, NextAttemptAtUtc? | Reliable notification queue |

The outbox is a database-backed queue. Save the order and notification request together, then let a background worker send the notification. Email failure must not undo a valid order.

## 5. Main relationships

```text
User
 |-- Addresses
 |-- Cart -- CartItems -- Product -- Category
 |-- Orders
      |-- OrderItems -- Product
      |-- Payments
      |-- OrderStatusHistory
      |-- Delivery -- DeliveryAttempts

Product
 |-- ProductImages
 |-- Inventory
 |-- StockMovements
```

## 6. Data-protection and business rules

- **Server-calculated totals:** clients send product IDs and quantities, not authoritative prices or totals.
- **Atomic checkout:** order creation, inventory reservation, stock history, and notification enqueueing succeed or roll back together.
- **Concurrent stock protection:** simultaneous checkouts must not reserve the same remaining stock.
- **Idempotent checkout:** retrying the same request returns the same order; the same key with different contents is rejected.
- **Ownership checks:** customers access only their own private records.
- **Historical preservation:** deactivate records where appropriate; do not cascade-delete order history.
- **Database constraints:** quantities positive; prices and stock nonnegative; reserved quantity cannot exceed on-hand quantity.
- **Unique values:** SKU, order number, cart/product combination, customer/idempotency-key combination.
- **Consistent types:** decimal money, UTC timestamps, PKR initially.
- **Safe repeated actions:** repeated delivery/cancellation calls must not collect cash or restore stock twice.

## 7. Proposed order lifecycle

```text
Placed -> Confirmed -> Preparing -> Dispatched -> Delivered
```

Exceptions and proposed rules:

- Customers may cancel while Placed.
- Admins may cancel before Dispatched, with a reason.
- Failed delivery creates an attempt record and permits retry/reassignment.
- Returned dispatched goods are not automatically saleable stock; staff confirms receipt and condition first.
- Whole-order fulfilment and cash on delivery are the initial scope.
- Partial shipments, online refunds, and perishable returns need additional design.

The complete allowed transitions for cancellation, returned deliveries, payment collection, and delivery status still need definition before implementing those operations.

## 8. First milestone and execution order

**Milestone:** a customer places a cash-on-delivery order, an admin processes it, and the system records delivery and payment correctly.

1. Confirm shopping rules and schema decisions.
2. Set up the new project and authentication/authorization.
3. Implement customer addresses and delivery coverage.
4. Implement catalogue and inventory.
5. Implement cart and transactional checkout.
6. Implement fulfilment, delivery, and payment collection.
7. Add card payments, wallet, discounts, delivery slots, and reordering in later milestones.

Audit/logging and reliable notification support should accompany the operations that need them.

## 9. Decisions awaiting confirmation

The previous response proposed the following defaults; the user has not yet explicitly confirmed them:

| Decision | Proposed starting point |
|---|---|
| Catalogue ownership | One platform-managed catalogue |
| Inventory | One stock location |
| Product quantities | Fixed packs, whole-number quantities |
| Currency | PKR |
| Initial payment method | Cash on delivery |
| Fulfilment | One delivery per order; whole-order fulfilment |
| Accounts | ASP.NET Core Identity with API authentication |
| Architecture | One layered application with SQL Server |

Also settle before affected features are implemented:

- Email versus phone login and verification channel.
- Staff onboarding and initial administrator creation.
- Exact delivery areas, fees, minimum order, and scheduling rules.
- How long unprocessed orders may reserve stock.
- Cancellation, failed-delivery, and return transitions.
- How cash collection and staff handover are recorded.
- New project name and workspace location.

## 10. Scope and evidence boundaries

- Excluded from the first version: Bazaar Pro procurement workflows, Easy Khata, full Keenu replication, multi-store tenancy, and business lending/credit.
- Bazaar's private database and current internal architecture are unknown.
- Publicly unverified Bazaar rules must be explicit choices for our app, not presented as facts about Bazaar.
- No code or database changes were made for this design.

Useful public references from earlier research:

- [Bazaar official website](https://www.bazaartech.com/)
- [Bazaar shopping app — Google Play](https://play.google.com/store/apps/details?id=bazaar.tech.com)
- [Bazaar shopping app — App Store](https://apps.apple.com/pk/app/bazaar-grocery-delivery/id6741764524)
- [Bazaar Pro](https://www.bazaartech.com/bazaar-pro)
- [Bazaar published terms](https://www.bazaartech.com/terms-conditions)

Public listings conflict on some delivery coverage details. No authenticated shopping journey or live order was tested.
