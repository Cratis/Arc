// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// An application speaking more than one language declares every message in a resource and names it from the
/// validator. What the validator names is a property a build writes from that resource, whose body is a lookup
/// resolved against the culture of the caller - so the compiler holds no value for it, and every message written that
/// way came back indistinguishable from one assembled while the request runs. A localized application therefore
/// stated no message anywhere, which is the whole of what it says about what it will not accept.
/// <para>
/// The key is what is stated rather than the text, because the text is one of several the application holds and
/// picking one would describe a language rather than an application. It is qualified by the resource declaring it,
/// since a key is unique to that resource and to nothing wider.
/// </para>
/// </summary>
public class a_validator_taking_its_messages_from_a_resource : Specification
{
    const string Source = """
        using System.Globalization;
        using System.Resources;
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using FluentValidation;

        namespace Library.Authors.Registration;

        internal class AuthorMessages
        {
            static ResourceManager resourceMan;
            static CultureInfo resourceCulture;

            internal static ResourceManager ResourceManager
            {
                get
                {
                    if (object.ReferenceEquals(resourceMan, null))
                    {
                        resourceMan = new ResourceManager("Library.AuthorMessages", typeof(AuthorMessages).Assembly);
                    }
                    return resourceMan;
                }
            }

            internal static string NameRequired
            {
                get
                {
                    return ResourceManager.GetString("NameRequired", resourceCulture);
                }
            }

            internal static string Nickname_Required
            {
                get
                {
                    return ResourceManager.GetString("Nickname-Required", resourceCulture);
                }
            }
        }

        internal class PenMessages
        {
            static ResourceManager resourceMan;
            static CultureInfo resourceCulture;

            internal static ResourceManager ResourceManager
            {
                get
                {
                    if (object.ReferenceEquals(resourceMan, null))
                    {
                        resourceMan = new ResourceManager("Library.PenMessages", typeof(PenMessages).Assembly);
                    }
                    return resourceMan;
                }
            }

            internal static string NameRequired
            {
                get
                {
                    return ResourceManager.GetString("NameRequired", resourceCulture);
                }
            }
        }

        public static class AuthorConstants
        {
            public const string AliasRequired = "An author must have an alias";
        }

        [Command]
        public record RegisterAuthor(string Name, string Pen, string Email, string Nickname, string Alias)
        {
            public void Handle()
            {
            }
        }

        public class RegisterAuthorValidator : CommandValidator<RegisterAuthor>
        {
            public RegisterAuthorValidator()
            {
                RuleFor(_ => _.Name).NotEmpty().WithMessage(_ => AuthorMessages.NameRequired);
                RuleFor(_ => _.Pen).NotEmpty().WithMessage(_ => PenMessages.NameRequired);
                RuleFor(_ => _.Email).NotEmpty().WithMessage(_ => string.Format(AuthorMessages.NameRequired, "an email address"));
                RuleFor(_ => _.Nickname).NotEmpty().WithMessage(_ => AuthorMessages.Nickname_Required);
                RuleFor(_ => _.Alias).NotEmpty().WithMessage(_ => AuthorConstants.AliasRequired);
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

    ValidationRuleModel Rule(string property) => _rules.First(_ => _.Property == property);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn((Analyzed.SlicePath, Source)).ShouldBeEmpty();
    [Fact] void should_state_every_rule_a_message_was_written_after() => _rules.Select(_ => _.Property).ShouldContainOnly(["Name", "Pen", "Email", "Nickname", "Alias"]);
    [Fact] void should_state_the_key_a_message_is_looked_up_by() => Rule("Name").Message.ShouldEqual("$strings.AuthorMessages.NameRequired");
    [Fact] void should_qualify_the_key_by_the_resource_declaring_it() => Rule("Pen").Message.ShouldEqual("$strings.PenMessages.NameRequired");
    [Fact] void should_leave_a_message_put_together_from_a_resource_off_its_rule() => Rule("Email").Message.ShouldBeNull();
    [Fact] void should_leave_a_key_it_has_no_way_of_writing_off_its_rule() => Rule("Nickname").Message.ShouldBeNull();
    [Fact] void should_still_recover_a_message_a_constant_holds() => Rule("Alias").Message.ShouldEqual("An author must have an alias");
    [Fact] void should_report_only_the_messages_it_could_not_write_down() => _analysis.Diagnostics.Select(_ => _.Code).Distinct(StringComparer.Ordinal).ShouldContainOnly([ScreenplayDiagnosticCodes.UnmappableValidationRule]);
    [Fact] void should_report_one_for_each_of_them() => _analysis.Diagnostics.Count.ShouldEqual(2);
    [Fact] void should_say_which_message_it_was() => _analysis.Diagnostics.Any(_ => _.Message.Contains("AuthorMessages.Nickname_Required", StringComparison.Ordinal)).ShouldBeTrue();
}
