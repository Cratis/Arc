// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_FileHeader.when_resolving;

public class with_a_multiline_property : given.a_project
{
    void Because() => _result = FileHeader.Format(FileHeader.Resolve(_project, "Application\\nAll rights reserved."), "Sample.ts");

    [Fact] void should_comment_each_property_line() => _result.ShouldEqual("// Application\n// All rights reserved.\n\n");
}
