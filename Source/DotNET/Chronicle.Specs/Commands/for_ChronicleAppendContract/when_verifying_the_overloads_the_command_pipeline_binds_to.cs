// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_ChronicleAppendContract;

/// <summary>
/// Guards the <see cref="IEventSequence"/> contract that the Arc command append pipeline binds to at compile time.
/// </summary>
/// <remarks>
/// Arc.dll bakes calls to specific <c>AppendMany</c>/<c>Append</c> overloads (see
/// <c>EventsCommandResponseValueHandler</c> and the single-event append path). Optional parameters are part of a
/// single method signature, so a trailing optional added or removed in Chronicle produces a *different* method —
/// Arc compiled against the old signature then throws <c>MissingMethodException</c> against a Chronicle that ships
/// the new one. This is exactly the regression that the Chronicle floor in <c>Directory.Packages.props</c> guards.
/// These facts fail at build time the moment the contract drifts, forcing the append call site and the Chronicle
/// floor to be reviewed together on the next Chronicle bump instead of crashing real consumers at runtime.
/// </remarks>
public class when_verifying_the_overloads_the_command_pipeline_binds_to : Specification
{
    MethodInfo? _appendMany;
    MethodInfo? _append;

    void Because()
    {
        var methods = typeof(IEventSequence).GetMethods();

        _appendMany = methods.FirstOrDefault(method =>
            method.Name == "AppendMany" &&
            method.GetParameters() is { Length: > 1 } parameters &&
            parameters[1].ParameterType == typeof(IEnumerable<object>));

        _append = methods.FirstOrDefault(method =>
            method.Name == "Append" &&
            method.GetParameters() is { Length: > 1 } parameters &&
            parameters[1].ParameterType == typeof(object));
    }

    [Fact] void should_expose_the_append_many_collection_overload() => _appendMany.ShouldNotBeNull();
    [Fact] void should_expose_the_single_append_overload() => _append.ShouldNotBeNull();

    [Fact] void should_keep_the_append_many_signature_the_pipeline_binds_to() =>
        string.Join(',', _appendMany!.GetParameters().Select(parameter => parameter.Name))
            .ShouldEqual("eventSourceId,events,eventStreamType,eventStreamId,eventSourceType,correlationId,tags,concurrencyScope,occurred,subject");

    [Fact] void should_keep_the_append_signature_the_pipeline_binds_to() =>
        string.Join(',', _append!.GetParameters().Select(parameter => parameter.Name))
            .ShouldEqual("eventSourceId,event,eventStreamType,eventStreamId,eventSourceType,correlationId,tags,concurrencyScope,occurred,subject");
}
