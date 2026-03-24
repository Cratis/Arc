// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ICommand, CommandResult } from '@cratis/arc/commands';
import { CommandScopeImplementation } from '../../CommandScopeImplementation';
import { FakeCommand } from '../FakeCommand';

describe('when executing with callbacks and command has exception', async () => {
    const exceptionResult = new CommandResult({
        correlationId: '00000000-0000-0000-0000-000000000000',
        isSuccess: false,
        isAuthorized: true,
        isValid: true,
        hasExceptions: true,
        validationResults: [],
        exceptionMessages: ['Something went wrong'],
        exceptionStackTrace: 'Stack trace here',
        authorizationFailureReason: '',
        response: {}
    }, Object, false);

    let onBeforeExecuteCalled = false;
    let onSuccessCalled = false;
    let onFailedCalled = false;
    let onExceptionCalled = false;
    let onExceptionReceivedCommand: ICommand | undefined;
    let onExceptionReceivedResult: CommandResult | undefined;
    let onUnauthorizedCalled = false;
    let onValidationFailureCalled = false;

    const scope = new CommandScopeImplementation(
        () => {},
        undefined,
        undefined,
        () => ({
            onBeforeExecute: () => { onBeforeExecuteCalled = true; },
            onSuccess: () => { onSuccessCalled = true; },
            onFailed: () => { onFailedCalled = true; },
            onException: (command, result) => {
                onExceptionCalled = true;
                onExceptionReceivedCommand = command;
                onExceptionReceivedResult = result;
            },
            onUnauthorized: () => { onUnauthorizedCalled = true; },
            onValidationFailure: () => { onValidationFailureCalled = true; },
        })
    );

    const command = new FakeCommand(true, exceptionResult);
    scope.addCommand(command);

    await scope.execute();

    it('should call onBeforeExecute callback', () => onBeforeExecuteCalled.should.be.true);
    it('should not call onSuccess callback', () => onSuccessCalled.should.be.false);
    it('should call onFailed callback', () => onFailedCalled.should.be.true);
    it('should not call onUnauthorized callback', () => onUnauthorizedCalled.should.be.false);
    it('should not call onValidationFailure callback', () => onValidationFailureCalled.should.be.false);
    it('should call onException callback', () => onExceptionCalled.should.be.true);
    it('should pass command to onException callback', () => onExceptionReceivedCommand!.should.equal(command));
    it('should pass result to onException callback', () => onExceptionReceivedResult!.should.equal(exceptionResult));
});
