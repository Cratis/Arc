// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_FileHeader.when_resolving;

public class with_a_root_boundary : given.a_project
{
    void Establish() => ConfigureProject("root = true\n[*.ts]\nfile_header_template = Not applicable");
    void Because() => _result = FileHeader.Resolve(_project, null);

    [Fact] void should_not_inherit_a_template_above_root() => _result.ShouldBeEmpty();
}
