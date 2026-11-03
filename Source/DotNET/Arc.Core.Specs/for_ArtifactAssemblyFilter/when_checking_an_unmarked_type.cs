// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.for_ArtifactAssemblyFilter;

public class when_checking_an_unmarked_type : Specification
{
    bool _serves;

    void Because() => _serves = new ArtifactAssemblyFilter(Options.Create(new ArcOptions())).Serves(typeof(when_checking_an_unmarked_type));

    [Fact] void should_serve_it() => _serves.ShouldBeTrue();
}
