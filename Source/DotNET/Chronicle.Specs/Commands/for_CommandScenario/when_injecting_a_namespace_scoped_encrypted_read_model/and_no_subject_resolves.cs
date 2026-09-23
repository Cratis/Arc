// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_injecting_a_namespace_scoped_encrypted_read_model;

/// <summary>
/// The command carries no <c language="csharp">Subject</c>/<c language="csharp">[Subject]</c>, relying on the
/// implicit event-source-id fallback like most commands do - the case that used to make read model resolution
/// skip release entirely. The
/// injected read model itself also resolves no subject (it declares neither an <c language="csharp">Id</c> nor a
/// <c language="csharp">[Subject]</c>), which is the ordinary shape for a value that needs no subject at all: a
/// namespace-scoped <see cref="EncryptedAttribute"/>. This exercises the real Chronicle client end to end, not a
/// mocked <c language="csharp">IReadModels</c> - proving Arc's wiring and Chronicle's release path cooperate
/// correctly for exactly the value that has no subject to be gated on.
/// </summary>
public class and_no_subject_resolves : Specification
{
    CommandScenario<UseNamespaceScopedEncryptedWebhookConfig> _scenario;
    CommandResult _result;
    EventSourceId _eventSourceId;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _scenario = new CommandScenario<UseNamespaceScopedEncryptedWebhookConfig>();
        _scenario.Given.ForEventSource(_eventSourceId).Events(new WebhookConfigured("top-secret"));
    }

    async Task Because() => _result = await _scenario.Execute(new UseNamespaceScopedEncryptedWebhookConfig(_eventSourceId));

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_have_appended_the_released_value() =>
        await _scenario.ShouldHaveAppendedEvent<UseNamespaceScopedEncryptedWebhookConfig, WebhookSecretUsed>(_eventSourceId, @event => @event.Secret == "top-secret");
}
