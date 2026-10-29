// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CallerValidationSeverity.when_capping.given;

public class a_caller_request : Specification
{
    protected Type _commandType;
    protected ValidationResultSeverity? _requested;
    protected ValidationResultSeverity? _result;
    protected CommandOptions _options;

    void Establish()
    {
        _commandType = typeof(NoPolicyCommand);
        _requested = ValidationResultSeverity.Error;
        _options = new CommandOptions();
    }

    protected void Cap() => _result = CallerValidationSeverity.Cap(_commandType, _requested, _options);

    public record NoPolicyCommand;
}
