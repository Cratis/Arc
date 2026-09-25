// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Reducers;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_injecting_a_namespace_scoped_encrypted_read_model;

/// <summary>
/// A read model materialized by a reducer, carrying only a namespace-scoped <see cref="EncryptedAttribute"/> value
/// and deliberately declaring no <c language="csharp">Id</c> or <c language="csharp">[Subject]</c> - the ordinary
/// shape for a secret shared across every document in a namespace, such as a webhook signing secret.
/// </summary>
public record EncryptedWebhookConfig
{
    [Encrypted(EncryptionScope.Namespace)]
    public string Secret { get; init; } = string.Empty;
}

/// <summary>
/// The reducer that materializes <see cref="EncryptedWebhookConfig"/> from webhook configuration events.
/// </summary>
public class EncryptedWebhookConfigReducer : IReducerFor<EncryptedWebhookConfig>
{
    public EncryptedWebhookConfig Configured(WebhookConfigured @event, EncryptedWebhookConfig? current) =>
        new() { Secret = @event.Secret };
}

[EventType("51201cba-5ce3-4415-9baf-f4480ef85646")]
public record WebhookConfigured(string Secret);

/// <summary>
/// A command that injects <see cref="EncryptedWebhookConfig"/> to prove that a namespace-scoped
/// <see cref="EncryptedAttribute"/> value - which has no subject to be released against - still reaches
/// <see cref="Cratis.Chronicle.ReadModels.IReadModels.Release{TReadModel}(TReadModel)"/> and is handed to the
/// command's <c language="csharp">Handle()</c> already released, end to end through the real Chronicle client and
/// its in-process test compliance simulation - not a mocked <c language="csharp">IReadModels</c>.
/// </summary>
/// <param name="EventSourceId">The event source to inject <see cref="EncryptedWebhookConfig"/> for.</param>
[Command]
public record UseNamespaceScopedEncryptedWebhookConfig(EventSourceId EventSourceId)
{
    public WebhookSecretUsed Handle(EncryptedWebhookConfig config) => new(config.Secret);
}

[EventType("edc38d94-df16-48d5-80de-bb1afe753896")]
public record WebhookSecretUsed(string Secret);
