// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator.when_naming_generated_files;

/// <summary>
/// Source-file output names a module after the C# file rather than the type, and rewrites imports to match; the
/// suffix has to survive that rewrite.
/// </summary>
public class with_the_proxy_suffix_and_source_file_output : given.a_proxy_suffix_fixture
{
    int _exitCode;

    async Task Because() => _exitCode = await RunGenerator(useProxyFileSuffix: true, useSourceFileAsOutputFile: true);

    [Fact] void should_complete() => _exitCode.ShouldEqual(0);
    [Fact] void should_name_the_combined_files_after_their_source_files_with_the_suffix() => GeneratedFileNames().ShouldContainOnly("ProxySuffixLines.proxy.ts", "ProxySuffixOrder.proxy.ts");
    [Fact] void should_import_the_renamed_module_by_its_suffixed_name() => ContentOf("ProxySuffixOrder.proxy.ts").ShouldContain("from './ProxySuffixLines.proxy'");
    [Fact] void should_leave_an_import_of_a_hand_written_module_alone() => ContentOf("ProxySuffixOrder.proxy.ts").ShouldContain($"from '{HandWrittenModule}'");
}
