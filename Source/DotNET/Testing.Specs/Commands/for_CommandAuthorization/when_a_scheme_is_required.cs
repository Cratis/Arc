// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_a_scheme_is_required : given.an_evaluation
{
    async Task Because() => Result = await CommandAuthorization.For<given.SchemeCommand>(Provider, Principal);
    [Fact] void should_be_unsupported() => Result.ShouldBeUnsupported();
    [Fact] void should_require_a_host() => Result.ReasonCode.ShouldEqual("host-required");
    [Fact] void should_restore_the_principal() => Accessor.Current.ShouldBeNull();
}
