// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_GeneratedFileIndex.when_adding_file;

public class with_backslash_separators : given.a_generated_file_index
{
    void Because() => _index.AddFile("folder\\subfolder\\file.ts");

    [Fact] void should_contain_folder_entry() => _index.Entries.ContainsKey("folder").ShouldBeTrue();
    [Fact] void should_contain_subfolder_in_folder() => _index.Entries["folder"].Children!.ContainsKey("subfolder").ShouldBeTrue();
    [Fact] void should_contain_file_in_subfolder() => _index.Entries["folder"].Children!["subfolder"].Children!.ContainsKey("file.ts").ShouldBeTrue();
}
