// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using VerifiedCallerConsumer;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerConsumer;

public class when_inspecting_the_public_boundary : Specification
{
    Type[] _trustedTypes;
    PropertyInfo[] _properties;
    ConstructorInfo[] _constructors;

    void Because()
    {
        var roots = new[] { typeof(VerifiedCaller), typeof(CallerResolution) };
        _properties = roots.SelectMany(type => type.GetProperties()).ToArray();
        _constructors = roots.SelectMany(type => type.GetConstructors()).ToArray();
        _trustedTypes = [typeof(IVerifiedCredentialSource), typeof(VerifiedCredentialFacts), typeof(VerifiedCallerBuilder), typeof(VerifiedCallerResolver), typeof(CredentialVerification), typeof(VerifiedCallerPrincipal), typeof(VerifiedCallerIdentity)];
    }

    [Fact] void should_have_no_public_trusted_constructors() => _constructors.ShouldBeEmpty();
    [Fact] void should_allow_reading_all_fields() => _properties.All(property => property.GetMethod!.IsPublic).ShouldBeTrue();
    [Fact] void should_have_no_public_setters_or_initializers() => _properties.Any(property => property.SetMethod?.IsPublic == true).ShouldBeFalse();
    [Fact] void should_keep_the_authority_graph_internal() => _trustedTypes.Any(type => type.IsVisible).ShouldBeFalse();
    [Fact] void should_use_a_real_non_friend_assembly() => typeof(VerifiedCaller).Assembly.GetCustomAttributes<InternalsVisibleToAttribute>().Any(attribute => attribute.AssemblyName.StartsWith(typeof(ToolHandler).Assembly.GetName().Name!, StringComparison.Ordinal)).ShouldBeFalse();
}
