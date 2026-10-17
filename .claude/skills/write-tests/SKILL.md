---
name: write-tests
description: Write or fix unit tests in Biller's xUnit project (tests/xUnitTests) following the repo's conventions — hand-built test data via TestData (no AutoFixture), strict Moq mocks with Verify, xUnit v3 theories with a Label per case, and the per-layer patterns for services, controllers, validators, mapping extensions, Swagger examples and DI registrations. Use this whenever the user wants tests added, extended, converted, reviewed or cleaned up in the Biller repo ("add tests for X", "cover this method", "close the coverage gap", "review this test file", "remove AutoData"), and whenever another skill reaches its testing step.
---

# Writing tests for Biller

One test project, `tests/xUnitTests`, mirrors `src` folder by folder
(`src/Validators/Invoice/X.cs` → `tests/xUnitTests/Validators/Invoice/XTest.cs`).
Stack: xUnit v3 (`xunit.v3`), Moq, FluentAssertions. AutoFixture has been removed from the project.

Before writing, open the code under test **and** a neighboring test in the same folder, and
mirror its layout. The rules below win where older tests disagree.

## Hard rules

- **No AutoFixture.** It was removed from the project, so don't add it back. Build data by hand:
  `tests/xUnitTests/Helpers/TestData.cs` has `TestData.New<X>Entity()` / `New<X>Model()` builders
  with every property set and fresh ids. Use them when a test needs a full object, and add a
  builder there for a new type. Use object initializers with only the relevant fields
  (`new SellerEntity { Id = Guid.NewGuid() }`) when the rest doesn't matter. Create exactly the
  entities the test needs, and pass the matching one by reference (`seller.Id`), never by index
  into a generated list (`sellers[1]`).
- **Verify mock calls.** Mocks are `new Mock<IX>(MockBehavior.Strict)`. Every `Setup` gets a
  matching `Verify(m => ..., Times.Once())` (or the exact expected count, e.g. `Times.Never()`) in
  the Assert section. Controller tests end with `VerifyNoOtherCalls()` on each mock.
- **Set up with exact arguments**, not `It.IsAny<T>()`, unless the argument is created inside the
  code under test and can't be known.
- **Expected values come from the spec, not from running the code.** If the current behaviour looks
  wrong (e.g. an empty list passes a validity check), don't encode it in a test. Ask the user.

## Shape of a test

- One flat test class per class under test, named `<Class>Test`. No nested classes or `#region`s.
- Shared mocks and the system under test are `private readonly` fields created in the constructor.
- Names: `Method_GivenCondition_ExpectedResult`, e.g. `IsValidSellerId_GivenOtherSeller_ReturnsFalse`.
  A theory that covers several conditions uses `Method_GivenX_ReturnsExpected`.
- Body sections are marked `//Arrange`, `//Act`, `//Assert`.
- Assert with FluentAssertions: `Should().Be`, `Should().BeEquivalentTo` for objects,
  `await act.Should().ThrowAsync<NotFoundException>()` for exceptions.

## Facts vs. theories

- `[Fact]` when a case needs its own arrange logic.
- `[Theory, InlineData(...)]` for primitive / string / enum variations (boundaries, languages).
- `[Theory, MemberData(nameof(GetXTestData))]` when cases differ only in their inputs and those
  inputs are collections, entities or `Guid`s, which `InlineData` can't hold. The data method is a
  `public static IEnumerable<TheoryDataRow<T1, T2, ...>>` placed directly above the theory, with
  one `yield return new(...)` per case. Pass the expected result as the last parameter.

**Every theory case has a title**, set with `Label`. Test Explorer and the test output then show
`Method [user has no sellers, returns false]` instead of a dump of the arguments. A title is a
lowercase sentence that names the situation (what the input or repository state is, in domain
terms) and the expected outcome, so a failing case is understood without opening the data method.
`"no sellers"` or `"case 2"` are too short. Titles replace `// comments` above cases. Don't use
`TestDisplayName`: it drops the method name and still appends every argument.

  ```csharp
  public static IEnumerable<TheoryDataRow<List<SellerEntity>, Guid, bool>> GetSellerTestData()
  {
      SellerEntity seller = new() { Id = Guid.NewGuid() };
      SellerEntity otherSeller = new() { Id = Guid.NewGuid() };

      yield return new([otherSeller, seller], seller.Id, true) { Label = "seller id is one of the user's sellers, returns true" };
      yield return new([], Guid.NewGuid(), false) { Label = "user has no sellers, returns false" };
  }

  [Theory]
  [MemberData(nameof(GetSellerTestData))]
  public async Task IsValidSellerId_GivenUsersSellers_ReturnsExpected(List<SellerEntity> sellers, Guid sellerId, bool expected)
  ```

  ```csharp
  [Theory]
  [InlineData(0, "nulis", Label = "zero is spelled out as nulis")]
  [InlineData(11, "vienuolika", Label = "teen number uses its own word")]
  public void Convert_GivenNumber_ReturnsWords(int number, string expected)
  ```

## Cases to cover

For each public method: the happy path, each way it can fail, and the edges — empty collections,
not found (`null` from the repository), duplicates, and IDs that belong to another
user/seller/customer. Flag behaviour that looks wrong to the user instead of testing around it.

## Per-layer patterns

- **Services** (`Application/Services`): strict repository mocks; `Get`/`Update`/`Delete` each
  get a `NotFoundException` case when the repository returns `null`; verify every repository call.
- **Controllers** (`WebAPI/Controllers`): strict mocks for the service and each `IValidator<T>`
  (set up `Validate(request)` to return `new ValidationResult()`), a loose
  `Mock<ILogger<XController>>`. Assert the `IActionResult` type (`OkObjectResult`,
  `CreatedAtActionResult`, `NoContentResult`) and its value against `model.ToResponse()`, then
  `VerifyNoOtherCalls()`.
- **Request validators** (`Validators/<Entity>`): no mocks. A `private static ValidRequest()`
  factory, one test proving it is valid, then one test per rule that breaks a single field and asserts
  `result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Field))`. Boundary values
  via `[Theory, InlineData]`.
- **Validators with repositories** (e.g. `InvoiceValidator`): strict repository mocks with
  `Verify`, `MemberData` theories feeding the repository result, the ID(s) to check and the
  expected `bool`.
- **Mapping extensions** (`Application/MappingProfiles`): build the source by hand with every
  property non-default, then `MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo)` for
  model/entity/response targets or `MapStyle.UsedAllFrom` for requests. One test per extension
  method.
- **Swagger examples** (`WebAPI/Swagger`): one test per example asserting
  `NullChecker.GetNullOrEmptyProperties(example.GetExamples())` is empty. Instantiate the **right** example
  class; copy-paste slips (an `Item...` test building an `Invoice...` example) have happened.
- **DI registrations** (`Application/DependencyInjectionTests.cs`,
  `Validators/DependencyInjectionTests.cs`): add an assert for every new service, repository or
  validator.
- **Number-to-words / price-to-words** (`Application/Helpers`): many `[InlineData]` rows; the
  `add-language` skill lists the values to cover.

## Finish

Run the new tests with the `test-coverage` skill (use `--filter "FullyQualifiedName~<Class>Test"`
for a quick run, then the full suite). Report the pass count, and any behaviour you flagged
instead of testing.
