// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A rule comparing a property against a member of an enumeration is handed the number behind the member, exactly as
/// a mapping is. A validate block reading <c>role == 6</c> against a concept declaring names is as unreadable there
/// as it is anywhere else, so the member is named here too.
/// </summary>
public class a_validator_comparing_against_a_member_of_an_enumeration : Specification
{
    const string Source = """
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using FluentValidation;

        namespace Library.Access.Inviting;

        public enum UserRole
        {
            None,
            CustomerAdvisor,
            ClientContact
        }

        [Command]
        public record InviteUser(UserRole Role, int Attempt)
        {
            public void Handle()
            {
            }
        }

        public class InviteUserValidator : CommandValidator<InviteUser>
        {
            public InviteUserValidator()
            {
                RuleFor(_ => _.Role).Equal(UserRole.ClientContact);
                RuleFor(_ => _.Attempt).GreaterThan(0);
            }
        }
        """;

    ApplicationModelAnalysis _analysis;
    IEnumerable<ValidationRuleModel> _rules;

    void Establish()
    {
        _analysis = Analyzed.Source(Source);
        _rules = _analysis.Slice().Commands.First().Validations;
    }

    ValidationRuleModel Rule(ValidationRuleKind kind) => _rules.First(_ => _.Kind == kind);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_name_the_member_the_rule_compares_against() => Rule(ValidationRuleKind.Equal).Value.ShouldEqual(new EnumValue("ClientContact"));
    [Fact] void should_leave_a_number_belonging_to_no_enumeration_as_a_number() => Rule(ValidationRuleKind.GreaterThan).Value.ShouldEqual(0);
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
