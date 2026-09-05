// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ArcSpecificationFactAdapter;
using Cratis.Screenplay.Generation.DotNet;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_preserving_legacy_placement_options : Specification
{
    const string Accounts = """
        using Cratis.Arc.Commands.ModelBound;

        namespace Accounts.Customers.Register;

        [Command]
        public record RegisterCustomer(string Name)
        {
            public void Handle()
            {
            }
        }
        """;

    const string Shipping = """
        using Cratis.Arc.Commands.ModelBound;

        namespace Shipping.Parcels.Dispatch;

        [Command]
        public record DispatchParcel(string TrackingNumber)
        {
            public void Handle()
            {
            }
        }
        """;

    ScreenplayGenerationResult _legacyConfigured = null!;
    ScreenplayGenerationResult _projectAwareConfigured = null!;
    ScreenplayGenerationResult _legacyNamespaceRoots = null!;
    ScreenplayGenerationResult _projectAwareNamespaceRoots = null!;

    void Because()
    {
        var generator = new ScreenplayGenerator();
        var accounts = Analyzed.Project(
            "Accounts",
            [],
            ("Source/Accounts/Customers/Register/RegisterCustomer.cs", Accounts));
        var shipping = Analyzed.Project(
            "Shipping",
            [],
            ("Source/Shipping/Parcels/Dispatch/DispatchParcel.cs", Shipping));
        var accountsProject = SourceProjects.Create("Accounts", DotNetProjectRole.Application, accounts);
        var shippingProject = SourceProjects.Create("Shipping", DotNetProjectRole.Application, shipping);
        var configured = new ScreenplayOptions
        {
            Domain = "Commerce",
            Module = "Commerce",
            SegmentsToSkip = 1
        };

        _legacyConfigured = generator.Generate(accounts, configured);
        _projectAwareConfigured = generator.Generate(accountsProject, configured);
        _legacyNamespaceRoots = generator.Generate([accounts, shipping], new ScreenplayOptions());
        _projectAwareNamespaceRoots = generator.Generate([shippingProject, accountsProject], new ScreenplayOptions());
    }

    [Fact] void should_preserve_configured_module_and_segment_output_bytes() => _projectAwareConfigured.Source.ShouldEqual(_legacyConfigured.Source);
    [Fact] void should_preserve_configured_diagnostics() => Diagnostics(_projectAwareConfigured).ShouldEqual(Diagnostics(_legacyConfigured));
    [Fact] void should_preserve_namespace_root_module_output_bytes() => _projectAwareNamespaceRoots.Source.ShouldEqual(_legacyNamespaceRoots.Source);
    [Fact] void should_preserve_namespace_root_diagnostics() => Diagnostics(_projectAwareNamespaceRoots).ShouldEqual(Diagnostics(_legacyNamespaceRoots));

    static string Diagnostics(ScreenplayGenerationResult result) => string.Join(
        '|',
        result.Diagnostics.Select(_ => $"{_.Code}:{_.Severity}:{_.Location}:{_.Message}"));
}
