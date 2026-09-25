// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_performing;

public class with_new_parameter_instances_on_each_read : given.a_model_bound_query_performer
{
    public record TestReadModel(object FirstDependency, string Name, object SecondDependency, int Age)
    {
        public static TestReadModel Query(object firstDependency, string name, object secondDependency, int age = 42) =>
            new(firstDependency, name, secondDependency, age);
    }

    readonly object _firstDependency = new();
    readonly object _secondDependency = new();
    TestReadModel _received;

    void Establish()
    {
        _serviceProviderIsService.IsService(typeof(object)).Returns(true);
        _serviceProviderIsService.IsService(typeof(string)).Returns(false);

        var method = new FreshParametersMethod(typeof(TestReadModel).GetMethod(nameof(TestReadModel.Query))!);
        _performer = new ModelBoundQueryPerformer(typeof(TestReadModel), typeof(TestReadModel).FullName!, method, _serviceProviderIsService, _authorizationEvaluator);
        _context = new QueryContext(
            _performer.FullyQualifiedName,
            Cratis.Execution.CorrelationId.New(),
            Paging.NotPaged,
            Sorting.None,
            new QueryArguments { ["name"] = "Jane" },
            [_firstDependency, _secondDependency]);
    }

    async Task Because() => _received = (TestReadModel)(await _performer.Perform(_context))!;

    [Fact] void should_bind_first_dependency() => ReferenceEquals(_received.FirstDependency, _firstDependency).ShouldBeTrue();
    [Fact] void should_bind_query_argument() => _received.Name.ShouldEqual("Jane");
    [Fact] void should_bind_second_dependency() => ReferenceEquals(_received.SecondDependency, _secondDependency).ShouldBeTrue();
    [Fact] void should_use_default_query_argument() => _received.Age.ShouldEqual(42);

    sealed class FreshParametersMethod(MethodInfo inner) : MethodInfo
    {
        public override string Name => inner.Name;
        public override Type? DeclaringType => inner.DeclaringType;
        public override Type? ReflectedType => inner.ReflectedType;
        public override MethodAttributes Attributes => inner.Attributes;
        public override RuntimeMethodHandle MethodHandle => inner.MethodHandle;
        public override Type ReturnType => inner.ReturnType;
        public override ICustomAttributeProvider ReturnTypeCustomAttributes => inner.ReturnTypeCustomAttributes;

        public override MethodInfo GetBaseDefinition() => inner.GetBaseDefinition();
        public override MethodImplAttributes GetMethodImplementationFlags() => inner.GetMethodImplementationFlags();
        public override ParameterInfo[] GetParameters() => inner.GetParameters().Select(parameter => new FreshParameter(parameter)).ToArray();
        public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture) =>
            inner.Invoke(obj, invokeAttr, binder, parameters, culture);
        public override object[] GetCustomAttributes(bool inherit) => inner.GetCustomAttributes(inherit);
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => inner.GetCustomAttributes(attributeType, inherit);
        public override bool IsDefined(Type attributeType, bool inherit) => inner.IsDefined(attributeType, inherit);
    }

    sealed class FreshParameter(ParameterInfo parameter) : ParameterInfo
    {
        public override string? Name { get; } = parameter.Name;
        public override Type ParameterType { get; } = parameter.ParameterType;
        public override int Position { get; } = parameter.Position;
        public override ParameterAttributes Attributes { get; } = parameter.Attributes;
        public override object? DefaultValue { get; } = parameter.DefaultValue;
        public override bool HasDefaultValue { get; } = parameter.HasDefaultValue;
        public override MemberInfo Member { get; } = parameter.Member;
    }
}
