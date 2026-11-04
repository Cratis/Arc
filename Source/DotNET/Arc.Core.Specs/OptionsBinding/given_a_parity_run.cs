// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The base of every parity spec: the reflection-free binder must leave the same options and raise the same failure as the
/// reflection binder for the same configuration.
/// </summary>
/// <remarks>
/// <see cref="ArcOptions"/> reads the process-wide derived types, and specs that configure Arc write them, so both instances must
/// see the same value: the parity specs run in the non-parallel <c>MutatesTypeDiscovery</c> collection.
/// </remarks>
[Collection("MutatesTypeDiscovery")]
public abstract class given_a_parity_run : Specification
{
    protected ParityRun _run;

    protected abstract IDictionary<string, string?> Configuration { get; }

    protected virtual Action<ArcOptions>? Prepare => null;

    void Because() => _run = new ParityRun(Configuration, Prepare);

    [Fact]
    public void should_leave_the_same_options() => _run.ReflectionFreeSnapshot.ShouldEqual(_run.ReflectionSnapshot);

    [Fact]
    public void should_raise_the_same_failure() => _run.ReflectionFreeFailure.ShouldEqual(_run.ReflectionFailure);
}
