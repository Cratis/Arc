// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A value of an enumeration that no member is declared with is a cast or several flags combined into one. There is
/// no name to write for it, and inventing one would describe a value the application does not have - so the number
/// stands as it is and the reader is told which value went unnamed rather than being left to wonder.
/// </summary>
public class a_handler_producing_a_value_no_member_is_declared_with : Specification
{
    const string Source = """
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Access.Granting;

        [Flags]
        public enum Access
        {
            None = 0,
            Read = 1,
            Write = 2
        }

        [EventType]
        public record AccessGranted(Access Access);

        [Command]
        public record GrantAccess(string Subject)
        {
            public object Handle() => new AccessGranted(Access.Read | Access.Write);
        }
        """;

    ApplicationModelAnalysis _analysis;
    ProducesModel _produced;

    void Establish()
    {
        _analysis = Analyzed.Source(Source);
        _produced = _analysis.Slice().Commands.First().Produces.First();
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_fall_back_to_the_number_behind_the_value() => _produced.Mappings.Single().Source.ShouldEqual(new LiteralSource(3));
    [Fact] void should_report_the_value_it_could_not_name() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.UnnamedEnumerationValue]);
    [Fact] void should_say_which_enumeration_it_was() => _analysis.Diagnostics.Single().Message.Contains("'Access'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_say_which_value_went_unnamed() => _analysis.Diagnostics.Single().Message.Contains("'3'", StringComparison.Ordinal).ShouldBeTrue();
}
