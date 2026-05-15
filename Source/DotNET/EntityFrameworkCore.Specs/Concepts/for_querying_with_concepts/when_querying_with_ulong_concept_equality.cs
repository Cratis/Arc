// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_querying_with_concepts;

[Collection(nameof(ConceptAsQueryingCollection))]
public class when_querying_with_ulong_concept_equality : given.a_test_database
{
    Product _product;
    ProductSequenceNumber _targetSequence;
    Product? _result;

    async Task Establish()
    {
        _targetSequence = new ProductSequenceNumber(1UL);
        _product = new Product { Id = ProductId.New(), Code = new ProductCode(42), Name = new ProductName("Test"), ProductSequenceNumber = _targetSequence };
        await _context.Products.AddAsync(_product);
        await _context.SaveChangesAsync();
    }

    async Task Because() => _result = await _context.Products
        .Where(p => p.ProductSequenceNumber == _targetSequence)
        .FirstOrDefaultAsync();

    [Fact] void should_find_the_product() => _result.ShouldNotBeNull();
    [Fact] void should_have_correct_sequence_number() => _result!.ProductSequenceNumber.Value.ShouldEqual(1UL);
}
