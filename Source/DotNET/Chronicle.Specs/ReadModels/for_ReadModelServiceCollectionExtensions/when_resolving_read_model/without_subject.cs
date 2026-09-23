// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions.when_resolving_read_model;

/// <summary>
/// The command's own subject - what <see cref="Cratis.Arc.Chronicle.Commands.CommandContextExtensions.GetSubject"/>
/// finds - has no bearing on whether the read model it resolves gets released. Most commands never declare an
/// explicit <c language="csharp">Subject</c>/<c language="csharp">[Subject]</c> and rely on the implicit
/// event-source-id fallback instead, so a command with no subject is the ordinary case, not a signal that its read
/// model carries nothing to release. Release is always attempted; <c language="csharp">IReadModels.Release</c>
/// itself is what decides whether the read model has anything to release, from the read model's own shape - not
/// the command's.
/// </summary>
public class without_subject : given.a_read_model_resolution
{
    void Establish()
    {
        GivenReadModelExists();
        GivenReadModelReleases();
    }

    void Because() => ResolveReadModel();

    [Fact] void should_return_the_released_read_model() => _result.ShouldEqual(_releasedReadModel);
    [Fact] void should_release_the_read_model() => ShouldHaveReleasedReadModel();
}
