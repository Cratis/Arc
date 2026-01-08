// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_querying_with_concepts;

[Collection(nameof(ConceptAsQueryingCollection))]
public class when_querying_by_guid_concept_id : given.a_test_database
{
    ProductId _productId;
    Guid _primitiveId;
    Product _result = null!;

    async Task Establish()
    {
        _primitiveId = Guid.NewGuid();
        _productId = new ProductId(_primitiveId);

        await _context.Products.AddAsync(new Product
        {
            Id = _productId,
            Code = new ProductCode(123),
            Name = new ProductName("Test Product"),
            Price = 99.99m
        });
        await _context.SaveChangesAsync();
    }

    async Task Because() => _result = (await _context.Products.Where(p => p.Id == _productId).FirstOrDefaultAsync())!;

    [Fact] void should_find_the_product() => _result.ShouldNotBeNull();
    [Fact] void should_have_correct_id() => _result.Id.Value.ShouldEqual(_primitiveId);
    [Fact] void should_have_correct_name() => _result.Name.Value.ShouldEqual("Test Product");
}
