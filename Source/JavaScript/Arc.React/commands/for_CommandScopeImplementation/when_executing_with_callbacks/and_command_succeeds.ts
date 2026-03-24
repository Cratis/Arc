// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ICommand, CommandResult } from '@cratis/arc/commands';
import { CommandScopeImplementation } from '../../CommandScopeImplementation';
import { FakeCommand } from '../FakeCommand';

describe('when executing with callbacks and command succeeds', async () => {
    let onBeforeExecuteCalled = false;
    let onBeforeExecuteReceivedCommand: ICommand | undefined;
    let onSuccessCalled = false;
    let onSuccessReceivedCommand: ICommand | undefined;
    let onSuccessReceivedResult: CommandResult | undefined;
    let onFailedCalled = false;
    let onExceptionCalled = false;
    let onUnauthorizedCalled = false;
    let onValidationFailureCalled = false;

    const scope = new CommandScopeImplementation(
        () => {},
        undefined,
        undefined,
        () => ({
            onBeforeExecute: (command) => {
                onBeforeExecuteCalled = true;
                onBeforeExecuteReceivedCommand = command;
            },
            onSuccess: (command, result) => {
                onSuccessCalled = true;
                onSuccessReceivedCommand = command;
                onSuccessReceivedResult = result;
            },
            onFailed: () => { onFailedCalled = true; },
            onException: () => { onExceptionCalled = true; },
            onUnauthorized: () => { onUnauthorizedCalled = true; },
            onValidationFailure: () => { onValidationFailureCalled = true; },
        })
    );

    const command = new FakeCommand(true);
    scope.addCommand(command);

    await scope.execute();

    it('should call onBeforeExecute callback', () => onBeforeExecuteCalled.should.be.true);
    it('should pass command to onBeforeExecute callback', () => onBeforeExecuteReceivedCommand!.should.equal(command));
    it('should call onSuccess callback', () => onSuccessCalled.should.be.true);
    it('should pass command to onSuccess callback', () => onSuccessReceivedCommand!.should.equal(command));
    it('should pass result to onSuccess callback', () => onSuccessReceivedResult!.should.not.be.undefined);
    it('should not call onFailed callback', () => onFailedCalled.should.be.false);
    it('should not call onException callback', () => onExceptionCalled.should.be.false);
    it('should not call onUnauthorized callback', () => onUnauthorizedCalled.should.be.false);
    it('should not call onValidationFailure callback', () => onValidationFailureCalled.should.be.false);
});
