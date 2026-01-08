// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_querying_with_concepts;

[Collection(nameof(ConceptAsQueryingCollection))]
public class when_querying_by_primitive_guid : given.a_test_database
{
    Guid _guidValue;
    Product? _result;

    async Task Establish()
    {
        _guidValue = Guid.NewGuid();
        var product = new Product
        {
            Id = new ProductId(_guidValue),
            Code = new ProductCode(100),
            Name = new ProductName("Guid Test"),
            Price = 9.99m
        };
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
    }

    async Task Because() => _result = await _context.Products
        .Where(p => p.Id == new ProductId(_guidValue))
        .FirstOrDefaultAsync();

    [Fact] void should_find_the_product() => _result.ShouldNotBeNull();
    [Fact] void should_have_correct_id() => _result!.Id.Value.ShouldEqual(_guidValue);
    [Fact] void should_have_correct_code() => _result!.Code.Value.ShouldEqual(100);
}
