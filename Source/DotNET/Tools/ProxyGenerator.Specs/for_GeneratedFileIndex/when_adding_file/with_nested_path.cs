// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_GeneratedFileIndex.when_adding_file;

public class with_nested_path : given.a_generated_file_index
{
    void Because() => _index.AddFile("level1/level2/level3/file.ts");

    [Fact] void should_contain_level1_entry() => _index.Entries.ContainsKey("level1").ShouldBeTrue();
    [Fact] void should_contain_level2_in_level1() => _index.Entries["level1"].Folders!.ContainsKey("level2").ShouldBeTrue();
    [Fact] void should_contain_level3_in_level2() => _index.Entries["level1"].Folders!["level2"].Folders!.ContainsKey("level3").ShouldBeTrue();
    [Fact] void should_contain_file_in_level3() => _index.Entries["level1"].Folders!["level2"].Folders!["level3"].Files!.ShouldContain("file.ts");
}
