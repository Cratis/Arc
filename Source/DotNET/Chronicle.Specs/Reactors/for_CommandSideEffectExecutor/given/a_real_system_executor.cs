// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Http;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.given;

public class a_real_system_executor : a_command_side_effect_executor
{
    protected CurrentPrincipalAccessor _principal;
    protected ClaimsPrincipal _original;
    protected ClaimsPrincipal _during;
    protected ClaimsPrincipal _after;
    protected ClaimsPrincipal _whilePending;
    protected TaskCompletionSource<CommandResult> _commandResult;
    protected TestCommand _command;
    protected Exception _error;

    void Establish()
    {
        var requestAccessor = Substitute.For<IHttpRequestContextAccessor>();
        requestAccessor.Current.Returns((IHttpRequestContext)null!);
        _principal = new(requestAccessor);
        _original = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "caller")], "caller"));
        _executor = new(_serviceScopeFactory, new SystemExecution(_principal));
        _command = new("command");
        _commandResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _commandPipeline.Execute(_command, _serviceProvider).Returns(_ =>
        {
            _during = _principal.Current!;
            return _commandResult.Task;
        });
    }

    public class InheritedSystemReactor : SystemReactor;

    public class CommandExecutionFailed() : Exception("The command execution failed.");

    public class ScopeCreationFailed() : Exception("The command scope could not be created.");

    public class PipelineResolutionFailed() : Exception("The command pipeline could not be resolved.");
}
