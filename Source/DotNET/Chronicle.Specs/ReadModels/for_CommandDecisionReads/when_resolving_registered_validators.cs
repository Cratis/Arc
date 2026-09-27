// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;
using TypeCatalog = Cratis.Types.Types;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;

#pragma warning disable SA1402, SA1649

public class when_resolving_registered_validators
{
    [Fact]
    public void should_refuse_registered_reader_dependencies_before_constructing_the_validator()
    {
        var constructions = 0;
        var readsResolved = 0;
        using var provider = new ServiceCollection()
            .AddSingleton<ICommandDependencySafety, DecisionDependencySafety>()
            .AddTransient(_ =>
            {
                readsResolved++;
                return Substitute.For<IDecisionReads>();
            })
            .AddSingleton(services =>
            {
                constructions++;
                return new ReaderValidator(services.GetRequiredService<IDecisionReads>());
            })
            .BuildServiceProvider();
        var validators = new DiscoverableValidators(TypeCatalog.Instance);

        Assert.Throws<InvalidOperationException>(() => validators.TryGet(typeof(ReaderCommand), provider, out _));
        Assert.Equal(0, constructions);
        Assert.Equal(0, readsResolved);
    }

    [Fact]
    public void should_refuse_registered_token_dependencies_before_folding()
    {
        var folds = 0;
        using var provider = new ServiceCollection()
            .AddSingleton<ICommandDependencySafety, DecisionDependencySafety>()
            .AddTransient(_ =>
            {
                folds++;
                return DecisionFixtures.Protected<TokenModel>("source");
            })
            .AddSingleton(services => new TokenValidator(services.GetRequiredService<DecisionRead<TokenModel>>()))
            .BuildServiceProvider();
        var validators = new DiscoverableValidators(TypeCatalog.Instance);

        Assert.Throws<InvalidOperationException>(() => validators.TryGet(typeof(TokenCommand), provider, out _));
        Assert.Equal(0, folds);
    }

    [Fact]
    public void should_still_construct_unregistered_reader_validators_per_invocation()
    {
        var readsResolved = 0;
        using var provider = new ServiceCollection()
            .AddSingleton<ICommandDependencySafety, DecisionDependencySafety>()
            .AddTransient(_ =>
            {
                readsResolved++;
                return Substitute.For<IDecisionReads>();
            })
            .BuildServiceProvider();
        var validators = new DiscoverableValidators(TypeCatalog.Instance);

        Assert.True(validators.TryGet(typeof(ReaderCommand), provider, out var first));
        Assert.True(validators.TryGet(typeof(ReaderCommand), provider, out var second));
        Assert.NotSame(first, second);
        Assert.Equal(2, readsResolved);
    }
}

public record ReaderCommand;
public record TokenCommand;
public class TokenModel;

public class TokenValidator(DecisionRead<TokenModel> token) : CommandValidator<TokenCommand>
{
    public DecisionRead<TokenModel> Token { get; } = token;
}

public class ReaderValidator(IDecisionReads reads) : CommandValidator<ReaderCommand>
{
    public IDecisionReads Reads { get; } = reads;
}

#pragma warning restore SA1402, SA1649
