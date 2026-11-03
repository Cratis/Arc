// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Authorization.for_AuthorizationCommandIdentity.when_reading_the_caller;

public class and_no_frame_exists : Specification
{
    bool _success;
    ReadModelKeyRefusal _refusal;

    void Because() => _success = AuthorizationCommandIdentity.TryGetCaller(null, out _, out _refusal);

    [Fact] void should_refuse_the_caller() => _success.ShouldBeFalse();
    [Fact] void should_report_no_caller() => _refusal.ShouldEqual(ReadModelKeyRefusal.NoCaller);
}
