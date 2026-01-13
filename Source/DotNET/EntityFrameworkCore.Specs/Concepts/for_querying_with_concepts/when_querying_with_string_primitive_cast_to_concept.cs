// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_querying_with_concepts;

/// <summary>
/// Tests the scenario where a primitive string is cast to a string-based ConceptAs type
/// and compared against an entity property.
/// This was causing "The binary operator Equal is not defined for the types 'System.String'
/// and 'ConceptType'" errors when the expression visitor didn't properly handle the cast.
/// </summary>
[Collection(nameof(ConceptAsQueryingCollection))]
public class when_querying_with_string_primitive_cast_to_concept : given.a_test_database
{
    string _nameValue;
    Product? _result;

    async Task Establish()
    {
        _nameValue = "Cast Test Product";
        var product = new Product
        {
            Id = ProductId.New(),
            Code = new ProductCode(555),
            Name = new ProductName(_nameValue),
            Price = 19.99m
        };
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Compare a primitive string cast to ProductName against the entity's Name property.
    /// The rewriter must handle the type mismatch: string vs ProductName.
    /// </summary>
    async Task Because() => _result = await _context.Products
        .Where(p => (ProductName)_nameValue == p.Name)
        .FirstOrDefaultAsync();

    [Fact] void should_find_the_product() => _result.ShouldNotBeNull();
    [Fact] void should_have_correct_code() => _result.Code.Value.ShouldEqual(555);
    [Fact] void should_have_correct_name() => _result.Name.Value.ShouldEqual("Cast Test Product");
}
