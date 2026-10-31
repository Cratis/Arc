// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_ForwardedIdentityCanonicalization;

public class when_framing_values : Specification
{
    string[] _frames;

    void Because() => _frames = [Identity.ForwardedIdentityCanonicalization.Frame(string.Empty), Identity.ForwardedIdentityCanonicalization.Frame("a:b"), Identity.ForwardedIdentityCanonicalization.Frame("é")];

    [Fact] void should_frame_empty_values() => _frames[0].ShouldEqual("0:\n");
    [Fact] void should_preserve_colons() => _frames[1].ShouldEqual("3:a:b\n");
    [Fact] void should_count_utf8_bytes() => _frames[2].ShouldEqual("2:é\n");
}
