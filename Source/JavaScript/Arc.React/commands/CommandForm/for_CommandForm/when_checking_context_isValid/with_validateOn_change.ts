// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, fireEvent, waitFor } from '@testing-library/react';
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

// With validateOn='change', validation fires on every keystroke. markUserInteracted()
// and setFieldValidity() are therefore called inside the onChange handler. isValid
// must track the actual per-field validity state: false while any field is invalid,
// true once all fields pass their rules.
describe("when checking context isValid with validateOn='change' after typing an invalid value", given(a_command_form_context, context => {
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
                    validateOn: 'change',
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

        // Type a value into name that is too short (fails minLength(3)), leave email empty.
        const nameInput = result.getByTestId('name-input') as HTMLInputElement;
        fireEvent.change(nameInput, { target: { value: 'ab' } });
    });

    it("should keep isValid as false because name is too short and email is empty", async () => {
        await waitFor(() => {
            expect(capturedIsValid).not.toBeUndefined();
        }, { timeout: 2000 });
        expect(capturedIsValid).toBe(false);
    });
}));

describe("when checking context isValid with validateOn='change' after typing valid values into all fields", given(a_command_form_context, context => {
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
                    validateOn: 'change',
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

        const nameInput = result.getByTestId('name-input') as HTMLInputElement;
        const emailInput = result.getByTestId('email-input') as HTMLInputElement;

        // Type valid values into both fields.
        fireEvent.change(nameInput, { target: { value: 'John Doe' } });
        fireEvent.change(emailInput, { target: { value: 'john@example.com' } });
    });

    it("should set isValid to true", async () => {
        await waitFor(() => {
            expect(capturedIsValid).toBe(true);
        }, { timeout: 2000 });
    });
}));
