// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_resolving_a_fallback_source_key
{
    [Fact]
    public void should_keep_the_provider_identifier_without_recanonicalizing_the_original_source()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var command = new Lookup(new(2026, 10, 18));
            var values = new EventSourceValuesProvider(NullLogger<EventSourceValuesProvider>.Instance).Provide(command);
            var context = new CommandContext(CorrelationId.New(), typeof(Lookup), command, [], values);
            var parameter = typeof(Lookup).GetMethod(nameof(Lookup.Handle))!.GetParameters()[0];

            var key = CommandReadModelLookup.Resolve(context, parameter, ReadProtectionMode.On, out var keyText);

            key.Value.ShouldEqual(context.GetEventSourceId().Value);
            key.Value.ShouldEqual("10/18/2026");
            keyText.ShouldEqual("2026-10-18");
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [ReadProtection(ReadProtectionMode.On)]
    public record Lookup([property: Cratis.Chronicle.Keys.Key] DateOnly Source)
    {
        public void Handle(Model model) { }
    }

    public record Model(string Id);
}
