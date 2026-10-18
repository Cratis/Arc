// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_FileHeader.when_resolving;

public class with_a_nearer_template : given.a_project
{
    void Establish() => ConfigureProject("[*]\nfile_header_template = General\n[*.cs]\nfile_header_template = CSharp\n[*.{vb,cs,fs}]\nfile_header_template = Nearest\n[*.ts]\nfile_header_template = TypeScript\n[Models/*.cs]\nfile_header_template = Path-specific");
    void Because() => _result = FileHeader.Resolve(_project, null);

    [Fact] void should_use_the_last_supported_section_in_the_nearest_file() => _result.ShouldEqual("Nearest");
}
