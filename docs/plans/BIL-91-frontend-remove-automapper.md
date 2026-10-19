# Plan: remove AutoMapper from the front end

Jira: [BIL-91](https://aldisadom.atlassian.net/browse/BIL-91). Epic: BIL-89 (Billeris). Repo: `BillerFrontEnd`.

## Why

AutoMapper (or any other mapping library) is not allowed in any Biller repo. Biller already maps
with hand-written extension methods. BillerFrontEnd is the only repo that still uses AutoMapper.
BillerContracts and BillioIntegrationTest don't use it.

## Current state (BillerFrontEnd master @ 44f9223)

| Where | What |
|---|---|
| `src/FrontEnd/FrontEnd.csproj`, `src/Domain/Domain.csproj` | `AutoMapper` 13.0.1. No code in `src/Domain` uses it |
| `src/FrontEnd/MappingProfiles/` | 5 profiles with 6 maps, all response → update request, `MemberList.Destination` |
| `src/FrontEnd/Capabilities/StartupMapper.cs`, `Program.cs:19` | `ConfigureAutoMapper()`, which runs `AssertConfigurationIsValid()` at startup |
| Pages | `_mapper.Map<…>` in `Customers_update.razor:90`, `Invoices_update.razor:164,179,231`, `Items_update.razor:62`, `Sellers_update.razor:87`, `Users_update.razor:66`. `Invoices_id.razor:15` injects `IMapper` but never uses it |

Every update-request property has a response property with the same name, so each map is a plain copy:

| Map | Copied | Not copied (response only) |
|---|---|---|
| `CustomerResponse` → `CustomerUpdateRequest` | Id, InvoiceName, InvoiceNumber, CompanyName, CompanyNumber, Street, City, State, Email, Phone | SellerId |
| `ItemResponse` → `ItemUpdateRequest` | Id, Name, Price, Quantity | CustomerId |
| `SellerResponse` → `SellerUpdateRequest` | Id, CompanyNumber, CompanyName, Street, City, State, Email, Phone, BankName, BankNumber | UserId |
| `UserResponse` → `UserUpdateRequest` | Id, Name, LastName | Email |
| `InvoiceItemResponse` → `InvoiceItemUpdateRequest` | Id, Quantity, Name, Price, Comments | |
| `InvoiceResponse` → `InvoiceUpdateRequest` | Id, InvoiceNumber, Seller, Customer, Items (nested maps above), Comments, DueDate, CreatedDate | UserId, TotalPrice, Status |

## Design decisions

1. **Hand-written extension methods, as in Biller.** Keep the file names, replace the contents:
   `MappingProfiles/<X>MappingProfile.cs` holds `public static class <X>ResponseExtensions` with
   `ToUpdateRequest(this <X>Response from)`. Pages call `item.ToUpdateRequest()`.
2. **Set every request property explicitly**, in the request's declaration order, so a review can
   check them line by line against the contract.
3. **Missing seller or customer is an error.** `InvoiceResponse.Seller` and `.Customer` are nullable,
   but `InvoiceUpdateRequest` requires them. Throw `InvalidOperationException($"Invoice {from.Id} has no seller")`
   (and the same for customer) instead of passing `null` on. `Items` of `null` becomes an empty list.
4. **Tests replace the startup check.** `AssertConfigurationIsValid` caught unmapped properties
   at startup. Its replacement is one unit test per method, building the response with every property
   non-default and checking that every request property got its source value, like Biller's
   `MappingTestHelper.TestMapp(..., MapStyle.MappedAllTo)`. A new request property without a mapping
   then fails a test.
5. **No other mapping library** (Mapster, Mapperly, …) either.

## Steps

1. Write the 6 methods in the 5 `MappingProfiles/*.cs` files. Remove the `Profile` classes.
2. In the 6 pages, replace `_mapper.Map<X>(y)` with `y.ToUpdateRequest()`, and remove `@using AutoMapper`
   and `@inject IMapper _mapper` (also from `Invoices_id.razor`, which doesn't use it).
3. Delete `Capabilities/StartupMapper.cs` and remove `.ConfigureAutoMapper()` from `Program.cs`.
4. Remove the `AutoMapper` PackageReference from `src/FrontEnd/FrontEnd.csproj` and `src/Domain/Domain.csproj`.
5. Mapping tests (design decision 4), one per method.
6. Check:
   - `grep -rn AutoMapper src` finds nothing.
   - `dotnet build BillerFrontEnd.sln -c Release` passes, and the tests pass.
   - Against a running Biller, open and save each update page: customer, item, seller, user, and
     invoice (including picking another seller and customer). The saved record keeps every field.

## Order and other plans

- **BIL-87 (front-end tests):** step 5 needs its test project. Do BIL-87 step 1 (test project and CI
  step) and this plan in one PR, so the new mapping is never untested. The mapping tests replace
  BIL-87's AutoMapper profile tests.
- **BIL-65 (series):** removes `InvoiceNumber` from `InvoiceUpdateRequest`, and `CustomerUpdateRequest`
  loses `InvoiceName`/`InvoiceNumber` and gains `SeriesId`. Whichever lands second updates the mapping
  methods. The tests then fail until it does.
- **BIL-75 (units):** adds a unit to items. The item mapping must copy it, and the test will say so.

## Risk

UI only, no API or data change. The real risk is a missed property: update requests replace the
whole record, so a property left at its default is saved as empty and overwrites the stored value
(for example a customer's email). Design decision 4 and the manual save check in step 6 cover this.

## How to revert

Revert the PR.

## Out of scope

- Changing which fields the update pages send.
- Moving other page logic out of the `.razor` files.
