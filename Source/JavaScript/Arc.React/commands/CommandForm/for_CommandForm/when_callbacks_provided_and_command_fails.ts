// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { CommandForm, useCommandFormContext } from '../CommandForm';
import { TestCommandWithResponse, TestCommandResponse } from './TestCommandWithResponse';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';
import { CommandResult } from '@cratis/arc/commands';
import { ValidationResult, ValidationResultSeverity } from '@cratis/arc/validation';

describe("when callbacks are provided and command fails", given(a_command_form_context, context => {
    let contextValue: ReturnType<typeof useCommandFormContext> | null = null;
    let onSuccessCalled = false;
    let onFailedCalled = false;
    let onFailedResult: CommandResult<TestCommandResponse> | undefined = undefined;
    let onExceptionCalled = false;
    let onExceptionMessages: string[] = [];
    let onExceptionStackTrace = '';
    let onUnauthorizedCalled = false;
    let onValidationFailureCalled = false;
    let onValidationFailureResults: ValidationResult[] = [];

    beforeEach(() => {
        const TestComponent = () => {
            contextValue = useCommandFormContext();
            return React.createElement('div');
        };

        render(
            React.createElement(
                CommandForm<TestCommandWithResponse, TestCommandResponse>,
                {
                    command: TestCommandWithResponse,
                    onSuccess: () => {
                        onSuccessCalled = true;
                    },
                    onFailed: (commandResult) => {
                        onFailedCalled = true;
                        onFailedResult = commandResult as CommandResult<TestCommandResponse>;
                    },
                    onException: (messages, stackTrace) => {
                        onExceptionCalled = true;
                        onExceptionMessages = messages;
                        onExceptionStackTrace = stackTrace;
                    },
                    onUnauthorized: () => {
                        onUnauthorizedCalled = true;
                    },
                    onValidationFailure: (validationResults) => {
                        onValidationFailureCalled = true;
                        onValidationFailureResults = validationResults;
                    }
                },
                React.createElement(TestComponent)
            ),
            { wrapper: context.createWrapper() }
        );
    });

    describe('and command has exceptions', () => {
        beforeEach(async () => {
            const mockResult = new CommandResult({
                correlationId: '0c0ee8c8-b5a6-4999-b030-6e6a0c931b91',
                isSuccess: false,
                isAuthorized: true,
                isValid: true,
                hasExceptions: true,
                validationResults: [],
                exceptionMessages: ['Something went wrong', 'Another error'],
                exceptionStackTrace: 'Stack trace here',
                authorizationFailureReason: '',
                response: {}
            }, Object, false);

            const commandInstance = contextValue!.commandInstance as TestCommandWithResponse;
            commandInstance.execute = async () => mockResult as never;

            await contextValue!.onExecute!();
        });

        it('should not call onSuccess callback', () => onSuccessCalled.should.be.false);
        it('should call onFailed callback', () => onFailedCalled.should.be.true);
        it('should pass command result to onFailed callback', () => onFailedResult!.should.not.be.undefined);
        it('should call onException callback', () => onExceptionCalled.should.be.true);
        it('should pass exception messages to onException callback', () => {
            onExceptionMessages.length.should.equal(2);
            onExceptionMessages[0].should.equal('Something went wrong');
            onExceptionMessages[1].should.equal('Another error');
        });
        it('should pass stack trace to onException callback', () => onExceptionStackTrace.should.equal('Stack trace here'));
        it('should not call onUnauthorized callback', () => onUnauthorizedCalled.should.be.false);
        it('should not call onValidationFailure callback', () => onValidationFailureCalled.should.be.false);
    });

    describe('and command is unauthorized', () => {
        beforeEach(async () => {
            const mockResult = new CommandResult({
                correlationId: '0c0ee8c8-b5a6-4999-b030-6e6a0c931b91',
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

            const commandInstance = contextValue!.commandInstance as TestCommandWithResponse;
            commandInstance.execute = async () => mockResult as never;

            onSuccessCalled = false;
            onFailedCalled = false;
            onExceptionCalled = false;
            onUnauthorizedCalled = false;
            onValidationFailureCalled = false;

            await contextValue!.onExecute!();
        });

        it('should not call onSuccess callback', () => onSuccessCalled.should.be.false);
        it('should call onFailed callback', () => onFailedCalled.should.be.true);
        it('should not call onException callback', () => onExceptionCalled.should.be.false);
        it('should call onUnauthorized callback', () => onUnauthorizedCalled.should.be.true);
        it('should not call onValidationFailure callback', () => onValidationFailureCalled.should.be.false);
    });

    describe('and command has validation errors', () => {
        beforeEach(async () => {
            const mockResult = new CommandResult({
                correlationId: '0c0ee8c8-b5a6-4999-b030-6e6a0c931b91',
                isSuccess: false,
                isAuthorized: true,
                isValid: false,
                hasExceptions: false,
                validationResults: [
                    { severity: ValidationResultSeverity.Error, message: 'Name is required', members: ['name'], state: {} },
                    { severity: ValidationResultSeverity.Error, message: 'Email is invalid', members: ['email'], state: {} }
                ],
                exceptionMessages: [],
                exceptionStackTrace: '',
                authorizationFailureReason: '',
                response: {}
            }, Object, false);

            const commandInstance = contextValue!.commandInstance as TestCommandWithResponse;
            commandInstance.execute = async () => mockResult as never;

            onSuccessCalled = false;
            onFailedCalled = false;
            onExceptionCalled = false;
            onUnauthorizedCalled = false;
            onValidationFailureCalled = false;

            await contextValue!.onExecute!();
        });

        it('should not call onSuccess callback', () => onSuccessCalled.should.be.false);
        it('should call onFailed callback', () => onFailedCalled.should.be.true);
        it('should not call onException callback', () => onExceptionCalled.should.be.false);
        it('should not call onUnauthorized callback', () => onUnauthorizedCalled.should.be.false);
        it('should call onValidationFailure callback', () => onValidationFailureCalled.should.be.true);
        it('should pass validation results to callback', () => {
            onValidationFailureResults.length.should.equal(2);
            onValidationFailureResults[0].message.should.equal('Name is required');
            onValidationFailureResults[1].message.should.equal('Email is invalid');
        });
    });
}));
