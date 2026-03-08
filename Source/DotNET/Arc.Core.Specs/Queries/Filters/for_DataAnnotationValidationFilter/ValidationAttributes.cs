// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649, SA1402

using System.ComponentModel.DataAnnotations;

namespace Cratis.Arc.Queries.Filters.for_DataAnnotationValidationFilter;

/// <summary>
/// Test validation attribute that always succeeds, for use in specs that verify
/// the happy-path flow through <see cref="DataAnnotationValidationFilter"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class AlwaysPassesAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) =>
        ValidationResult.Success;
}

/// <summary>
/// Test validation attribute that always fails with a deterministic message, for use in specs
/// that verify <see cref="DataAnnotationValidationFilter"/> correctly propagates validation failures.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class AlwaysFailsAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object? value, ValidationContext validationContext) =>
        new("Validation always fails.", [validationContext.MemberName ?? "unknown"]);
}
