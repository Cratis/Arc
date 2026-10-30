// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_command_dialog_navigating_after_success : given.a_screen_using_components
{
    void Establish() => _component = Imports + """

        export const AuthorRetirement = () => <CommandDialog command={RetireAuthor} onSuccess={() => navigate('/authors')} />;
        """;

    void Because() => Generate();

    [Fact] void should_compile_the_document() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_have_no_document_warnings_or_errors() => DocumentIsClean.ShouldBeTrue();
    [Fact] void should_reprint_identically() => _reprinted.ShouldEqual(_result.Source);
    [Fact] void should_not_invent_navigation() => Says("navigate to").ShouldBeFalse();

    [Fact] void should_keep_the_dialog_action() => Says("action RetireAuthor").ShouldBeTrue();
    [Fact] void should_state_the_screen_subject_of_the_action() => Says("action RetireAuthor for screen").ShouldBeTrue();
    [Fact] void should_report_the_post_success_callback() => Reports("SP0028", "after success").ShouldBeTrue();
    [Fact] void should_report_only_the_post_success_callback() => Count("SP0028").ShouldEqual(1);
}
