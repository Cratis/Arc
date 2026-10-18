// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_FileHeader.when_resolving;

public class with_an_empty_property : given.a_project
{
    void Because() => _result = FileHeader.Resolve(_project, string.Empty);

    [Fact] void should_fall_back_to_the_parent_editorconfig() => _result.ShouldEqual("Parent header");
}
