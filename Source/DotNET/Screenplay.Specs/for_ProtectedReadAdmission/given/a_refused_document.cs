// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;
using Cratis.Arc.Screenplay.Model;
using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Arc.Screenplay.for_ProtectedReadAdmission.given;

public class a_refused_document : a_protected_document
{
    internal ProtectedReadAdmitted _admitted;
    internal ProtectedReadAdmission _admission;
    protected ApplicationModel _model;
    protected ScreenplayEmission _emission;
    protected ScreenplayOptions _options;
    protected DiagnosticArguments _arguments;
    protected IScreenplayVerifier _verifier;

    protected IEnumerable<ScreenplayDiagnostic> Reports => _admitted.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnprotectableCommandRead);

    void Establish()
    {
        const string Commands = """
            [Command, ReadProtection(ReadProtectionMode.On)]
            public record BookRoom(RoomId RoomId, int Guests)
            {
                public Result<RoomBooked, ValidationResult> Handle(RoomOccupancy occupancy, RoomOccupancy alternative) => new RoomBooked(Guests, occupancy.FreeBeds);
            }
            [Command, ReadProtection(ReadProtectionMode.On)]
            public record ReserveRoom(RoomId RoomId, int Guests)
            {
                public Result<RoomBooked, ValidationResult> Handle(RoomOccupancy occupancy) => new RoomBooked(Guests, occupancy.FreeBeds);
            }
            """;
        _model = Analyzed.Source(Library(Commands, Occupancy.Replace(".RemovedWith<RoomClosed>()", string.Empty, StringComparison.Ordinal))).Model;
        _options = new();
        var emitter = new ScreenplayEmitter();
        _emission = emitter.Emit(_model, _options);
        var verified = new ScreenplayVerifier().Verify(_emission.Source);
        _arguments = verified.BindingDiagnostics.First(diagnostic => diagnostic.Code == "PLAY0781").Arguments;
        _verifier = Substitute.For<IScreenplayVerifier>();
        _verifier.Verify(Arg.Any<string>()).Returns(call => new ScreenplayVerifier().VerifySyntax(call.Arg<string>()));
        _admission = new(emitter, _verifier);
    }

    protected void Refuse(params Diagnostic[] diagnostics) => _verifier.Verify(_emission.Source).Returns(
        new ScreenplayVerifier().VerifySyntax(_emission.Source) with { BindingDiagnostics = diagnostics });

    protected Diagnostic Refusal(string code, string read = "occupancy", string reason = "no-removal")
    {
        var arguments = DiagnosticArguments.Empty.With("command", _arguments["command"]);
        if (code != "PLAY0783")
        {
            arguments = arguments.With("read", DiagnosticArgument.Text(read));
        }

        arguments = code switch
        {
            "PLAY0781" => arguments.With("readModel", _arguments["readModel"]).With("reason", DiagnosticArgument.Keyword(reason)),
            "PLAY0784" or "PLAY0785" => arguments.With("path", DiagnosticArgument.Text($"{read}.freeBeds")),
            _ => arguments
        };

        return Diagnostic.Error(code, "Wording has changed; 'alternative.wrong' is not the diagnostic subject.", SourceLocation.Start, arguments);
    }

    protected void AssertOnlyOccupancyWithheld()
    {
        _admitted.Emission.Source.ShouldContain("reads RoomOccupancy as alternative");
        _admitted.Emission.Source.Split("reads RoomOccupancy as occupancy", StringSplitOptions.None).Length.ShouldEqual(2);
        _admitted.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).ShouldBeEmpty();
    }
}
