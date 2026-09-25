// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator.when_naming_generated_files;

/// <summary>
/// Turning the option on in a project that already has generated files must not leave the old names behind.
/// </summary>
public class when_switching_the_proxy_suffix_on : given.a_proxy_suffix_fixture
{
    string[] _before = null!;
    string _indexBefore = null!;
    int _exitCode;

    async Task Because()
    {
        await RunGenerator(useProxyFileSuffix: false);
        _before = GeneratedFileNames();
        _indexBefore = ContentOf("index.ts");
        _exitCode = await RunGenerator(useProxyFileSuffix: true);
    }

    [Fact] void should_complete() => _exitCode.ShouldEqual(0);
    [Fact] void should_name_files_after_their_types_without_the_option() => _before.ShouldContainOnly("ProxySuffixLine.ts", "ProxySuffixOrder.ts");
    [Fact] void should_export_the_unsuffixed_files_without_the_option() => _indexBefore.ShouldContain("export * from './ProxySuffixOrder';");
    [Fact] void should_remove_the_previously_generated_names() => GeneratedFileNames().ShouldContainOnly("ProxySuffixLine.proxy.ts", "ProxySuffixOrder.proxy.ts");
    [Fact] void should_export_only_the_suffixed_names() => ContentOf("index.ts").ShouldNotContain("from './ProxySuffixOrder';");
}
