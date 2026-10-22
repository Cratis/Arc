// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_MetadataQueryAttributes;

public class when_starting_a_new_run_after_the_module_changes : given.a_replaceable_module
{
    bool _first;
    bool _second;

    void Establish()
    {
        WriteModule(marked: true);
        using var context = CreateContext();
        using var attributes = new MetadataQueryAttributes();
        var owner = context.LoadFromAssemblyPath(_path).GetType("Owner")!;
        _first = attributes.Find(owner, typeof(ReadModelAttribute).FullName!) is not null;
    }

    void Because()
    {
        WriteModule(marked: false);
        using var context = CreateContext();
        using var attributes = new MetadataQueryAttributes();
        var owner = context.LoadFromAssemblyPath(_path).GetType("Owner")!;
        _second = attributes.Find(owner, typeof(ReadModelAttribute).FullName!) is not null;
    }

    [Fact] void should_find_the_marker_in_the_original_run() => _first.ShouldBeTrue();
    [Fact] void should_read_the_replaced_module_in_the_new_run() => _second.ShouldBeFalse();
}
