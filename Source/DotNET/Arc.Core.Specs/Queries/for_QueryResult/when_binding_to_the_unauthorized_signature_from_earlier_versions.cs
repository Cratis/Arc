// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_QueryResult;

/// <summary>
/// Guards the binary surface published assemblies bind to. Adding an optional parameter to an existing
/// public method keeps callers compiling, but assemblies built against the old signature fail with
/// <see cref="MissingMethodException"/>. The old signature must stay as its own overload.
/// </summary>
public class when_binding_to_the_unauthorized_signature_from_earlier_versions : Specification
{
    MethodInfo? _method;
    QueryResult _result;

    void Because()
    {
        _method = typeof(QueryResult).GetMethod(
            nameof(QueryResult.Unauthorized),
            BindingFlags.Public | BindingFlags.Static,
            [typeof(CorrelationId)]);
        _result = (QueryResult)_method!.Invoke(null, [CorrelationId.New()])!;
    }

    [Fact] void should_exist_with_only_the_correlation_id() => _method.ShouldNotBeNull();
    [Fact] void should_return_a_query_result() => _method!.ReturnType.ShouldEqual(typeof(QueryResult));
    [Fact] void should_not_be_authorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_have_no_authorization_failure_reason() => _result.AuthorizationFailureReason.ShouldEqual(string.Empty);
}
