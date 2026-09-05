// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.InjectedReadModelAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_InjectedReadModelAnalyzer.for_ARC0006;

public class when_handle_parameter_is_nullable_projection_read_model
{
    [Fact] async Task should_not_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Projections
{
    public interface IProjectionFor<T> { }
}

namespace TestNamespace
{
    [Command]
    public record AssignContact
    {
        public void Handle(Customer? customer) { }
    }

    public class Customer { }

    public class CustomerProjection : IProjectionFor<Customer> { }
}");
}
