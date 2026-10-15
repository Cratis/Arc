// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_the_command_key_is_missing : given.a_parameter_lookup
{
    Exception _legacy;
    Exception _off;
    Exception _measure;

    void Establish()
    {
        Configure(new Convention(new("from")));
        _context.Values.Remove(WellKnownCommandContextKeys.EventSourceId);
    }

    void Because()
    {
        var parameter = typeof(Convention).GetMethod(nameof(Convention.Provide))!.GetParameters()[0];
        _legacy = Catch.Exception(() => ReadModelServiceCollectionExtensions.ResolveReadModel(typeof(Balance), _context, _readModels));
        _off = Catch.Exception(() => CommandReadModelLookup.Resolve(_context, parameter, ReadProtectionMode.Off));
        _measure = Catch.Exception(() => CommandReadModelLookup.Resolve(_context, parameter, ReadProtectionMode.Measure));
    }

    [Fact] void should_preserve_the_legacy_off_exception() => _off.GetType().ShouldEqual(_legacy.GetType());
    [Fact] void should_preserve_the_legacy_measure_exception() => _measure.GetType().ShouldEqual(_legacy.GetType());
    [Fact] void should_keep_the_missing_source_refusal() => _legacy.ShouldBeOfExactType<MissingEventSourceIdInCommandContext>();
    [Fact] void should_not_read_a_model() => _readModels.ReceivedCalls().ShouldBeEmpty();
}
