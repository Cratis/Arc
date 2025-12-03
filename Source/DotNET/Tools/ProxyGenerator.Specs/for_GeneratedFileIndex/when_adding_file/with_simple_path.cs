// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_GeneratedFileIndex.when_adding_file;

public class with_simple_path : given.a_generated_file_index
{
    void Because() => _index.AddFile("folder/file.ts");

    [Fact] void should_contain_folder_entry() => _index.Entries.ContainsKey("folder").ShouldBeTrue();
    [Fact] void should_mark_folder_as_not_file() => _index.Entries["folder"].IsFile.ShouldBeFalse();
    [Fact] void should_contain_file_in_folder() => _index.Entries["folder"].Children!.ContainsKey("file.ts").ShouldBeTrue();
    [Fact] void should_mark_file_as_file() => _index.Entries["folder"].Children!["file.ts"].IsFile.ShouldBeTrue();
}
