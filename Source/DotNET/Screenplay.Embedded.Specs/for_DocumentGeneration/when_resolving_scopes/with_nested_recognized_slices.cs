// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Generation;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_resolving_scopes;

public class with_nested_recognized_slices : Specification
{
    IReadOnlyList<DocumentScope> _scopes;

    void Because()
    {
        var model = ApplicationModel.Empty with
        {
            Slices =
            [
                SliceModel.Empty("App.Accounts.SignIn", "SignIn", SliceKind.StateChange),
                SliceModel.Empty("App.Accounts.SignIn.Mfa", "Mfa", SliceKind.StateChange)
            ]
        };
        _scopes = DocumentScopes.Of(model, new("App", "App"));
    }

    [Fact] void should_keep_the_group_as_a_rooted_feature() => _scopes.Single(_ => _.Id == "App.Accounts").Kind.ShouldEqual(EmbeddedDocumentKind.Feature);
    [Fact] void should_not_invent_a_feature_out_of_a_recognized_slice() => _scopes.Any(_ => _.Id == "App.Accounts.SignIn").ShouldBeFalse();
    [Fact] void should_not_invent_a_module_when_only_slice_namespaces_are_beneath_the_group() => _scopes.Any(_ => _.Kind == EmbeddedDocumentKind.Module).ShouldBeFalse();
}
