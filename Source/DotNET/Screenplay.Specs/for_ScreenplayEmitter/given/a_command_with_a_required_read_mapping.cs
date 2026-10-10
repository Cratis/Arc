// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Arc.Screenplay.Verification;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.given;

public class a_command_with_a_required_read_mapping : a_routed_model
{
    protected ApplicationModel _application;

    void Establish()
    {
        const string Namespace = "Library.Authors.Registration";
        PropertyModel[] properties = [new("Name", Text), new("Lines", new TypeReferenceModel("Undeclared", false, true))];
        _command = _command with
        {
            Authoring = _command.Authoring! with
            {
                Reads = [new("Report", "report", "Id") { Namespace = Namespace, Properties = properties }]
            },
            Produces = [new("Registered", null, [new("Name", new PropertyPathSource("Name")), new("PreviousName", new PropertyPathSource("report.Name"))]) { UsesCommandContext = true, CanInline = true }]
        };
        var model = Model();
        _application = model with
        {
            Slices = [model.Slices.Single() with
            {
                ReadModels = [new("Report", properties)],
                Events = [new("Registered", [new("Name", Text), new("PreviousName", Text)], [])],
                Projections = [new("Reports", "Report", "Log", ProjectionAutoMapMode.Enabled, false, ProjectionScopeModel.Empty)],
                Specifications = [new("a_successful_registration", [], new("Register", SpecificationStateKind.Command, []), [new("Registered", SpecificationStateKind.Event, [])], [])]
            }]
        };
    }

    protected void AssertNoUnexpectedBindingErrors(bool authoring = false) => new ScreenplayVerifier().Verify(_result.Source).UnexpectedBindingErrors(authoring).ShouldBeEmpty();
}
