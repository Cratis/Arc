// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator.when_naming_generated_files;

public class with_the_proxy_suffix : given.a_proxy_suffix_fixture
{
    int _exitCode;

    async Task Because() => _exitCode = await RunGenerator(useProxyFileSuffix: true);

    [Fact] void should_complete() => _exitCode.ShouldEqual(0);
    [Fact] void should_name_every_generated_file_with_the_suffix() => GeneratedFileNames().ShouldContainOnly("ProxySuffixLine.proxy.ts", "ProxySuffixOrder.proxy.ts");
    [Fact] void should_import_another_generated_file_by_its_suffixed_name() => ContentOf("ProxySuffixOrder.proxy.ts").ShouldContain("from './ProxySuffixLine.proxy'");
    [Fact] void should_leave_an_import_of_a_hand_written_module_alone() => ContentOf("ProxySuffixOrder.proxy.ts").ShouldContain($"from '{HandWrittenModule}'");
    [Fact] void should_export_the_suffixed_files_from_the_index() => ContentOf("index.ts").ShouldContain("export * from './ProxySuffixOrder.proxy';");
}
