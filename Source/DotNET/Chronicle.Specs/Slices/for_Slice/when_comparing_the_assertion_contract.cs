// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Chronicle.Commands.for_CommandScenario;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Chronicle.Testing.Slices;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Chronicle.Slices.for_Slice;

public class when_comparing_the_assertion_contract : Specification
{
    MethodInfo[] _extensions;
    string[] _missingForwarders;

    void Establish()
    {
        _extensions = [
            .. typeof(CommandResultShouldExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.Name == "ShouldBeSuccessful" || method.Name == "ShouldBeRejected" || method.Name == "ShouldBeDenied"),
            .. typeof(CommandScenarioAppendedEventsAssertionExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static),
            .. typeof(CommandScenarioChronicleAssertionExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.Name == "ShouldHaveAppendedEvent")
        ];
    }

    void Because()
    {
        var forwarders = typeof(SliceOutcome<SettleLedger>).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        _missingForwarders = _extensions
            .Where(extension => !forwarders.Any(forwarder => Matches(extension, forwarder)))
            .Select(extension => extension.ToString()!)
            .ToArray();
    }

    [Fact] void should_find_the_underlying_assertion_extensions() => _extensions.ShouldNotBeEmpty();
    [Fact] void should_mirror_every_assertion_signature() => _missingForwarders.ShouldBeEmpty();

    static bool Matches(MethodInfo extension, MethodInfo forwarder)
    {
        var commandGenericArgument = extension.GetParameters()[0].ParameterType.IsGenericType ? 1 : 0;
        if (extension.Name != forwarder.Name || extension.GetGenericArguments().Length - commandGenericArgument != forwarder.GetGenericArguments().Length)
        {
            return false;
        }

        var expected = Close(extension, true);
        var actual = Close(forwarder, false);
        var expectedParameters = expected.GetParameters().Skip(1).ToArray();
        var actualParameters = actual.GetParameters();
        return expected.ReturnType == actual.ReturnType &&
            expectedParameters.Select(parameter => parameter.ParameterType).SequenceEqual(actualParameters.Select(parameter => parameter.ParameterType)) &&
            expectedParameters.Select(parameter => parameter.IsDefined(typeof(ParamArrayAttribute))).SequenceEqual(actualParameters.Select(parameter => parameter.IsDefined(typeof(ParamArrayAttribute))));
    }

    static MethodInfo Close(MethodInfo method, bool extension) => method.IsGenericMethodDefinition
        ? method.MakeGenericMethod(method.GetGenericArguments().Select((_, index) => extension && index == 0 ? typeof(SettleLedger) : typeof(LedgerSettled)).ToArray())
        : method;
}
