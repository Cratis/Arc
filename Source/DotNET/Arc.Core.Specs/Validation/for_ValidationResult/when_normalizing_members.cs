// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation.for_ValidationResult;

public class when_normalizing_members : Specification
{
    [Theory]
    [InlineData("Name", "name")]
    [InlineData("Address.Street", "address.street")]
    [InlineData("Items[0].Name", "items[0].name")]
    [InlineData("items[0].name", "items[0].name")]
    [InlineData("", "")]
    [InlineData("$", "$")]
    [InlineData("Not a property path", "Not a property path")]
    [InlineData("Items[unknown].Name", "Items[unknown].Name")]
    [InlineData(".Name", ".Name")]
    [InlineData("Items[", "Items[")]
    [InlineData("Items[]", "Items[]")]
    [InlineData("Items[0]extra", "Items[0]extra")]
    [InlineData("Items[0][12].Name", "items[0][12].name")]
    [InlineData("Address..Street", "Address..Street")]
    void should_normalize_only_field_paths_on_construction(string member, string expected) =>
        new ValidationResult(ValidationResultSeverity.Error, "Invalid", [member], null!).Members.ShouldContainOnly(expected);

    [Theory]
    [InlineData(ValidationResultSeverity.Error)]
    [InlineData(ValidationResultSeverity.Warning)]
    [InlineData(ValidationResultSeverity.Information)]
    void should_normalize_factory_results(ValidationResultSeverity severity)
    {
        var result = severity switch
        {
            ValidationResultSeverity.Error => ValidationResult.Error("Invalid", ["Address.Street"]),
            ValidationResultSeverity.Warning => ValidationResult.Warning("Invalid", ["Address.Street"]),
            _ => ValidationResult.Information("Invalid", ["Address.Street"])
        };
        result.Members.ShouldContainOnly("address.street");
    }

    [Fact]
    void should_normalize_members_replaced_with_a_record_copy() =>
        (ValidationResult.Error("Invalid", ["Name"]) with { Members = ["Items[0].Name"] }).Members.ShouldContainOnly("items[0].name");

    [Fact]
    void should_treat_null_members_on_construction_as_empty() =>
        new ValidationResult(ValidationResultSeverity.Error, "Invalid", null!, null!).Members.ShouldBeEmpty();

    [Fact]
    void should_treat_null_members_replaced_with_a_record_copy_as_empty() =>
        (ValidationResult.Error("Invalid", ["Name"]) with { Members = null! }).Members.ShouldBeEmpty();

    [Fact]
    void should_be_idempotent()
    {
        var result = ValidationResult.Error("Invalid", ["Items[0].Name"]);
        (result with { Members = result.Members }).Members.ShouldContainOnly("items[0].name");
    }
}
