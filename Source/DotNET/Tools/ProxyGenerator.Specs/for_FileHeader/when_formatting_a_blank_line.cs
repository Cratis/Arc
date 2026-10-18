// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_FileHeader;

public class when_formatting_a_blank_line : Specification
{
    string _result = string.Empty;

    void Because() => _result = FileHeader.Format("First\n\nLast", "Example.proxy.ts");

    [Fact] void should_write_a_bare_comment_marker_without_trailing_whitespace() => _result.ShouldEqual("// First\n//\n// Last\n\n");
}
