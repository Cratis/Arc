// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_querying_with_concepts;

[Collection(nameof(ConceptAsQueryingCollection))]
public class when_querying_with_int_concept_range_comparison : given.a_test_database
{
    Product _first;
    Product _second;
    Product _third;
    ProductCode _minCode;
    IEnumerable<Product> _result = [];

    async Task Establish()
    {
        _minCode = new ProductCode(200);
        _first = new Product { Id = ProductId.New(), Code = new ProductCode(100), Name = new ProductName("First") };
        _second = new Product { Id = ProductId.New(), Code = new ProductCode(200), Name = new ProductName("Second") };
        _third = new Product { Id = ProductId.New(), Code = new ProductCode(300), Name = new ProductName("Third") };
        await _context.Products.AddRangeAsync(_first, _second, _third);
        await _context.SaveChangesAsync();
    }

    async Task Because() => _result = await _context.Products
        .Where(p => p.Code >= _minCode)
        .ToListAsync();

    [Fact] void should_return_two_products() => _result.Count().ShouldEqual(2);
    [Fact] void should_not_include_the_first_product() => _result.ShouldNotContain(p => p.Code == _first.Code);
    [Fact] void should_include_the_second_product() => _result.ShouldContain(p => p.Code == _second.Code);
    [Fact] void should_include_the_third_product() => _result.ShouldContain(p => p.Code == _third.Code);
}
