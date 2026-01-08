// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_querying_with_concepts;

[Collection(nameof(ConceptAsQueryingCollection))]
public class when_querying_with_reverse_primitive_comparison : given.a_test_database
{
    int _codeValue;
    Product? _result;

    async Task Establish()
    {
        _codeValue = 777;
        var product = new Product
        {
            Id = ProductId.New(),
            Code = new ProductCode(_codeValue),
            Name = new ProductName("Reverse Comparison Test"),
            Price = 34.99m
        };
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
    }

    async Task Because() => _result = await _context.Products
        .Where(p => new ProductCode(_codeValue) == p.Code)
        .FirstOrDefaultAsync();

    [Fact] void should_find_the_product() => _result.ShouldNotBeNull();
    [Fact] void should_have_correct_code() => _result!.Code.Value.ShouldEqual(777);
    [Fact] void should_have_correct_name() => _result!.Name.Value.ShouldEqual("Reverse Comparison Test");
}
