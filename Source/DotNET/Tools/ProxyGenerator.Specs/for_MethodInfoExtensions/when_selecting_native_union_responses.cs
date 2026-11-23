// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using Cratis.Arc.Commands;
using Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;
using Cratis.Arc.ProxyGenerator.Templates;
using Cratis.Arc.Validation;

namespace Cratis.Arc.ProxyGenerator.for_MethodInfoExtensions;

public class when_selecting_native_union_responses : Specification
{
    // Compiler-generated union structs; equality is not meaningful for a handler return.
#pragma warning disable CA1815
    public readonly union EventOrValidation(ProjectRegistered, ValidationResult);
    public readonly union ReplyOrValidation(string, ValidationResult)
    {
        public ReplyOrValidation(int number, bool flag) : this("receipt") { }
        internal ReplyOrValidation(decimal number) : this("receipt") { }
    }
    public readonly union OperationOrValidation(CommandOperations, ValidationResult);
    public readonly union TupleOrValidation((Guid, ProjectRegistered), ValidationResult);
    public readonly union Generic<T>(T, ValidationResult);
    public readonly union Nested(ReplyOrValidation, ProjectRegistered);
    public readonly union BareOperations(List<ICommandOperation>, ValidationResult);
    public readonly union ManyClients(ClientResponse, string, ProjectId);
    public readonly union PrimitiveClients(ClientResponse, int);
#pragma warning restore CA1815

    public static EventOrValidation Event() => default;
    public static ReplyOrValidation Reply() => default;
    public static Task<OperationOrValidation> Operations() => Task.FromResult(default(OperationOrValidation));
    public static ValueTask<TupleOrValidation> Tuple() => new(default(TupleOrValidation));
    public static Generic<string> ClosedGeneric() => default;
    public static Nested NestedReply() => default;
    public static BareOperations Bare() => default;
    public static ManyClients Many() => default;
    public static PrimitiveClients Primitive() => default;

    public record ClientResponse(string Name);
    public record ProjectId(Guid Value) : Cratis.Concepts.ConceptAs<Guid>(Value);

    [Fact] void should_suppress_all_server_handled_cases() => Response(nameof(Event)).HasResponse.ShouldBeFalse();
    [Fact] void should_suppress_operation_cases() => Response(nameof(Operations)).HasResponse.ShouldBeFalse();
    [Fact] void should_ignore_user_constructor_parameters() => typeof(ReplyOrValidation).NativeUnionCases().ShouldEqual([typeof(string), typeof(ValidationResult)]);
    [Fact] void should_select_the_client_case() => Response(nameof(Reply)).ResponseModel.Type.ShouldEqual(typeof(string));
    [Fact] void should_select_a_tuple_response_after_awaiting() => Response(nameof(Tuple)).ResponseModel.Type.ShouldEqual(typeof(Guid));
    [Fact] void should_select_the_substituted_generic_case() => Response(nameof(ClosedGeneric)).ResponseModel.Type.ShouldEqual(typeof(string));
    [Fact] void should_select_a_nested_union_case() => Response(nameof(NestedReply)).ResponseModel.Type.ShouldEqual(typeof(string));
    [Fact] void should_prefer_a_concept_over_other_client_cases() => Response(nameof(Many)).ResponseModel.Type.ShouldEqual(typeof(ProjectId));
    [Fact] void should_prefer_a_primitive_over_an_object_case() => Response(nameof(Primitive)).ResponseModel.Type.ShouldEqual(typeof(int));
    [Fact] void should_preserve_controller_outputs() => Response(nameof(Event), false).HasResponse.ShouldBeTrue();
    [Fact] void should_reject_a_bare_operation_collection() => Catch.Exception(() => Response(nameof(Bare))).ShouldBeOfExactType<UnsupportedCommandOperationCollection>();
    [Fact] void should_render_a_command_without_a_response() => Render(nameof(Event)).ShouldContain("extends Command<IExecute>");
    [Fact] void should_render_a_command_with_the_client_case() => Render(nameof(Reply)).ShouldContain("extends Command<IExecute, string>");
    [Fact] void should_not_render_union_wrapper_types() => Render(nameof(Reply)).ShouldNotContain("ReplyOrValidation");

    static (bool HasResponse, ModelDescriptor ResponseModel) Response(string method, bool handlers = true) =>
        typeof(when_selecting_native_union_responses).GetMethod(method)!.GetResponseModel(handlers);

    static string Render(string method)
    {
        var response = Response(method);
        return TemplateTypes.Command(new
        {
            Name = "Execute",
            Route = "api/execute",
            response.HasResponse,
            ResponseType = response.ResponseModel,
            Properties = Array.Empty<object>(),
            Parameters = Array.Empty<object>(),
            Imports = Array.Empty<object>(),
            Roles = Array.Empty<string>(),
            TreatWarningsAsErrorsForPolicy = false
        });
    }
}
#endif
