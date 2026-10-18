---
name: write-tests
description: Write or fix unit tests in Biller's xUnit project (tests/xUnitTests) following the repo's conventions — hand-built test data with object initializers (no AutoFixture, no shared TestData builders), strict Moq mocks with Verify, xUnit v3 theories with a Label per case, and the per-layer patterns for services, controllers, validators, mapping extensions, Swagger examples and DI registrations. Use this whenever the user wants tests added, extended, converted, reviewed or cleaned up in the Biller repo ("add tests for X", "cover this method", "close the coverage gap", "review this test file", "remove AutoData"), and whenever another skill reaches its testing step.
---

# Writing tests for Biller

One test project, `tests/xUnitTests`, mirrors `src` folder by folder and file by file
(`src/Validators/Invoice/X.cs` → `tests/xUnitTests/Validators/Invoice/XTest.cs`): one test file
per source file, never one per class.
Stack: xUnit v3 (`xunit.v3`), Moq, FluentAssertions. AutoFixture has been removed from the project.

Before writing, open the code under test **and** a neighboring test in the same folder, and
mirror its layout. The rules below win where older tests disagree.

## Hard rules

- **No AutoFixture, no shared builders.** AutoFixture was removed from the project, so don't add
  it back, and don't add a shared `TestData` helper class. Build data inline with object
  initializers setting only the fields the test itself references (in a setup, call or assert).
  If `GetId` only uses `customer.Id`, write `new() { Id = Guid.NewGuid() }`, not
  `new() { Id = Guid.NewGuid(), SellerId = Guid.NewGuid() }`; a list whose ids are never read is
  `[new(), new(), new()]`. When a test
  class needs a larger object graph many times (e.g. a full invoice), give that class its own
  `private static New<X>()` method. Create exactly the
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
- When a source file holds several classes, its single test file holds one top-level test class
  per class, in the same order as the source. For example,
  `src/Application/Helpers/NumberToWords/NumberToWordsEN.cs` defines `NumberToWordsEN`,
  `OnesToWordsEN`, … `MillionsToWordsEN`, so `NumberToWordsENTest.cs` holds `NumberToWordsENTest`,
  `OnesToWordsENTest`, … `MillionsToWordsENTest`. Each class has its own mocks and constructor.
  Don't merge classes under test into one test class, and don't split them into extra files.
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
  factory, one test proving it is valid, then tests that break a single field and assert
  `result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Field))`. Fields that fail
  the same way (required field left empty) share one theory,
  `Validate_GivenEmptyRequiredField_IsInvalid`, fed by
  `IEnumerable<TheoryDataRow<Action<TRequest>, string>>` with one labelled row per field:
  `yield return new(r => r.CompanyName = string.Empty, nameof(TRequest.CompanyName)) { Label = "empty company name is rejected" };`.
  Rules with their own logic (email format, boundaries, nested validators) keep their own test;
  boundary values via `[Theory, InlineData]`.
- **Validators with repositories** (e.g. `InvoiceValidator`): strict repository mocks with
  `Verify`, `MemberData` theories feeding the repository result, the ID(s) to check and the
  expected `bool`.
- **Mapping extensions** (`Application/MappingProfiles`): build the source by hand with every
  property non-default, then `MappingTestHelper.TestMapp(from, to, MapStyle.MappedAllTo)` for
  model/entity/response targets or `MapStyle.UsedAllFrom` for requests. One test per extension
  method.
- **Swagger examples** (`WebAPI/Swagger`): one theory per entity file,
  `GetExamples_GivenExample_HasNoNullOrEmptyProperties(Func<object> getExamples)`, asserting
  `NullChecker.GetNullOrEmptyProperties(getExamples())` is empty. Add a row per example:
  `yield return new(() => new ItemAddRequestExample().GetExamples()) { Label = "item add request example has no empty properties" };`.
  Use the **right** example class in each row; copy-paste slips (an `Item...` row building an
  `Invoice...` example) have happened.
- **DI registrations** (`Application/DependencyInjectionTests.cs`,
  `Validators/DependencyInjectionTests.cs`): add an assert for every new service, repository or
  validator.
- **Number-to-words / price-to-words** (`Application/Helpers`): many `[InlineData]` rows; the
  `add-language` skill lists the values to cover.

## Finish

Run the new tests with the `test-coverage` skill (use `--filter "FullyQualifiedName~<Class>Test"`
for a quick run, then the full suite). Report the pass count, and any behaviour you flagged
instead of testing.
