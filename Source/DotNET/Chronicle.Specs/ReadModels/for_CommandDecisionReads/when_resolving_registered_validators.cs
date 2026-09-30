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
    public void should_refuse_a_supplied_provider_before_resolving_the_reader()
    {
        var constructions = 0;
        var readsResolved = 0;
        using var provider = new ServiceCollection()
            .AddSingleton<ICommandProtectedDecisionSupport, DecisionDependencySafety>()
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
        using var policy = DecisionPolicyForSpecs.Begin(typeof(ProtectedReaderCommand));

        Assert.Throws<DiscoverableValidatorRefusedInProtectedDecision>(() => validators.TryGet(typeof(ReaderCommand), provider, out _));
        Assert.Equal(0, constructions);
        Assert.Equal(0, readsResolved);
    }

    [Fact]
    public void should_refuse_a_supplied_provider_before_resolving_the_token()
    {
        var folds = 0;
        var validatorFactories = 0;
        using var provider = new ServiceCollection()
            .AddSingleton<ICommandProtectedDecisionSupport, DecisionDependencySafety>()
            .AddTransient(_ =>
            {
                folds++;
                return DecisionFixtures.Protected<TokenModel>("source");
            })
            .AddSingleton(services =>
            {
                validatorFactories++;
                return new TokenValidator(services.GetRequiredService<DecisionRead<TokenModel>>());
            })
            .BuildServiceProvider();
        var validators = new DiscoverableValidators(TypeCatalog.Instance);
        using var policy = DecisionPolicyForSpecs.Begin(typeof(ProtectedReaderCommand));

        Assert.Throws<DiscoverableValidatorRefusedInProtectedDecision>(() => validators.TryGet(typeof(TokenCommand), provider, out _));
        Assert.Equal(0, folds);
        Assert.Equal(0, validatorFactories);
    }

    [Fact]
    public void should_keep_command_dependency_guards()
    {
        var safety = new DecisionDependencySafety();
        Assert.Throws<ProtectedCommandRequiresCommandAwareReader>(() => safety.ValidateCommandDependency(typeof(IDecisionReads), null));
        Assert.Throws<ProtectedCommandRequiresIssuedDecisionRead>(() => safety.ValidateCommandDependency(typeof(DecisionRead<TokenModel>), null));
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

[ProtectedDecision]
public record ProtectedReaderCommand;
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
