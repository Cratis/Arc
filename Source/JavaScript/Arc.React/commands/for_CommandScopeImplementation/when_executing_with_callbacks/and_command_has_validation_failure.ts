// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ICommand, CommandResult } from '@cratis/arc/commands';
import { CommandScopeImplementation } from '../../CommandScopeImplementation';
import { FakeCommand } from '../FakeCommand';

describe('when executing with callbacks and command has validation failure', async () => {
    const validationFailedResult = new CommandResult({
        correlationId: '00000000-0000-0000-0000-000000000000',
        isSuccess: false,
        isAuthorized: true,
        isValid: false,
        hasExceptions: false,
        validationResults: [{ severity: 0, message: 'Name is required', members: ['name'], state: {} }],
        exceptionMessages: [],
        exceptionStackTrace: '',
        authorizationFailureReason: '',
        response: {}
    }, Object, false);

    let onBeforeExecuteCalled = false;
    let onSuccessCalled = false;
    let onFailedCalled = false;
    let onFailedReceivedCommand: ICommand | undefined;
    let onFailedReceivedResult: CommandResult | undefined;
    let onExceptionCalled = false;
    let onUnauthorizedCalled = false;
    let onValidationFailureCalled = false;
    let onValidationFailureReceivedCommand: ICommand | undefined;
    let onValidationFailureReceivedResult: CommandResult | undefined;

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
            onUnauthorized: () => { onUnauthorizedCalled = true; },
            onValidationFailure: (command, result) => {
                onValidationFailureCalled = true;
                onValidationFailureReceivedCommand = command;
                onValidationFailureReceivedResult = result;
            },
        })
    );

    const command = new FakeCommand(true, validationFailedResult);
    scope.addCommand(command);

    await scope.execute();

    it('should call onBeforeExecute callback', () => onBeforeExecuteCalled.should.be.true);
    it('should not call onSuccess callback', () => onSuccessCalled.should.be.false);
    it('should call onFailed callback', () => onFailedCalled.should.be.true);
    it('should pass command to onFailed callback', () => onFailedReceivedCommand!.should.equal(command));
    it('should pass result to onFailed callback', () => onFailedReceivedResult!.should.equal(validationFailedResult));
    it('should not call onException callback', () => onExceptionCalled.should.be.false);
    it('should not call onUnauthorized callback', () => onUnauthorizedCalled.should.be.false);
    it('should call onValidationFailure callback', () => onValidationFailureCalled.should.be.true);
    it('should pass command to onValidationFailure callback', () => onValidationFailureReceivedCommand!.should.equal(command));
    it('should pass result to onValidationFailure callback', () => onValidationFailureReceivedResult!.should.equal(validationFailedResult));
});
