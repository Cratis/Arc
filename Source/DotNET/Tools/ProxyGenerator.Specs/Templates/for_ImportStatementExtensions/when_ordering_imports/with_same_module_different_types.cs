// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Templates.for_ImportStatementExtensions.when_ordering_imports;

public class with_same_module_different_types : Specification
{
    List<ImportStatement> _imports;
    IOrderedEnumerable<ImportStatement> _result;

    void Establish() => _imports =
    [
        new ImportStatement(typeof(string), "TypeZ", "@cratis/fundamentals"),
        new ImportStatement(typeof(string), "TypeA", "@cratis/fundamentals"),
        new ImportStatement(typeof(string), "TypeM", "@cratis/fundamentals")
    ];

    void Because() => _result = _imports.ToOrderedImports();

    [Fact] void should_order_by_type_name_alphabetically() => _result.Select(_ => _.Type).ShouldContainOnly("TypeA", "TypeM", "TypeZ");
    [Fact] void should_have_type_a_first() => _result.First().Type.ShouldEqual("TypeA");
    [Fact] void should_have_type_z_last() => _result.Last().Type.ShouldEqual("TypeZ");
}
