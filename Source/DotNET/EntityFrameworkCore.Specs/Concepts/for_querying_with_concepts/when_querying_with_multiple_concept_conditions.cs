// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_querying_with_concepts;

[Collection(nameof(ConceptAsQueryingCollection))]
public class when_querying_with_multiple_concept_conditions : given.a_test_database
{
    ProductId _productId;
    ProductCode _productCode;
    Product? _result;

    async Task Establish()
    {
        _productId = ProductId.New();
        _productCode = new ProductCode(999);
        var product = new Product
        {
            Id = _productId,
            Code = _productCode,
            Name = new ProductName("Multi Condition Test"),
            Price = 49.99m
        };
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
    }

    async Task Because() => _result = await _context.Products
        .Where(p => p.Id == _productId && p.Code == _productCode)
        .FirstOrDefaultAsync();

    [Fact] void should_find_the_product() => _result.ShouldNotBeNull();
    [Fact] void should_have_correct_code() => _result!.Code.Value.ShouldEqual(999);
    [Fact] void should_have_correct_name() => _result!.Name.Value.ShouldEqual("Multi Condition Test");
}
