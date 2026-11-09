// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using FluentValidation;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

public record InheritedCommandDetails(string Street);

public record CommandInputsBase
{
    public string Name { get; init; } = string.Empty;
    public InheritedCommandDetails Details { get; init; } = new(string.Empty);
    public virtual string Description { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;

    [JsonIgnore]
    public string IgnoredInput { get; init; } = string.Empty;

    protected string InternalInput { get; init; } = string.Empty;
}

public record CommandInputsMiddle : CommandInputsBase
{
    public int Count { get; init; }
}

[Command]
public record CommandWithInheritedInputs : CommandInputsMiddle
{
    public override string Description { get; init; } = string.Empty;
    public new int Code { get; init; }
    public bool Enabled { get; init; }

    [JsonIgnore]
    public string DeclaredIgnoredInput { get; init; } = string.Empty;

    public void Handle() { }
}

public class CommandWithInheritedInputsValidator : CommandValidator<CommandWithInheritedInputs>
{
    public CommandWithInheritedInputsValidator() => RuleFor(command => command.Name).NotEmpty();
}
