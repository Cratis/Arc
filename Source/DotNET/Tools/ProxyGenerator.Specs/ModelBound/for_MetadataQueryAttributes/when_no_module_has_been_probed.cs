// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_MetadataQueryAttributes;

public class when_no_module_has_been_probed : given.a_metadata_discovery
{
    void Because() => _attributes.Dispose();

    [Fact] void should_not_open_any_images() => _openedPaths.ShouldBeEmpty();
}
