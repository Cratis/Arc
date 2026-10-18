// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_FileHeader;

public class when_formatting_newlines : Specification
{
    string[] _results = [];

    void Because() => _results = new[] { "\n", "\r\n", "\r", "\\n", "\\r\\n" }
        .Select(newline => FileHeader.Format($"First{newline}{{fileName}}{newline}Last", "Example.proxy.ts")).ToArray();

    [Fact] void should_normalize_windows_unix_and_escaped_newlines() => _results.ShouldEachConformTo(result => result == "// First\n// Example.proxy.ts\n// Last\n\n");
}
