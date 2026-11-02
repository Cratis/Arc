// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Model;
using Cratis.Arc.Screenplay.Verification;

namespace Cratis.Arc.Screenplay.for_UnboundProductionAdmission.given;

public class an_unbound_command : Specification
{
    protected ApplicationModel Model;
    protected ScreenplayOptions Options;
    protected ScreenplayEmission Candidate;
    internal ProtectedReadAdmitted Admitted;
    protected IScreenplayVerifier Verifier;
    protected ScreenplayEmitter Emitter;

    void Establish()
    {
        Model = Analyzed.Source((Analyzed.SlicePath, """
            using System;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Chronicle.Events;
            namespace Library.Authors.Registration;
            public enum Role { Reader, Administrator }
            [Command] public record Register(Guid Id, Role Role)
            {
                public Registered? Handle() => Role == Role.Reader ? new Registered(Role) : null;
            }
            [Command] public record Keep(Guid Id, Role Role)
            {
                public Kept Handle() => new(Role);
            }
            [EventType] public record Registered(Role Role);
            [EventType] public record Kept(Role Role);
            """)).Model;
        Model = Model with
        {
            Slices = Model.Slices.Select(slice => slice with
            {
                Commands = slice.Commands.Select(command => command.Name == "Register" ? command with
                {
                    Produces = command.Produces.Select(production => production with { When = new ComparisonCondition("Role", ComparisonKind.Equal, new LiteralSource(2)) }).ToList()
                } : command).ToList()
            }).ToList()
        };
        Options = new ScreenplayOptions().WithDefaults(Analyzed.AssemblyName);
        Emitter = new();
        Candidate = Emitter.Emit(Model, Options);
        Verifier = new ScreenplayVerifier();
    }

    protected void Admit() => Admitted = new UnboundProductionAdmission(Emitter, Verifier).Admit(Model, Options, Candidate);
}
