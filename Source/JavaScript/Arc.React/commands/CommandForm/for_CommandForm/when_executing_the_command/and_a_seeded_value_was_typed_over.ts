// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, fireEvent, act, waitFor } from '@testing-library/react';
import { CommandForm, useCommandFormContext } from '../../CommandForm';
import { asCommandFormField } from '../../asCommandFormField';
import { Command } from '@cratis/arc/commands';
import { TestCommand } from '../TestCommand';
import { a_command_form_being_executed } from '../given/a_command_form_being_executed';
import { given } from '../../../../given';

const SimpleTextField = asCommandFormField<{
    value: string;
    onChange: (value: unknown) => void;
    onBlur?: () => void;
    invalid: boolean;
    required: boolean;
    errors: string[];
    'data-testid'?: string;
}>(
    (props) => React.createElement('input', {
        type: 'text',
        value: props.value,
        onChange: props.onChange,
        onBlur: props.onBlur,
        'data-testid': props['data-testid']
    }),
    {
        defaultValue: '',
        extractValue: (e: unknown) => (e as React.ChangeEvent<HTMLInputElement>).target.value
    }
);

// The seed layer is a stable object, which is how a caller that is not fighting the form writes it.
const seededValues = { name: 'seed' };

// The control. Reporting execution state adds renders to the form, and extra renders are exactly what
// makes a seed layer overwrite what the user has typed - which is how the previous attempt at this
// feature reverted field values on submit. Nothing here is about execution state; it fails the moment
// the new state disturbs value resolution.
describe('when executing the command and a seeded value was typed over', given(a_command_form_being_executed, context => {
    let contextValue: ReturnType<typeof useCommandFormContext> | null = null;
    let valueBeforeSubmitting: string | undefined;
    let valueAfterSubmitting: string | undefined;

    const Recorder = () => {
        contextValue = useCommandFormContext();
        return React.createElement('div');
    };

    beforeEach(async () => {
        context.reset();
        contextValue = null;

        const renderResult = render(
            React.createElement(
                CommandForm,
                {
                    command: TestCommand,
                    currentValues: seededValues
                },
                React.createElement(SimpleTextField, {
                    value: (c: TestCommand) => c.name,
                    title: 'Name',
                    'data-testid': 'name-input'
                }),
                React.createElement(Recorder)
            ),
            { wrapper: context.createWrapper() }
        );

        const nameInput = renderResult.getByTestId('name-input') as HTMLInputElement;
        await waitFor(() => { expect((contextValue!.commandInstance as TestCommand).name).to.equal('seed'); });

        await act(async () => {
            fireEvent.change(nameInput, { target: { value: 'typed-by-user' } });
        });
        valueBeforeSubmitting = (contextValue!.commandInstance as TestCommand).name;

        let pending: Promise<unknown> = Promise.resolve();
        await act(async () => {
            (contextValue!.commandInstance as unknown as Command).execute = context.executeDeferred as never;
            pending = contextValue!.onExecute!();
        });

        await act(async () => {
            context.executions[0].succeed(context.successfulResult());
            await pending;
        });

        valueAfterSubmitting = (contextValue!.commandInstance as TestCommand).name;
    });

    it('should hold the typed value before submitting', () => expect(valueBeforeSubmitting).to.equal('typed-by-user'));
    it('should still hold the typed value after submitting', () => expect(valueAfterSubmitting).to.equal('typed-by-user'));
    it('should not have reverted to the seeded value', () => expect(valueAfterSubmitting).to.not.equal('seed'));
}));
