// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_querying_with_concepts;

[Collection(nameof(ConceptAsQueryingCollection))]
public class when_querying_by_primitive_string : given.a_test_database
{
    string _nameValue;
    Product? _result;

    async Task Establish()
    {
        _nameValue = "String Test Product";
        var product = new Product
        {
            Id = ProductId.New(),
            Code = new ProductCode(333),
            Name = new ProductName(_nameValue),
            Price = 24.99m
        };
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
    }

    async Task Because() => _result = await _context.Products
        .Where(p => p.Name == new ProductName(_nameValue))
        .FirstOrDefaultAsync();

    [Fact] void should_find_the_product() => _result.ShouldNotBeNull();
    [Fact] void should_have_correct_code() => _result!.Code.Value.ShouldEqual(333);
    [Fact] void should_have_correct_name() => _result!.Name.Value.ShouldEqual("String Test Product");
}
