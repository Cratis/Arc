// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.ReadModels;
using OneOf;

namespace Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup.given;

public static class ProvidedDeclarations
{
    public record Model;

    public class AsyncScalar
    {
        public Task<DecisionRead<Model>> Provide(IDecisionReads reads) => reads.Get<Model>("model");
        public void Handle(DecisionRead<Model> balance) { }
    }

    public class AsyncTuple
    {
        public async Task<(DecisionRead<Model>, string)> Provide(IDecisionReads reads) => (await reads.Get<Model>("model"), "name");
        public void Handle(DecisionRead<Model> balance, string name) { }
    }

    public class AsyncUnion
    {
        public async ValueTask<OneOf<DecisionRead<Model>, ValidationResult>> Provide(IDecisionReads reads) => await reads.Get<Model>("model");
        public void Handle(DecisionRead<Model> balance) { }
    }

    public class TupleUnion
    {
        public async Task<(OneOf<DecisionRead<Model>, ValidationResult>, string)> Provide(IDecisionReads reads) => (await reads.Get<Model>("model"), "name");
        public void Handle(DecisionRead<Model> balance, string name) { }
    }

    public class UnionAlternatives
    {
        public async Task<OneOf<DecisionRead<Model>, (DecisionRead<Model>, string)>> Provide(IDecisionReads reads) => await reads.Get<Model>("model");
        public void Handle(DecisionRead<Model> first, string name, DecisionRead<Model> second) { }
    }

    public class NestedTuple
    {
        public async Task<((DecisionRead<Model>, string), int)> Provide(IDecisionReads reads) => ((await reads.Get<Model>("model"), "name"), 1);
        public void Handle(DecisionRead<Model> balance) { }
    }

    public class EnumerableValue
    {
        public async Task<DecisionRead<Model>[]> Provide(IDecisionReads reads) => [await reads.Get<Model>("model")];
        public void Handle(DecisionRead<Model> balance) { }
    }

    public class RepeatedValue
    {
        public Task<DecisionRead<Model>> Provide(IDecisionReads reads) => reads.Get<Model>("model");
        public void Handle(DecisionRead<Model> first, DecisionRead<Model> second) { }
    }

    public class AssignableValue
    {
        public Task<DecisionRead<Model>> Provide(IDecisionReads reads) => reads.Get<Model>("model");
        public void Handle(object first, DecisionRead<Model> balance) { }
    }

    public class UnrelatedValue
    {
        public string Provide() => "name";
        public void Handle(DecisionRead<Model> balance) { }
    }

    public class ControlSignal
    {
        public (CommandResult, DecisionRead<Model>) Provide(CommandResult result, DecisionRead<Model> balance) => (result, balance);
        public void Handle(object first, DecisionRead<Model> balance) { }
    }

    public class LongTuple
    {
        public async Task<(int, int, int, int, int, int, int, DecisionRead<Model>)> Provide(IDecisionReads reads) => (1, 2, 3, 4, 5, 6, 7, await reads.Get<Model>("model"));
        public void Handle(DecisionRead<Model> balance) { }
    }

    public class NullableTuple
    {
        public async Task<(DecisionRead<Model>, string)?> Provide(IDecisionReads reads) => (await reads.Get<Model>("model"), "name");
        public void Handle(DecisionRead<Model> balance, string name) { }
    }

    public class InheritedValue : AsyncScalar;

    [ReadProtection(ReadProtectionMode.On)]
    public class InjectedProvideRead
    {
        public Declarations.Balance? Provide(Declarations.Balance? balance) => balance;
        public void Handle(Declarations.Balance? balance) { }
    }

    [ProtectedDecision, ReadProtection(ReadProtectionMode.On)]
    public class InjectedProvideToken
    {
        public DecisionRead<Model> Provide(DecisionRead<Model> balance) => balance;
        public void Handle(DecisionRead<Model> balance) { }
    }
}
