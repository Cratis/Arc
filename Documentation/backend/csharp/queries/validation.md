---
title: Query validation
description: Validate model-bound scalar arguments with FluentValidation and distinguish MVC validation.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Reject malformed search input before the query reaches storage. Arc's model-bound query pipeline supports discovered FluentValidation validators; MVC actions use their own model-binding/model-state path. Choose the example for the endpoint style you actually expose.

## Validate the whole argument set

A parameter model lets you validate required strings and relationships between scalar arguments without expecting HTTP to bind a complex DTO. This alternative banking declaration uses the [shared `AccountId` and `AccountName` concepts](model-bound/index.md#model-account-identities-and-names), the Arc MongoDB provider, and validator discovery. The prefix is search text, so its rule belongs to the query rather than to the complete account-name value:

```csharp
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using FluentValidation;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Banking.Accounts;

[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, decimal Balance)
{
    [Path("/api/accounts/search")]
    public static IEnumerable<DebitAccount> Search(
        string prefix,
        decimal minimumBalance,
        decimal maximumBalance,
        IMongoCollection<DebitAccount> collection)
    {
        var namePrefix = new BsonRegularExpression("^" + Regex.Escape(prefix));
        var filter = Builders<DebitAccount>.Filter.Regex(account => account.Name, namePrefix) &
            Builders<DebitAccount>.Filter.Gte(account => account.Balance, minimumBalance) &
            Builders<DebitAccount>.Filter.Lte(account => account.Balance, maximumBalance);
        return collection.Find(filter).SortBy(account => account.Id).Limit(100).ToList();
    }
}

public class SearchParameters
{
    public string Prefix { get; set; } = string.Empty;
    public decimal MinimumBalance { get; set; }
    public decimal MaximumBalance { get; set; }
}

public class SearchValidator : QueryValidator<SearchParameters>
{
    public SearchValidator()
    {
        RuleFor(parameters => parameters.Prefix).NotEmpty().Length(3, 50);
        RuleFor(parameters => parameters.MinimumBalance).GreaterThanOrEqualTo(0);
        RuleFor(parameters => parameters.MaximumBalance)
            .GreaterThanOrEqualTo(parameters => parameters.MinimumBalance);
    }
}
```

```http
GET /api/accounts/search?prefix=Sav&minimumBalance=0&maximumBalance=1000
```

The query lives on the model it returns, so it qualifies for discovery. Its HTTP arguments remain flat. The provider filter searches the serialized `AccountName` field without weakening the domain model; `Regex.Escape` keeps the prefix literal. The 100-row limit is a fixed cap, not automatic paging. `SearchParameters` is materialized internally for validation; the method does not accept a `SearchParameters` HTTP DTO. Missing/short `prefix` or a reversed range fails validation before the method executes. This teaching example still needs [authorization](model-bound/authorization.md) before exposing private data.

## Argument-model discovery

Arc looks in the read model's assembly for a type named `{QueryName}Parameters` or `{ReadModelName}{QueryName}Parameters`. It must have properties with matching names **and types** covering every caller argument; injected dependencies do not count. Use unambiguous names within that assembly.

When a matching model can be materialized, the FluentValidation filter validates it as one object graph. Otherwise it falls back to validating individual supplied arguments. Missing/null individual arguments are skipped by that filter; the performer enforces its required-argument rules. Plain strings are implicitly optional in the current performer, which is why the whole-argument `NotEmpty` rule above matters.

Argument-model materialization leaves absent members at their type defaults; do not assume optional method defaults are copied into that validation object. Test omitted values as well as explicit ones. If the model cannot be constructed, fallback validation does not preserve cross-field rules that existed only on that model.

## Concepts and nested validation

A `ConceptValidator<T>` defines invariants for a strongly typed value and can apply to supplied query arguments as well as command properties. FluentValidation traverses the object graph, including nested values and collections, and reports failures using the argument/member path. Concept `Value` failures map to the containing field rather than exposing `.Value` to the client.

That validation capability does not add HTTP binding support: built-in model-bound GET/QUERY readers use [scalar conversion](model-bound/query-arguments.md#supported-input-shapes). Already-typed pipeline inputs or explicitly tested custom converters can carry richer values.

Generated clients extract supported FluentValidation rules for early feedback. Server validation remains authoritative; custom/async rules are not a blanket promise of browser/server parity. See [proxy validation](../proxy-generation/validation.md).

## DataAnnotations limitation

The current model-bound `DataAnnotationValidationFilter` reads attributes from `parameter.Type`, **not** the method parameter's attributes. It does not recursively validate DTO properties. Therefore `[Required]`, `[MinLength]`, or `[Range]` written on static query parameters does not enforce those rules through this filter.

Use the verified FluentValidation pattern above rather than presenting parameter annotations as a working model-bound validation recipe. This is a current runtime limitation; broader parameter-annotation support would require an implementation change.

## Controller-based validation

MVC has separate DataAnnotations/model-state behavior. Use [the complete MVC DTO example](controller-based/query-arguments.md#bind-and-validate-a-search-dto), where annotations are applied to DTO properties. MVC positional-record validation metadata also differs from ordinary object-property validation; do not mechanically transplant model-bound annotation advice into MVC records.

Arc's GET action filter uses model state and may wrap validation errors. `[ApiController]`, custom filters, and `[AspNetResult]` can change which response runs first or whether it is wrapped. Test the application's actual HTTP response instead of assuming one universal MVC error envelope.

## Validation results and security

A model-bound validation failure has `isValid: false`, `isSuccess: false`, and entries in `validationResults` containing `message`, `members`, and `severity`. Direct HTTP maps a validation verdict to 400. The response's paging fields are `page`, `size`, `totalItems`, and `totalPages`; see [the full QueryResult contract](query-pipeline.md#query-result-metadata).

Validation is not ownership or authorization. Put access decisions in an [authorization verdict/filter](query-pipeline.md#query-filters), and test forbidden callers independently of invalid input. At minimum test missing, empty, malformed, out-of-range, and cross-field-invalid values through GET, QUERY, and hub subscriptions when those paths are exposed.
