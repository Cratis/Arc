// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandAttributeAnalyzer;

public class when_a_provider_has_no_chronicle_backing : Specification
{
    Task _result;

    void Because() => _result = AnalyzerVerifier<CommandAttributeAnalyzer>.VerifyAnalyzerAsync(
        "using Cratis.Arc.Commands; using Cratis.Chronicle.ReadModels; " +
        "public class Command { public void Handle([LookUpBy<CallerSubject>] MongoModel mongo, " +
        "[LookUpBy<CallerSubject>] EntityModel entity, [LookUpBy<CallerSubject>] DecisionRead<MongoModel> decision, " +
        "[LookUpBy<CallerSubject>] MongoDB.Driver.IMongoCollection<MongoModel> collection) { } } " +
        "public class MongoModel; public class EntityModel; namespace MongoDB.Driver { public interface IMongoCollection<T> { } }",
        Unsupported(),
        Unsupported());

    [Fact] Task should_report_only_the_decision_read_and_collection_bindings() => _result;

    static ExpectedDiagnostic Unsupported() => new("ARCCHR0031", DiagnosticSeverity.Error, "only supported for Chronicle read models");
}
