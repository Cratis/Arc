// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.ModelBound.for_CommandHandlerProvider;

public class when_hosts_share_a_universe_with_different_opt_ins : Specification
{
    ITypes _types;
    ArtifactAssemblyFilter _excluded;
    ArtifactAssemblyFilter _included;
    CommandHandlerProvider _excludedFirst;
    CommandHandlerProvider _includedSecond;
    CommandHandlerProvider _includedFirst;
    CommandHandlerProvider _excludedSecond;

    void Establish()
    {
        _types = Substitute.For<ITypes>();
        _types.All.Returns([typeof(InternalCommand)]);
        _excluded = new ArtifactAssemblyFilter([], _ => true);
        _included = new ArtifactAssemblyFilter([typeof(InternalCommand).Assembly], _ => true);
    }

    void Because()
    {
        _excludedFirst = new CommandHandlerProvider(_types, _excluded);
        _includedSecond = new CommandHandlerProvider(_types, _included);
        var otherUniverse = Substitute.For<ITypes>();
        otherUniverse.All.Returns([typeof(InternalCommand)]);
        _includedFirst = new CommandHandlerProvider(otherUniverse, _included);
        _excludedSecond = new CommandHandlerProvider(otherUniverse, _excluded);
    }

    [Fact] void should_exclude_for_the_first_host() => _excludedFirst.Handlers.ShouldBeEmpty();
    [Fact] void should_include_after_an_excluding_host() => _includedSecond.Handlers.Count().ShouldEqual(1);
    [Fact] void should_include_for_the_first_host() => _includedFirst.Handlers.Count().ShouldEqual(1);
    [Fact] void should_exclude_after_an_including_host() => _excludedSecond.Handlers.ShouldBeEmpty();
}
