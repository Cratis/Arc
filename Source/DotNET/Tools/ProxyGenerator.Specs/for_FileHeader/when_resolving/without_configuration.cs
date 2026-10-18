// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_FileHeader.when_resolving;

public class without_configuration : given.a_project
{
    void Establish() => File.WriteAllText(Path.Combine(_root, ".editorconfig"), "root = true");
    void Because() => _result = FileHeader.Resolve(_project, null);

    [Fact] void should_emit_no_header() => _result.ShouldBeEmpty();
}
