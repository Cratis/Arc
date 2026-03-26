// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ICommand, CommandResult } from '@cratis/arc/commands';
import { CommandScopeImplementation } from '../../CommandScopeImplementation';
import { FakeCommand } from '../FakeCommand';

describe('when executing with callbacks and command is unauthorized', async () => {
    const unauthorizedResult = new CommandResult({
        correlationId: '00000000-0000-0000-0000-000000000000',
        isSuccess: false,
        isAuthorized: false,
        isValid: true,
        hasExceptions: false,
        validationResults: [],
        exceptionMessages: [],
        exceptionStackTrace: '',
        authorizationFailureReason: 'Unauthorized',
        response: {}
    }, Object, false);

    let onBeforeExecuteCalled = false;
    let onSuccessCalled = false;
    let onFailedCalled = false;
    let onFailedReceivedCommand: ICommand | undefined;
    let onFailedReceivedResult: CommandResult | undefined;
    let onExceptionCalled = false;
    let onUnauthorizedCalled = false;
    let onUnauthorizedReceivedCommand: ICommand | undefined;
    let onUnauthorizedReceivedResult: CommandResult | undefined;
    let onValidationFailureCalled = false;

    const scope = new CommandScopeImplementation(
        () => {},
        undefined,
        undefined,
        () => ({
            onBeforeExecute: () => { onBeforeExecuteCalled = true; },
            onSuccess: () => { onSuccessCalled = true; },
            onFailed: (command, result) => {
                onFailedCalled = true;
                onFailedReceivedCommand = command;
                onFailedReceivedResult = result;
            },
            onException: () => { onExceptionCalled = true; },
            onUnauthorized: (command, result) => {
                onUnauthorizedCalled = true;
                onUnauthorizedReceivedCommand = command;
                onUnauthorizedReceivedResult = result;
            },
            onValidationFailure: () => { onValidationFailureCalled = true; },
        })
    );

    const command = new FakeCommand(true, unauthorizedResult);
    scope.addCommand(command);

    await scope.execute();

    it('should call onBeforeExecute callback', () => onBeforeExecuteCalled.should.be.true);
    it('should not call onSuccess callback', () => onSuccessCalled.should.be.false);
    it('should call onFailed callback', () => onFailedCalled.should.be.true);
    it('should pass command to onFailed callback', () => onFailedReceivedCommand!.should.equal(command));
    it('should pass result to onFailed callback', () => onFailedReceivedResult!.should.equal(unauthorizedResult));
    it('should not call onException callback', () => onExceptionCalled.should.be.false);
    it('should call onUnauthorized callback', () => onUnauthorizedCalled.should.be.true);
    it('should pass command to onUnauthorized callback', () => onUnauthorizedReceivedCommand!.should.equal(command));
    it('should pass result to onUnauthorized callback', () => onUnauthorizedReceivedResult!.should.equal(unauthorizedResult));
    it('should not call onValidationFailure callback', () => onValidationFailureCalled.should.be.false);
});
