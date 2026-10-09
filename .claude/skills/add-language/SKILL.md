---
name: add-language
description: Add a new document language to Biller's PDF generation (e.g. Latvian, Polish, German) — the Language enum value, invoice label translations (IInvoiceTexts), the number/amount-to-words converter with correct grammar, factory registrations, currency wording, and thorough tests — then render sample PDFs. Use this whenever the user wants invoices or acts in another language, asks to translate invoice texts, fix how amounts are spelled out in words ("suma žodžiais", "amount in words"), or fix number-to-words grammar in LT/EN, even if they don't say "language".
---

# Add a document language

A language touches five places, all keyed by `BillerContracts.Enums.Language`:

| # | What | Where |
|---|---|---|
| 1 | Enum value: ISO 3166-1 alpha-2 code (e.g. `LV`) | BillerContracts package: use the `release-contracts` skill. Append at the end |
| 2 | Labels | `src/Application/Helpers/Invoice/InvoiceTexts<LANG>.cs`: `readonly struct` implementing `IInvoiceTexts` |
| 3 | Number → words | `src/Application/Helpers/NumberToWords/NumberToWords<LANG>.cs`: `OnesToWords`, `TensToWords`, `HundredsToWords`, `ThousandsToWords`, `MillionsToWords` + `NumberToWords<LANG>` façade |
| 4 | Wiring | `PriceToWordsFactory.GetConverter` (builds the chain) and `InvoiceDocumentFactory.GetTexts` |
| 5 | Tests | `tests/xUnitTests/Application/Helpers/NumberToWords/NumberToWords<LANG>Test.cs`, `PriceToWords/PriceToWords<LANG>Test.cs`, plus `[InlineData(Language.<LANG>)]` in `PriceToWordsFactoryTest` |

## The language code

`<LANG>` (the `Language` enum value and the class-name suffix everywhere below) is the
**ISO 3166-1 alpha-2** code of the country, in uppercase: `LV`, `PL`, `DE`, `EE`, `CZ`, `SE`, `DK`,
`UA`, `GR`. It is the country code, not the ISO 639-1 language code. These often differ, for
example Estonian is `EE` (not `ET`), Czech `CZ` (not `CS`), Swedish `SE` (not `SV`), Danish
`DK` (not `DA`), Ukrainian `UA` (not `UK`), and Greek `GR` (not `EL`). Use the same code in
the enum, the `InvoiceTexts<LANG>` / `NumberToWords<LANG>` / `*ToWords<LANG>` class names, and the
test class names.

The existing `EN` predates this rule and isn't a 3166-1 code (it would be `GB` or `US`). Leave it
as is unless the user asks to rename it, because that would be a breaking contracts change. If a
language is spoken in several countries, ask the user which country's code to use.

Before starting, confirm with the user: the language and its ISO 3166-1 alpha-2 code, whether they have
authoritative translations for the legal labels (invoice titles and seller/buyer terms have legal
meaning; machine translation is a draft to be checked), and the currency wording.

## 1. Labels

Copy `InvoiceTextsEN.cs`, translate every member, and keep the composite ones composite (for
example, `IndividualNumber()` uses `NumberShort()`, and the price/sum headers append `Currency()`).
Use the language's real diacritics. Mark any string you're unsure of with `// TODO verify` and list
them for the user.

## 2. Number to words, the hard part

Each class handles one magnitude and delegates down. `ThousandsSplit` and `MillionsSplit` pick the
noun form from the count, and `hasBefore` controls the leading space. Read `NumberToWordsLT.cs`
for a language with case/number agreement. LT uses three noun forms: `tūkstantis` (ends in 1,
not 11), `tūkstančiai` (2–9), and `tūkstančių` (0, or 10–20 / teens). Read `NumberToWordsEN.cs`
for a simpler model. Pick whichever is closer to the target language's grammar, then work out
its rules explicitly before coding:
- the forms for 1 / few / many (Slavic and Baltic languages have 3; Latvian has special 1 vs. others
  rules, as with LT),
- the teens and compound tens (does the language invert order like German "einundzwanzig"? join
  with hyphens?),
- gender agreement of "one/two" with thousand/million nouns,
- zero, and whether "one thousand" says "one".

The range is 0 … 999 999 999. Negative or ≥ 1e9 throws `ArgumentException`; keep that contract.

## 3. Currency wording

`PriceToWords.Decode` currently hard-codes `"{words} € {cents} ct."` for every language. If the
new language needs different currency words or order, don't special-case inside `Decode`. Add
`Currency`/`Cents` wording to the converter (e.g. a constructor parameter supplied by
`PriceToWordsFactory`) and keep LT/EN output byte-identical. Their existing tests must stay green.

## 4. Wiring

Add the case to both switches. The `_ => throw NotSupportedException` default must stay.

## 5. Tests: be generous

Mirror `NumberToWordsLTTest` / `PriceToWordsLTTest`, with dozens of `[InlineData]` rows, not a handful.
Cover at least: 0, 1, 2, 5, 10, 11, 12, 19, 20, 21, 22, 99, 100, 101, 111, 200, 999, 1 000, 1 001, 2 000,
5 000, 11 000, 21 000, 100 000, 999 999, 1 000 000, 1 000 001, 2 000 000, 21 000 000,
999 999 999, out-of-range throws, and price decoding with 0, .01, .10, .99, and truncation
(`1.999` → 99 ct). Take the expected strings from a native-language source or the user, not from
your own implementation's output.

## 6. Verify

1. `test-coverage` skill: everything green, new classes covered.
2. `new-invoice-layout` skill's render script. `render-samples.sh Invoice <LANG>` and
   `render-samples.sh JobDoneAct <LANG>` render every document type in the new language. Open the
   PDFs and check that labels fit their columns (translations are often longer), that diacritics
   render with the calibri font, and that amount-in-words matches the total.
3. Remind the user about the BillerContracts release (the enum) and any `// TODO verify`
   translations.
