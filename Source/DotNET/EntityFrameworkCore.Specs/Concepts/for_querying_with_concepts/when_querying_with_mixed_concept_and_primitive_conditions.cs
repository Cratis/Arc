// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_querying_with_concepts;

[Collection(nameof(ConceptAsQueryingCollection))]
public class when_querying_with_mixed_concept_and_primitive_conditions : given.a_test_database
{
    ProductId _productId;
    int _codeValue;
    Product? _result;

    async Task Establish()
    {
        _productId = ProductId.New();
        _codeValue = 555;
        var product = new Product
        {
            Id = _productId,
            Code = new ProductCode(_codeValue),
            Name = new ProductName("Mixed Condition Test"),
            Price = 59.99m
        };
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
    }

    async Task Because() => _result = await _context.Products
        .Where(p => p.Id == _productId && p.Code == new ProductCode(_codeValue))
        .FirstOrDefaultAsync();

    [Fact] void should_find_the_product() => _result.ShouldNotBeNull();
    [Fact] void should_have_correct_code() => _result!.Code.Value.ShouldEqual(555);
    [Fact] void should_have_correct_name() => _result!.Name.Value.ShouldEqual("Mixed Condition Test");
}
