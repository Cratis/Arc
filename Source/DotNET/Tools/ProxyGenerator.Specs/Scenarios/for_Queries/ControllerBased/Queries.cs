// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ControllerBased;

/// <summary>
/// Controller for testing controller-based queries.
/// </summary>
[ApiController]
[Route("api/controller-queries")]
public class ControllerQueriesController : ControllerBase
{
    /// <summary>
    /// Gets all items.
    /// </summary>
    /// <returns>Collection of items.</returns>
    [HttpGet("items")]
    public ActionResult<IEnumerable<ControllerQueryItem>> GetAll()
    {
        return Ok(new List<ControllerQueryItem>
        {
            new() { Id = Guid.NewGuid(), Name = "Item 1", Value = 1 },
            new() { Id = Guid.NewGuid(), Name = "Item 2", Value = 2 }
        });
    }

    /// <summary>
    /// Gets a single item by ID.
    /// </summary>
    /// <param name="id">The item ID.</param>
    /// <returns>The item.</returns>
    [HttpGet("items/{id}")]
    public ActionResult<ControllerQueryItem> GetById([FromRoute] Guid id)
    {
        return Ok(new ControllerQueryItem
        {
            Id = id,
            Name = $"Item {id}",
            Value = 42
        });
    }

    /// <summary>
    /// Searches items with query parameters.
    /// </summary>
    /// <param name="name">The name filter.</param>
    /// <param name="minValue">The minimum value filter.</param>
    /// <param name="maxValue">The maximum value filter.</param>
    /// <returns>Collection of matching items.</returns>
    [HttpGet("items/search")]
    public ActionResult<IEnumerable<ControllerQueryItem>> Search(
        [FromQuery] string? name,
        [FromQuery] int? minValue,
        [FromQuery] int? maxValue)
    {
        return Ok(new List<ControllerQueryItem>
        {
            new() { Id = Guid.NewGuid(), Name = name ?? "Default", Value = minValue ?? 0 }
        });
    }

    /// <summary>
    /// Gets authorized data.
    /// </summary>
    /// <returns>The authorized data.</returns>
    [HttpGet("authorized")]
    [Authorize]
    public ActionResult<ControllerQueryItem> GetAuthorized()
    {
        return Ok(new ControllerQueryItem
        {
            Id = Guid.NewGuid(),
            Name = "Authorized Item",
            Value = 999
        });
    }

    /// <summary>
    /// Gets data that may throw.
    /// </summary>
    /// <param name="shouldThrow">Whether to throw an exception.</param>
    /// <returns>The item.</returns>
    /// <exception cref="QueryExecutionFailed">The exception that is thrown when <paramref name="shouldThrow"/> is true.</exception>
    [HttpGet("exception")]
    public ActionResult<ControllerQueryItem> GetWithException([FromQuery] bool shouldThrow)
    {
        if (shouldThrow)
        {
            throw new QueryExecutionFailed("Controller query exception");
        }

        return Ok(new ControllerQueryItem { Id = Guid.NewGuid(), Name = "Success", Value = 1 });
    }

    /// <summary>
    /// Gets complex nested data.
    /// </summary>
    /// <returns>The complex item.</returns>
    [HttpGet("complex")]
    public ActionResult<ControllerComplexItem> GetComplex()
    {
        return Ok(new ControllerComplexItem
        {
            Id = Guid.NewGuid(),
            Name = "Complex Item",
            Nested = new ControllerNestedItem
            {
                Description = "Nested data",
                Amount = 123.45m,
                ProcessingDuration = TimeSpan.FromSeconds(45)
            },
            Items = [1, 2, 3],
            Tags = new Dictionary<string, string>
            {
                ["tag1"] = "value1",
                ["tag2"] = "value2"
            }
        });
    }

    internal static int FluentValidatedCallCount;

    /// <summary>
    /// Searches with fluent validation.
    /// </summary>
    /// <param name="email">The email filter.</param>
    /// <param name="minAge">Minimum age.</param>
    /// <returns>Collection of items.</returns>
    [HttpGet("fluent-validated")]
    public ActionResult<IEnumerable<ControllerQueryItem>> SearchFluentValidated(
        [FromQuery] string email,
        [FromQuery] int minAge)
    {
        FluentValidatedCallCount++;
        return Ok(new List<ControllerQueryItem>
        {
            new() { Id = Guid.NewGuid(), Name = email, Value = minAge }
        });
    }

    internal static int DataAnnotationsValidatedCallCount;

    /// <summary>
    /// Searches with DataAnnotations validation.
    /// </summary>
    /// <param name="email">The email filter.</param>
    /// <param name="name">The name filter.</param>
    /// <param name="minAge">Minimum age.</param>
    /// <param name="website">Website URL.</param>
    /// <returns>Collection of items.</returns>
    [HttpGet("data-annotations-validated")]
    public ActionResult<IEnumerable<ControllerQueryItem>> SearchDataAnnotationsValidated(
        [FromQuery]
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.EmailAddress]
        string email,
        [FromQuery]
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(50, MinimumLength = 3)]
        string name,
        [FromQuery]
        [System.ComponentModel.DataAnnotations.Range(0, 150)]
        int minAge,
        [FromQuery]
        [System.ComponentModel.DataAnnotations.Url]
        string website)
    {
        DataAnnotationsValidatedCallCount++;
        return Ok(new List<ControllerQueryItem>
        {
            new() { Id = Guid.NewGuid(), Name = name, Value = minAge }
        });
    }
}

/// <summary>
/// A query item for controller-based testing.
/// </summary>
public class ControllerQueryItem
{
    /// <summary>
    /// Gets or sets the ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public int Value { get; set; }
}

/// <summary>
/// A complex item for controller-based testing.
/// </summary>
public class ControllerComplexItem
{
    /// <summary>
    /// Gets or sets the ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the nested item.
    /// </summary>
    public ControllerNestedItem? Nested { get; set; }

    /// <summary>
    /// Gets or sets the items.
    /// </summary>
    public List<int> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets the tags.
    /// </summary>
    public Dictionary<string, string> Tags { get; set; } = [];
}

/// <summary>
/// A nested item for controller-based testing.
/// </summary>
public class ControllerNestedItem
{
    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the processing duration.
    /// </summary>
    public TimeSpan ProcessingDuration { get; set; }
}

/// <summary>
/// Query DTO for fluent validated search.
/// </summary>
public class ControllerFluentValidatedQuery
{
    /// <summary>
    /// Gets or sets the email.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the minimum age.
    /// </summary>
    public int MinAge { get; set; }
}

/// <summary>
/// Validator for ControllerFluentValidatedQuery using QueryValidator.
/// </summary>
public class ControllerFluentValidatedQueryValidator : QueryValidator<ControllerFluentValidatedQuery>
{
    public const string EmailRequiredMessage = "Valid email is required";
    public const string AgeRangeMessage = "Age must be between 0 and 150";

    public ControllerFluentValidatedQueryValidator()
    {
        RuleFor(q => q.Email).NotEmpty().WithMessage(EmailRequiredMessage).EmailAddress().WithMessage(EmailRequiredMessage);
        RuleFor(q => q.MinAge).GreaterThanOrEqualTo(0).WithMessage(AgeRangeMessage).LessThan(150).WithMessage(AgeRangeMessage);
    }
}

/// <summary>
/// Query DTO for abstract validated search.
/// </summary>
public class ControllerAbstractValidatedQuery
{
    /// <summary>
    /// Gets or sets the code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the minimum amount.
    /// </summary>
    public decimal MinAmount { get; set; }
}

/// <summary>
/// Validator for ControllerAbstractValidatedQuery using AbstractValidator directly.
/// </summary>
public class ControllerAbstractValidatedQueryValidator : AbstractValidator<ControllerAbstractValidatedQuery>
{
    public ControllerAbstractValidatedQueryValidator()
    {
        RuleFor(q => q.Code).NotEmpty().Length(5, 10).WithMessage("Code must be between 5 and 10 characters");
        RuleFor(q => q.MinAmount).GreaterThan(0).WithMessage("Amount must be positive");
    }
}
