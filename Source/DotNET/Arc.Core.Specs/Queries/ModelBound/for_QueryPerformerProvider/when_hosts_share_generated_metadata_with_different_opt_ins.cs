// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_QueryPerformerProvider;

public class when_hosts_share_generated_metadata_with_different_opt_ins : given.a_generated_provider_with_an_assembly_filter
{
    QueryPerformerProvider _excludedFirst;
    QueryPerformerProvider _includedSecond;
    QueryPerformerProvider _includedFirst;
    QueryPerformerProvider _excludedSecond;

    void Because()
    {
        _excludedFirst = CreateProvider(_excluded);
        _includedSecond = CreateProvider(_included);
        _registry = Substitute.For<IQueryMetadataRegistry>();
        _registry.All.Returns(new Dictionary<string, Type> { [_queryName] = typeof(PublicReadModelWithValidQuery) });
        _includedFirst = CreateProvider(_included);
        _excludedSecond = CreateProvider(_excluded);
    }

    [Fact] void should_exclude_for_the_first_host() => _excludedFirst.Performers.ShouldBeEmpty();
    [Fact] void should_include_after_an_excluding_host() => _includedSecond.TryGetPerformerFor(_queryName, out _).ShouldBeTrue();
    [Fact] void should_include_for_the_first_host() => _includedFirst.TryGetPerformerFor(_queryName, out _).ShouldBeTrue();
    [Fact] void should_exclude_after_an_including_host() => _excludedSecond.Performers.ShouldBeEmpty();
}
