// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Build;

namespace Cratis.Arc.Screenplay.Embedded.for_SdkCompilerTask;

public class when_the_sdk_compiler_is_the_same : Specification
{
    bool _isolate;

    void Because() => _isolate = SdkCompilerTask.IsNewer(new(5, 9, 0, 0), new(5, 9, 0, 0));

    [Fact] void should_keep_the_legacy_pipeline() => _isolate.ShouldBeFalse();
}
