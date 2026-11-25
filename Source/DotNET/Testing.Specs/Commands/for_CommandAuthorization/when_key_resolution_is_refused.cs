// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_key_resolution_is_refused : given.an_evaluation
{
    void Establish() => Behavior.Evaluate = (_, _) => throw new TargetInvocationException(new ReadModelKeyResolutionDenied(ReadModelKeyRefusal.NoCaller));
    async Task Because() => Result = await CommandAuthorization.For(Provider, Principal, new given.ResourceCommand(true));
    [Fact] void should_be_unsupported() => Result.ShouldBeUnsupported();
    [Fact] void should_identify_key_resolution() => Result.ReasonCode.ShouldEqual("read-model-key-resolution");
    [Fact] void should_restore_the_principal() => Accessor.Current.ShouldBeNull();
}
