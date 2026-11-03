// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.for_ArtifactAssemblyFilter;

public class when_checking_a_type_from_a_marked_assembly_that_is_included : given.a_marked_assembly
{
    ArcOptions _options;
    bool _serves;

    void Establish()
    {
        _options = new ArcOptions();
        _options.Artifacts.Include(_artifactType.Assembly);
    }

    void Because() => _serves = new ArtifactAssemblyFilter(Options.Create(_options)).Serves(_artifactType);

    [Fact] void should_serve_it() => _serves.ShouldBeTrue();
}
