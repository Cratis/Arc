// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { CommandForm, useCommandFormContext } from '../CommandForm';
import { TestCommandWithResponse, TestCommandResponse } from './TestCommandWithResponse';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';
import { CommandResult } from '@cratis/arc/commands';

describe("when callbacks are provided and command succeeds", given(a_command_form_context, context => {
    let contextValue: ReturnType<typeof useCommandFormContext> | null = null;
    let onSuccessCalled = false;
    let onSuccessResponse: TestCommandResponse | undefined = undefined;
    let onFailedCalled = false;
    let onExceptionCalled = false;
    let onUnauthorizedCalled = false;
    let onValidationFailureCalled = false;

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
                    onSuccess: (response: TestCommandResponse) => {
                        onSuccessCalled = true;
                        onSuccessResponse = response;
                    },
                    onFailed: () => {
                        onFailedCalled = true;
                    },
                    onException: () => {
                        onExceptionCalled = true;
                    },
                    onUnauthorized: () => {
                        onUnauthorizedCalled = true;
                    },
                    onValidationFailure: () => {
                        onValidationFailureCalled = true;
                    }
                },
                React.createElement(TestComponent)
            ),
            { wrapper: context.createWrapper() }
        );
    });

    describe('and command execution returns success', () => {
        beforeEach(async () => {
            const mockResult = new CommandResult({
                correlationId: '0c0ee8c8-b5a6-4999-b030-6e6a0c931b91',
                isSuccess: true,
                isAuthorized: true,
                isValid: true,
                hasExceptions: false,
                validationResults: [],
                exceptionMessages: [],
                exceptionStackTrace: '',
                authorizationFailureReason: '',
                response: { id: '123', message: 'Success!' }
            }, Object, false);

            // Mock the execute method to return our success result
            const commandInstance = contextValue!.commandInstance as TestCommandWithResponse;
            commandInstance.execute = async () => mockResult as never;

            await contextValue!.onExecute!();
        });

        it('should call onSuccess callback', () => onSuccessCalled.should.be.true);
        it('should pass response to onSuccess callback', () => {
            onSuccessResponse!.id!.should.equal('123');
            onSuccessResponse!.message!.should.equal('Success!');
        });
        it('should not call onFailed callback', () => onFailedCalled.should.be.false);
        it('should not call onException callback', () => onExceptionCalled.should.be.false);
        it('should not call onUnauthorized callback', () => onUnauthorizedCalled.should.be.false);
        it('should not call onValidationFailure callback', () => onValidationFailureCalled.should.be.false);
    });
}));
