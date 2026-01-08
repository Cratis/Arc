// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_querying_with_concepts;

[Collection(nameof(ConceptAsQueryingCollection))]
public class when_property_mapping_concepts : given.a_test_database
{
    Guid _guidValue;
    int _codeValue;
    string _nameValue;
    Product? _result;

    async Task Establish()
    {
        _guidValue = Guid.NewGuid();
        _codeValue = 123;
        _nameValue = "Property Mapping Test";

        var product = new Product
        {
            Id = new ProductId(_guidValue),
            Code = new ProductCode(_codeValue),
            Name = new ProductName(_nameValue),
            Price = 99.99m
        };
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();
    }

    async Task Because() => _result = await _context.Products
        .FirstOrDefaultAsync(p => p.Id == new ProductId(_guidValue));

    [Fact] void should_correctly_map_guid_concept_property() => _result!.Id.Value.ShouldEqual(_guidValue);
    [Fact] void should_correctly_map_int_concept_property() => _result!.Code.Value.ShouldEqual(_codeValue);
    [Fact] void should_correctly_map_string_concept_property() => _result!.Name.Value.ShouldEqual(_nameValue);
    [Fact] void should_correctly_map_all_properties_together() => _result.ShouldNotBeNull();
}
