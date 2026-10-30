// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Arc.Commands;
using Cratis.Arc.Identity;
using Samples.Features.AuthenticationQueries;
using Samples.Features.ChangeStream;
using Samples.Features.CrossCuttingAuthorization;
using Samples.Features.LiveFeed;
using Samples.Features.ObservableCollection;
using Samples.Features.ObservableCollectionWithGuid;
using Samples.Features.QueryShowcase;
using Samples.Features.Ticker;

namespace Samples;

/// <summary>
/// Provides JSON metadata for the shared sample commands, responses and read models.
/// </summary>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(ModelBoundCommand))]
[JsonSerializable(typeof(PostToFeed))]
[JsonSerializable(typeof(RunSecuredCommand))]
[JsonSerializable(typeof(AddChangeStreamItem))]
[JsonSerializable(typeof(UpdateChangeStreamItem))]
[JsonSerializable(typeof(RemoveChangeStreamItem))]
[JsonSerializable(typeof(AddObservableCollectionItem))]
[JsonSerializable(typeof(RemoveObservableCollectionItem))]
[JsonSerializable(typeof(AddObservableCollectionWithGuidItem))]
[JsonSerializable(typeof(RemoveObservableCollectionWithGuidItem))]
[JsonSerializable(typeof(CommandResult<string>))]
[JsonSerializable(typeof(CommandResult<LiveFeedMessage>))]
[JsonSerializable(typeof(LiveFeedMessage))]
[JsonSerializable(typeof(ClientPrincipal))]
[JsonSerializable(typeof(ModelBoundReadModel))]
[JsonSerializable(typeof(IEnumerable<ModelBoundReadModel>))]
[JsonSerializable(typeof(List<ModelBoundReadModel>))]
[JsonSerializable(typeof(ModelBoundReadModel[]))]
[JsonSerializable(typeof(ShowcaseItem))]
[JsonSerializable(typeof(IEnumerable<ShowcaseItem>))]
[JsonSerializable(typeof(List<ShowcaseItem>))]
[JsonSerializable(typeof(ShowcaseItem[]))]
[JsonSerializable(typeof(LiveFeed))]
[JsonSerializable(typeof(IEnumerable<LiveFeed>))]
[JsonSerializable(typeof(List<LiveFeed>))]
[JsonSerializable(typeof(LiveFeed[]))]
[JsonSerializable(typeof(ChangeStreamItem))]
[JsonSerializable(typeof(IEnumerable<ChangeStreamItem>))]
[JsonSerializable(typeof(List<ChangeStreamItem>))]
[JsonSerializable(typeof(ChangeStreamItem[]))]
[JsonSerializable(typeof(ObservableCollectionItem))]
[JsonSerializable(typeof(IEnumerable<ObservableCollectionItem>))]
[JsonSerializable(typeof(List<ObservableCollectionItem>))]
[JsonSerializable(typeof(ObservableCollectionItem[]))]
[JsonSerializable(typeof(ObservableCollectionWithGuidItem))]
[JsonSerializable(typeof(IEnumerable<ObservableCollectionWithGuidItem>))]
[JsonSerializable(typeof(List<ObservableCollectionWithGuidItem>))]
[JsonSerializable(typeof(ObservableCollectionWithGuidItem[]))]
[JsonSerializable(typeof(Ticker))]
[JsonSerializable(typeof(IEnumerable<Ticker>))]
[JsonSerializable(typeof(List<Ticker>))]
[JsonSerializable(typeof(Ticker[]))]
[JsonSerializable(typeof(AuthenticationQueryItem))]
[JsonSerializable(typeof(IEnumerable<AuthenticationQueryItem>))]
[JsonSerializable(typeof(List<AuthenticationQueryItem>))]
[JsonSerializable(typeof(AuthenticationQueryItem[]))]
[JsonSerializable(typeof(CrossCuttingAuthorizationStatus))]
[JsonSerializable(typeof(IEnumerable<CrossCuttingAuthorizationStatus>))]
[JsonSerializable(typeof(List<CrossCuttingAuthorizationStatus>))]
[JsonSerializable(typeof(CrossCuttingAuthorizationStatus[]))]
public partial class SamplesJsonSerializerContext : JsonSerializerContext;
