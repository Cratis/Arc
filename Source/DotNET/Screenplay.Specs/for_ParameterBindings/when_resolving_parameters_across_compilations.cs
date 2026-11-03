// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Aggregates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.for_ParameterBindings;

public class when_resolving_parameters_across_compilations : Specification
{
    Compilation _domain;
    Compilation _consumer;
    Compilation _otherDomain;
    SemanticModel _sourceModel;
    SemanticModel _callModel;
    ParameterBindings _bindings;
    ParameterBindings _localBindings;
    IMethodSymbol _sourceMethod;
    IMethodSymbol _calledMethod;

    void Establish()
    {
        const string domainSource = """
            namespace Domain;
            public class Behavior
            {
                public string Apply(string first, string second)
                {
                    System.Func<string, string> shadow = first => first;
                    return first + second;
                }
                public string Apply(string first) => first;
                public string Other(string first) => first;
            }
            public class OtherBehavior
            {
                public string Apply(string first, string second) => first + second;
            }
            public class LocalConsumer
            {
                public string Call(Behavior behavior) => behavior.Apply("local-one", second: "local-two");
            }
            """;
        const string consumerSource = """
            class Consumer
            {
                string Call(Domain.Behavior behavior) => behavior.Apply(second: "two", first: "one");
            }
            """;
        _domain = Analyzed.Project("Domain", [], ("Domain.cs", domainSource));
        _consumer = Analyzed.Project("Consumer", [], ("Consumer.cs", consumerSource))
            .WithReferences(((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator).Select(_ => MetadataReference.CreateFromFile(_))
                .Append<MetadataReference>(_domain.ToMetadataReference()));
        _otherDomain = _domain.WithAssemblyName("OtherDomain");
        _sourceModel = _domain.GetSemanticModel(_domain.SyntaxTrees.Single());
        _callModel = _consumer.GetSemanticModel(_consumer.SyntaxTrees.Single());
        var call = _consumer.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
        _calledMethod = (IMethodSymbol)_callModel.GetSymbolInfo(call).Symbol!;
        _sourceMethod = _domain.GetTypeByMetadataName("Domain.Behavior")!.GetMembers("Apply").OfType<IMethodSymbol>().Single(_ => _.Parameters.Length == 2);
        _bindings = ParameterBindings.For(_calledMethod, call, _callModel);
        var localCall = _domain.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
        _localBindings = ParameterBindings.For((IMethodSymbol)_sourceModel.GetSymbolInfo(localCall).Symbol!, localCall, _sourceModel);
    }

    BoundArgument? Resolve(string type, string method, int parameterCount, string parameter) =>
        _bindings.Resolve(
            _domain.GetTypeByMetadataName(type)!.GetMembers(method).OfType<IMethodSymbol>()
            .Single(_ => _.Parameters.Length == parameterCount).DeclaringSyntaxReferences.Single().GetSyntax()
                .DescendantNodes().OfType<IdentifierNameSyntax>().Last(_ => _.Identifier.ValueText == parameter),
            _sourceModel);

    BoundArgument? ResolveIn(Compilation compilation, ParameterBindings bindings, string parameter)
    {
        var expression = compilation.GetTypeByMetadataName("Domain.Behavior")!.GetMembers("Apply").OfType<IMethodSymbol>()
            .Single(_ => _.Parameters.Length == 2).DeclaringSyntaxReferences.Single().GetSyntax()
            .DescendantNodes().OfType<IdentifierNameSyntax>().Last(_ => _.Identifier.ValueText == parameter);
        return bindings.Resolve(expression, compilation.GetSemanticModel(expression.SyntaxTree));
    }

    [Fact] void should_compile_the_other_assembly() => Analyzed.ErrorsIn(_otherDomain).ShouldBeEmpty();
    [Fact] void should_distinguish_the_other_assembly() => _otherDomain.Assembly.Identity.Equals(_domain.Assembly.Identity).ShouldBeFalse();
    [Fact] void should_not_bind_the_first_parameter_from_another_assembly() => ResolveIn(_otherDomain, _bindings, "first").ShouldBeNull();
    [Fact] void should_not_bind_the_second_parameter_from_another_assembly() => ResolveIn(_otherDomain, _bindings, "second").ShouldBeNull();
    [Fact] void should_bind_the_local_positional_argument() => (ResolveIn(_domain, _localBindings, "first")?.Expression.ToString()).ShouldEqual("\"local-one\"");
    [Fact] void should_bind_the_local_named_argument() => (ResolveIn(_domain, _localBindings, "second")?.Expression.ToString()).ShouldEqual("\"local-two\"");
    [Fact] void should_keep_the_local_call_site_model() => (ResolveIn(_domain, _localBindings, "first")?.SemanticModel).ShouldEqual(_sourceModel);
    [Fact] void should_compile_the_domain() => Analyzed.ErrorsIn(_domain).ShouldBeEmpty();
    [Fact] void should_compile_the_consumer() => Analyzed.ErrorsIn(_consumer).ShouldBeEmpty();
    [Fact] void should_compare_distinct_parameter_symbols() => SymbolEqualityComparer.Default.Equals(_sourceMethod.Parameters[0], _calledMethod.Parameters[0]).ShouldBeFalse();
    [Fact] void should_bind_the_reordered_first_argument() => (Resolve("Domain.Behavior", "Apply", 2, "first")?.Expression.ToString()).ShouldEqual("\"one\"");
    [Fact] void should_bind_the_named_second_argument() => (Resolve("Domain.Behavior", "Apply", 2, "second")?.Expression.ToString()).ShouldEqual("\"two\"");
    [Fact] void should_keep_the_call_site_model() => (Resolve("Domain.Behavior", "Apply", 2, "first")?.SemanticModel).ShouldEqual(_callModel);
    [Fact] void should_not_bind_an_overload() => Resolve("Domain.Behavior", "Apply", 1, "first").ShouldBeNull();
    [Fact] void should_not_bind_another_method() => Resolve("Domain.Behavior", "Other", 1, "first").ShouldBeNull();
    [Fact] void should_not_bind_another_declaring_type() => Resolve("Domain.OtherBehavior", "Apply", 2, "first").ShouldBeNull();
    [Fact] void should_not_bind_a_shadowing_lambda_parameter() => _bindings.Resolve(
        (ExpressionSyntax)_domain.SyntaxTrees.Single().GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<SimpleLambdaExpressionSyntax>().Single().Body,
        _sourceModel).ShouldBeNull();
}
