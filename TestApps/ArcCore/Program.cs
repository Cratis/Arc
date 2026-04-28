// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Authentication;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using TestApps.Authentication;
using TestApps.Features.AuthenticationQueries;
using TestApps.Features.ChangeStream;
using TestApps.Features.CrossCuttingAuthorization;
using TestApps.Features.LiveFeed;
using TestApps.Features.ObservableCollection;
using TestApps.Features.ObservableCollectionWithGuid;
using TestApps.Features.QueryShowcase;
using TestApps.Features.Ticker;

var builder = ArcApplication.CreateBuilder(args);

_ = typeof(ShowcaseItem);
RegisterGeneratedQueryMetadata();

builder
    .AddCratisArc(configureOptions: options =>
    {
        options.GeneratedApis.RoutePrefix = "api";
        options.GeneratedApis.IncludeCommandNameInRoute = false;
        options.GeneratedApis.SegmentsToSkipForRoute = 1;
    });

builder.Services.AddSingleton<IAuthentication>(_ =>
    new Authentication(new KnownInstancesOf<IAuthenticationHandler>([
        new CookieAuthenticationHandler(),
        new MicrosoftIdentityPlatformHeaderAuthenticationHandler()
    ])));

builder.Services.AddSingleton<IAuthorizationEvaluator>(serviceProvider =>
    new AuthorizationEvaluator(
        serviceProvider.GetRequiredService<Cratis.Arc.Http.IHttpRequestContextAccessor>(),
        new KnownInstancesOf<IAnonymousEvaluator>([
            new AnonymousEvaluator()
        ]),
        new KnownInstancesOf<IAuthorizationAttributeEvaluator>([
            new AuthorizationAttributeEvaluator()
        ])));

builder.Services.AddSingleton<IObservableQueryDemultiplexer, ObservableQueryDemultiplexer>();

builder.Services.AddSingleton<IQueryPerformerProviders>(serviceProvider =>
{
    var provider = new QueryPerformerProvider(
        serviceProvider.GetRequiredService<ITypes>(),
        serviceProvider.GetRequiredService<IQueryMetadataRegistry>(),
        serviceProvider.GetRequiredService<IServiceProviderIsService>(),
        serviceProvider.GetRequiredService<IAuthorizationEvaluator>());

    return new QueryPerformerProviders(new KnownInstancesOf<IQueryPerformerProvider>([provider]));
});

var app = builder.Build();
app.UseCratisArc();

await app.RunAsync();

static void RegisterGeneratedQueryMetadata()
{
    QueryMetadataRegistry.Register("TestApps.Features.AuthenticationQueries.AuthenticationQueryItem.Anonymous", typeof(AuthenticationQueryItem));
    QueryMetadataRegistry.Register("TestApps.Features.AuthenticationQueries.AuthenticationQueryItem.Authenticated", typeof(AuthenticationQueryItem));
    QueryMetadataRegistry.Register("TestApps.Features.ChangeStream.ChangeStreamItem.All", typeof(ChangeStreamItem));
    QueryMetadataRegistry.Register("TestApps.Features.CrossCuttingAuthorization.CrossCuttingAuthorizationStatus.Secured", typeof(CrossCuttingAuthorizationStatus));
    QueryMetadataRegistry.Register("TestApps.Features.LiveFeed.LiveFeed.All", typeof(LiveFeed));
    QueryMetadataRegistry.Register("TestApps.Features.LiveFeed.LiveFeed.ByAuthor", typeof(LiveFeed));
    QueryMetadataRegistry.Register("TestApps.Features.ObservableCollection.ObservableCollectionItem.All", typeof(ObservableCollectionItem));
    QueryMetadataRegistry.Register("TestApps.Features.ObservableCollectionWithGuid.ObservableCollectionWithGuidItem.All", typeof(ObservableCollectionWithGuidItem));
    QueryMetadataRegistry.Register("TestApps.Features.QueryShowcase.ShowcaseItem.Latest", typeof(ShowcaseItem));
    QueryMetadataRegistry.Register("TestApps.Features.QueryShowcase.ShowcaseItem.All", typeof(ShowcaseItem));
    QueryMetadataRegistry.Register("TestApps.Features.QueryShowcase.ShowcaseItem.ById", typeof(ShowcaseItem));
    QueryMetadataRegistry.Register("TestApps.Features.QueryShowcase.ShowcaseItem.GetAll", typeof(ShowcaseItem));
    QueryMetadataRegistry.Register("TestApps.Features.Ticker.Ticker.Observe", typeof(Ticker));
    QueryMetadataRegistry.Register("TestApps.ModelBoundReadModel.GetAll", typeof(TestApps.ModelBoundReadModel));
    QueryMetadataRegistry.Register("TestApps.ModelBoundReadModel.GetById", typeof(TestApps.ModelBoundReadModel));
}
