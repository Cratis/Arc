// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, render, type RenderResult } from '@testing-library/react';
import { CommandResult } from '@cratis/arc/commands';
import type { ValidationResult } from '@cratis/arc/validation';
import { CommandForm, useCommandFormContext, useSetCommandResult, type CommandFormProps } from '../../CommandForm';
import { TestCommand } from '../TestCommand';
import { a_command_form_context } from './a_command_form_context';

export class a_command_form_with_exception_feedback extends a_command_form_context {
    readonly privateMessage = 'Synthetic private database credential: never-display-this';
    readonly privateStack = 'SyntheticPrivateHandler at /private/server/command.cs:42';
    readonly defaultMessage = 'An unexpected error occurred. Please try again.';
    formContext!: ReturnType<typeof useCommandFormContext<TestCommand>>;
    setResult!: ReturnType<typeof useSetCommandResult>;
    rendered!: RenderResult;

    exceptionResult(hasExceptions = true, messages = [this.privateMessage], validationResults: ValidationResult[] = []) {
        return new CommandResult({
            correlationId: '0c0ee8c8-b5a6-4999-b030-6e6a0c931b91',
            isSuccess: false,
            isAuthorized: true,
            isValid: validationResults.length === 0,
            hasExceptions,
            validationResults,
            exceptionMessages: messages,
            exceptionStackTrace: this.privateStack,
            authorizationFailureReason: '',
            response: {}
        }, Object, false);
    }

    async renderForm(props: Omit<CommandFormProps<TestCommand>, 'command'> = {}) {
        const Probe = () => {
            this.formContext = useCommandFormContext<TestCommand>();
            this.setResult = useSetCommandResult();
            return null;
        };

        await act(async () => {
            this.rendered = render(
                <CommandForm command={TestCommand} {...props}>
                    <Probe />
                    {props.children}
                </CommandForm>,
                { wrapper: this.createWrapper() }
            );
        });
    }
}
