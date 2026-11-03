// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.for_ArcOptions;

public class when_including_an_artifact_assembly : Specification
{
    ArcOptions _options;
    ArtifactsOptions _result;

    void Establish() => _options = new ArcOptions();

    void Because() => _result = _options.Artifacts.Include(typeof(when_including_an_artifact_assembly).Assembly)
        .Include(typeof(when_including_an_artifact_assembly).Assembly);

    [Fact] void should_list_the_assembly() => _options.Artifacts.IncludedAssemblies.ShouldContain(typeof(when_including_an_artifact_assembly).Assembly);
    [Fact] void should_list_it_once() => _options.Artifacts.IncludedAssemblies.Count.ShouldEqual(1);
    [Fact] void should_return_the_options_for_chaining() => _result.ShouldBeSame(_options.Artifacts);
}
