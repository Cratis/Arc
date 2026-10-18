// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_FileHeader.when_resolving;

public class with_an_empty_template : given.a_project
{
    void Establish() => ConfigureProject("[*.cs]\nfile_header_template = ");
    void Because() => _result = FileHeader.Resolve(_project, null);

    [Fact] void should_suppress_the_inherited_template() => _result.ShouldBeEmpty();
}
