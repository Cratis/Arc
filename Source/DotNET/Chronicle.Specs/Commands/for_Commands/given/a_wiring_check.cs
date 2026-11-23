// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle;
using Cratis.Chronicle.ReadModels;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Testing.Commands.for_Commands.given;

public class a_wiring_check : Specification
{
    protected IServiceCollection Services;
    protected ITypes Types;
    protected IClientArtifactsProvider Artifacts;
    protected IReadOnlyList<CommandWiringProblem> Problems;
    ServiceProvider _root;
    IServiceScope _scope;

    protected IServiceProvider Provider
    {
        get
        {
            _root ??= Services.BuildServiceProvider();
            _scope ??= _root.CreateScope();
            return _scope.ServiceProvider;
        }
    }

    void Establish()
    {
        Types = Substitute.For<ITypes>();
        Artifacts = Substitute.For<IClientArtifactsProvider>();
        Artifacts.ModelBoundProjections.Returns([typeof(Model)]);
        Services = new ServiceCollection().AddSingleton(Types).AddSingleton(Artifacts).AddCompatibilityConfiguration();
    }

    void Destroy()
    {
        _scope?.Dispose();
        _root?.Dispose();
    }

    [ReadModel]
    public record Model(string Id);

    [Command]
    public class DefaultCommand
    {
        public void Handle(Model model) => throw new InvalidOperationException("Must not execute");
    }

    [Command]
    public class ExplicitCommand
    {
        public string Key => throw new InvalidOperationException("Must not read command values");
        public void Handle([LookUpBy(nameof(Key))] Model model) => throw new InvalidOperationException();
    }

    [Command]
    public class InvalidPropertyCommand
    {
        public void Handle([LookUpBy("Missing")] Model? model) => throw new InvalidOperationException();
    }

    [Command]
    public class CallerCommand
    {
        public void Handle([LookUpBy<CallerKey>] Model model) => throw new InvalidOperationException();
    }

    [Command]
    public class InvalidCallerCommand
    {
        public void Handle([LookUpBy<CallerKey>, LookUpBy("Id")] Model model) => throw new InvalidOperationException();
    }

    [Command]
    [ReadProtection(ReadProtectionMode.On)]
    public class AmbiguousCommand
    {
        public string First { get; set; }
        public string Second { get; set; }
        public void Handle(Model model) => throw new InvalidOperationException();
    }

    [Command]
    public class TokenCommand
    {
        public void Handle(DecisionRead<Model> model) => throw new InvalidOperationException();
    }

    [Command]
    public class RedirectedTokenCommand
    {
        public void Handle([LookUpBy<CallerKey>] DecisionRead<Model> model) => throw new InvalidOperationException();
    }

    [Command]
    public class StreamCommand
    {
        public void Handle(ReadModels.for_per_stream_models.given.a_monthly_account.MonthlyWithdrawals model) => throw new InvalidOperationException();
    }

    [Command]
    public class RedirectedStreamCommand
    {
        public void Handle([LookUpBy("Id")] ReadModels.for_per_stream_models.given.a_monthly_account.MonthlyWithdrawals model) => throw new InvalidOperationException();
    }

    public sealed class CallerKey : IReadModelKeyProvider
    {
        public CallerKey() => throw new InvalidOperationException("Must not construct caller key providers");
        public ReadModelKeyLookup Resolve(ReadModelKeyContext context) => throw new InvalidOperationException("Must not execute caller key providers");
    }

    public class Validator(Model model) : CommandValidator<DefaultCommand>
    {
        public Model Model { get; } = model;
    }
}
