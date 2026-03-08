// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, fireEvent, waitFor, act } from '@testing-library/react';
import { CommandForm, useCommandFormContext } from '../../CommandForm';
import { asCommandFormField } from '../../asCommandFormField';
import { Command, CommandValidator } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';
import { a_command_form_context } from '../given/a_command_form_context';
import { given } from '../../../../given';

class TwoFieldCommandValidator extends CommandValidator<TwoFieldCommand> {
    constructor() {
        super();
        this.ruleFor(c => c.name).notEmpty().minLength(3);
        this.ruleFor(c => c.email).notEmpty().emailAddress();
    }
}

class TwoFieldCommand extends Command {
    readonly route = '/api/test';
    readonly validation = new TwoFieldCommandValidator();
    readonly propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('name', String, false),
        new PropertyDescriptor('email', String, false)
    ];

    name = '';
    email = '';

    get properties(): string[] { return ['name', 'email']; }
    get requestParameters(): string[] { return []; }

    constructor() { super(Object, false); }
}

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

// After the form reaches a valid state (isValid=true), changing a field to an invalid
// value must immediately flip isValid back to false — regardless of validateOn.
// This is the core correctness guarantee: isValid always reflects actual form validity.
describe('when making a valid form invalid by changing a field', given(a_command_form_context, context => {
    let capturedIsValid: boolean | undefined;

    const ContextCapture = () => {
        const ctx = useCommandFormContext();
        capturedIsValid = ctx.isValid;
        return React.createElement('div');
    };

    beforeEach(async () => {
        capturedIsValid = undefined;

        const result = render(
            React.createElement(
                CommandForm,
                {
                    command: TwoFieldCommand,
                    initialValues: { name: '', email: '' }
                },
                React.createElement(SimpleTextField, {
                    value: (c: TwoFieldCommand) => c.name,
                    title: 'Name',
                    'data-testid': 'name-input'
                }),
                React.createElement(SimpleTextField, {
                    value: (c: TwoFieldCommand) => c.email,
                    title: 'Email',
                    'data-testid': 'email-input'
                }),
                React.createElement(ContextCapture)
            ),
            { wrapper: context.createWrapper() }
        );

        // Step 1: type valid values into both fields so the form becomes valid.
        const nameInput = result.getByTestId('name-input') as HTMLInputElement;
        const emailInput = result.getByTestId('email-input') as HTMLInputElement;

        await act(async () => {
            fireEvent.change(nameInput, { target: { value: 'John Doe' } });
        });
        await act(async () => {
            fireEvent.change(emailInput, { target: { value: 'john@example.com' } });
        });

        // Wait for silent validation to confirm the form is valid before proceeding.
        await waitFor(() => { expect(capturedIsValid).toBe(true); }, { timeout: 2000 });

        // Step 2: change the email field to an invalid value.
        await act(async () => {
            fireEvent.change(emailInput, { target: { value: 'not-a-valid-email' } });
        });
    });

    it('should set isValid to false', async () => {
        await waitFor(() => { expect(capturedIsValid).toBe(false); }, { timeout: 2000 });
    });
}));
