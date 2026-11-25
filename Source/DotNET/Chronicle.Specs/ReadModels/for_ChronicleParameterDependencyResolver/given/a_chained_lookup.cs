// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.Arc.Queries;
using Cratis.Arc.Validation;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;
using Cratis.Concepts;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver.given;

public class a_chained_lookup : Specification
{
    protected IDecisionReads _reader;
    protected UnitOfWork _unit;
    protected ServiceProvider _services;
    protected CommandContext _context;
    protected readonly List<string> _reads = [];
    protected readonly List<DecisionRead<Node>> _tokens = [];
    protected IReadModels _stored;
    protected readonly Node _first = new(new("first"), new("second"));
    protected readonly Node _second = new(new("second"), new("third"));
    protected readonly Node _third = new(new("third"), null);

    void Establish()
    {
        var (store, _, unit) = for_CommandDecisionReads.DecisionFixtures.Transaction();
        _unit = unit;
        _reader = Substitute.For<for_CommandDecisionReads.when_reading_a_stream.given.DecisionReaderForSpecs>();
        _stored = Substitute.For<IReadModels>();
        _stored.Release(Arg.Any<Node>()).Returns(call => call.Arg<Node>());
        var eventType = new Cratis.Chronicle.Events.EventType((Cratis.Chronicle.Events.EventTypeId)Guid.NewGuid().ToString(), Cratis.Chronicle.Events.EventTypeGeneration.First);
        _reader.Describe<Node>().Returns(new DecisionReadDescription(new(true, null), [eventType], false));
        foreach (var node in new[] { _first, _second, _third })
        {
            ReturnRead(node.Id.Value, node);
        }
        var accessor = Substitute.For<ICommandContextAccessor>();
        accessor.Current.Returns(_ => _context);
        ReadModelDispatch.Register<Node>();
        CommandReadModelKeys.DeclareModelKey(typeof(Node), typeof(NodeId));
        foreach (var command in new[] { typeof(Chain), typeof(Providing) })
        {
            CommandReadModelKeys.RegisterCommand(command, new CommandReadModelKeyProperty(nameof(Chain.Root), typeof(NodeId), instance => ((Chain)instance).Root));
        }
        ChainedReadModelKeys.RegisterModel(typeof(Node),
            new(nameof(Node.Next), typeof(NodeId), instance => ((Node)instance).Next),
            new(nameof(Node.Label), typeof(string), instance => ((Node)instance).Label));
        _services = new ServiceCollection()
            .AddSingleton(accessor)
            .AddSingleton<ICommandProtectedDecisionSupport>(new DecisionDependencySafety())
            .AddSingleton<IInstancesOf<ICanResolveParameterDependency>>(new KnownInstancesOf<ICanResolveParameterDependency>([new ChronicleParameterDependencyResolver([typeof(Node)])]))
            .AddSingleton(new RegisteredReadModelTypes([typeof(Node)]))
            .AddProtectedKeyTraversal(new ProtectedReadModelKeyOptions { Strict = true })
            .AddSingleton<IDecisionReads>(new CommandDecisionReads(_reader, store, _stored))
            .BuildServiceProvider();
        Select(new Chain(new("first")));
    }

    protected void ReturnRead(string key, Node? instance) =>
        _reader.GetFor<Node>(_unit, (ReadModelKey)key, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _reads.Add(key);
            var token = for_CommandDecisionReads.DecisionFixtures.Protected(key, instance);
            _tokens.Add(token);
            _unit.AddDecisionRead(token);
            return token;
        });

    protected Exception? Validate(string member, ReadProtectionMode mode = ReadProtectionMode.On) =>
        Catch.Exception(() => ChainedReadModelLookup.Order(typeof(Chain), typeof(Chain).GetMethod(member)!.GetParameters(), new HashSet<Type> { typeof(Node) }, mode));

    protected void Select(object command) => _context = new(Cratis.Execution.CorrelationId.New(), command.GetType(), command, [], new()) { ServiceProvider = _services };

    protected Task<object?[]> Bind(string member = "Handle") => InDecision(async () =>
    {
        var handler = Substitute.For<ICommandHandler>();
        handler.Parameters.Returns(_context.Type.GetMethod(member)!.GetParameters());
        return (await new CommandHandlerArgumentResolver(new CommandProvideInvoker()).Resolve(handler, _context, _services, null)).Arguments.ToArray();
    });

    protected Task<ChainValidator> BindValidator() => InDecision(() => Task.FromResult((ChainValidator)DiscoverableValidators.ConstructDependencies(_services, typeof(ChainValidator))));

    async Task<T> InDecision<T>(Func<Task<T>> action)
    {
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(_context.Type);
        CommandTransaction.Current = _unit;
        using var protection = _unit.EnforceReadProtection();
        CommandDecisionReads.Begin(_context.Type);
        try
        {
            return await action();
        }
        finally
        {
            CommandDecisionReads.End();
        }
    }

    void Destroy()
    {
        _services.Dispose();
        CommandTransaction.Current = null;
    }

    public record NodeId(string Value) : ConceptAs<string>(Value);
    public record Node(NodeId Id, NodeId? Next)
    {
        public string Label => Id.Value;
    }

    [ReadProtection(ReadProtectionMode.On)]
    public record Chain(NodeId Root)
    {
        public void Handle([LookUpBy(nameof(middle), nameof(Node.Next))] Node last, [LookUpBy(nameof(first), nameof(Node.Next))] Node middle, [LookUpBy(nameof(Root))] Node first) { }
        public void Cycle([LookUpBy(nameof(second), nameof(Node.Next))] Node first, [LookUpBy(nameof(first), nameof(Node.Next))] Node second) { }
        public void Optional([LookUpBy(nameof(first), nameof(Node.Next))] Node? target, [LookUpBy(nameof(Root))] Node? first) { }
        public void Required([LookUpBy(nameof(first), nameof(Node.Next))] Node target, [LookUpBy(nameof(Root))] Node? first) { }
        public void Unknown([LookUpBy("unknown", nameof(Node.Next))] Node target) { }
        public void Self([LookUpBy(nameof(target), nameof(Node.Next))] Node target) { }
        public void NonModel([LookUpBy(nameof(source), nameof(Node.Next))] Node target, string source) { }
        public void Missing([LookUpBy(nameof(source), "Missing")] Node target, Node source) { }
        public void Mismatch([LookUpBy(nameof(source), nameof(Node.Label))] Node target, Node source) { }
        public void Advisory([LookUpBy(nameof(source), nameof(Node.Next))] Node target, [Unprotected("advisory")] Node source) { }
        public void Mixed([LookUpBy<UnusedProvider>, LookUpBy(nameof(source), nameof(Node.Next))] Node target, Node source) { }
    }

    public record Providing(NodeId Root) : Chain(Root)
    {
        public Node Provide([LookUpBy(nameof(first), nameof(Node.Next))] Node target, [LookUpBy(nameof(Root))] Node first) => target;
    }

    public class ChainValidator([LookUpBy(nameof(first), nameof(Node.Next))] Node target, [LookUpBy(nameof(Chain.Root))] Node first) : AbstractValidator<Chain>, IDiscoverableValidator<Chain>
    {
        public Node Target { get; } = target;
        public Node First { get; } = first;
    }

    public class UnusedProvider : IReadModelKeyProvider
    {
        public ReadModelKeyLookup Resolve(ReadModelKeyContext context) => throw new InvalidOperationException("A metadata preflight must not invoke a provider");
    }
}
