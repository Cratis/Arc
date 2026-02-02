// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Templates.for_ImportStatementExtensions.when_ordering_imports;

public class with_different_package_modules : Specification
{
    List<ImportStatement> _imports;
    IOrderedEnumerable<ImportStatement> _result;

    void Establish() => _imports =
    [
        new ImportStatement(typeof(string), "TypeZ", "zlib"),
        new ImportStatement(typeof(string), "TypeA", "alib"),
        new ImportStatement(typeof(string), "TypeM", "mlib")
    ];

    void Because() => _result = _imports.ToOrderedImports();

    [Fact] void should_order_by_module_name_alphabetically() => _result.Select(_ => _.Module).ShouldContainOnly("alib", "mlib", "zlib");
    [Fact] void should_have_alib_first() => _result.First().Module.ShouldEqual("alib");
    [Fact] void should_have_zlib_last() => _result.Last().Module.ShouldEqual("zlib");
}
