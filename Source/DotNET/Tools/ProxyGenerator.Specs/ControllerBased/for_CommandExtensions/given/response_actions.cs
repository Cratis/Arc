// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;
using Cratis.Arc.Validation;
using Cratis.Monads;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.ProxyGenerator.ControllerBased.for_CommandExtensions.given;

public record ClientResponse(string Name);

public record ReturnedOperation : ICommandOperation
{
    public void Execute() { }
}

[Route("api/responses")]
public class response_actions : ControllerBase
{
    [HttpPost("event")]
    public ProjectRegistered EventResponse() => new("Project");

    [HttpPost("async-event")]
    public Task<ProjectRegistered> AsyncEventResponse() => Task.FromResult(new ProjectRegistered("Project"));

    [HttpPost("events")]
    public IEnumerable<ProjectRegistered> EventCollectionResponse() => [];

    [HttpPost("nested-tuple")]
    public ((ProjectRegistered Event, ClientResponse Client) Nested, DependencyHandledValue Handled) NestedTupleResponse() => default;

    [HttpPost("tuple-in-union")]
    public Result<(ProjectRegistered Event, ClientResponse Client), ValidationResult> TupleInUnionResponse() => null!;

    [HttpPost("union-in-tuple")]
    public (Result<ProjectRegistered, ValidationResult> Nested, ClientResponse Client) UnionInTupleResponse() => default;

    [HttpPost("union")]
    public OneOf.OneOf<ProjectRegistered, ClientResponse> UnionResponse() => default;

    [HttpPost("collection-in-union")]
    public Result<(IEnumerable<ProjectRegistered> Events, ClientResponse Client), ValidationResult> CollectionInUnionResponse() => null!;

    [HttpPost("typed-in-union")]
    public Result<(DependencyHandledValue Handled, ClientResponse Client), ValidationResult> TypedInUnionResponse() => null!;

    [HttpPost("typed")]
    public DependencyHandledValue TypedResponse() => new();

    [HttpPost("operation")]
    public ReturnedOperation OperationResponse() => new();

    [HttpPost("operation-collection")]
    public ReturnedOperation[] OperationCollectionResponse() => [];

    [HttpPost("nested-operation")]
    public (Result<ReturnedOperation, ValidationResult> Nested, ClientResponse Client) NestedOperationResponse() => default;

    [HttpPost("ordinary")]
    public ClientResponse OrdinaryResponse() => new("Project");
}

public static class response_commands
{
    [Command]
    public record EventResponse
    {
        public ProjectRegistered Handle() => new("Project");
    }

    [Command]
    public record AsyncEventResponse
    {
        public Task<ProjectRegistered> Handle() => Task.FromResult(new ProjectRegistered("Project"));
    }

    [Command]
    public record EventCollectionResponse
    {
        public IEnumerable<ProjectRegistered> Handle() => [];
    }

    [Command]
    public record NestedTupleResponse
    {
        public ((ProjectRegistered Event, ClientResponse Client) Nested, DependencyHandledValue Handled) Handle() => default;
    }

    [Command]
    public record TupleInUnionResponse
    {
        public Result<(ProjectRegistered Event, ClientResponse Client), ValidationResult> Handle() => null!;
    }

    [Command]
    public record UnionInTupleResponse
    {
        public (Result<ProjectRegistered, ValidationResult> Nested, ClientResponse Client) Handle() => default;
    }

    [Command]
    public record UnionResponse
    {
        public OneOf.OneOf<ProjectRegistered, ClientResponse> Handle() => default;
    }

    [Command]
    public record CollectionInUnionResponse
    {
        public Result<(IEnumerable<ProjectRegistered> Events, ClientResponse Client), ValidationResult> Handle() => null!;
    }

    [Command]
    public record TypedInUnionResponse
    {
        public Result<(DependencyHandledValue Handled, ClientResponse Client), ValidationResult> Handle() => null!;
    }

    [Command]
    public record TypedResponse
    {
        public DependencyHandledValue Handle() => new();
    }

    [Command]
    public record OperationResponse
    {
        public ReturnedOperation Handle() => new();
    }

    [Command]
    public record OperationCollectionResponse
    {
        public ReturnedOperation[] Handle() => [];
    }

    [Command]
    public record NestedOperationResponse
    {
        public (Result<ReturnedOperation, ValidationResult> Nested, ClientResponse Client) Handle() => default;
    }

    [Command]
    public record OrdinaryResponse
    {
        public ClientResponse Handle() => new("Project");
    }
}
