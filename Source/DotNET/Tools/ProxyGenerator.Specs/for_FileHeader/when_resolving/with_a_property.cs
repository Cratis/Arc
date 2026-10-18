// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_FileHeader.when_resolving;

public class with_a_property : given.a_project
{
    void Because() => _result = FileHeader.Resolve(_project, "Copyright (c) Application");

    [Fact] void should_prefer_the_property_to_editorconfig() => _result.ShouldEqual("Copyright (c) Application");
}
