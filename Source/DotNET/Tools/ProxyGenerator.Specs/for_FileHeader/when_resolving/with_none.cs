// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_FileHeader.when_resolving;

public class with_none : given.a_project
{
    void Because() => _result = FileHeader.Resolve(_project, "NoNe");

    [Fact] void should_disable_the_editorconfig_header() => _result.ShouldBeEmpty();
}
