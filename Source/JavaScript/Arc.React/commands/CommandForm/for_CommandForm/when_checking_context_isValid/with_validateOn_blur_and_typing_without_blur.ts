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

// Two required fields — both empty at start so silentValidationResult reports invalid on mount.
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

// With validateOn='blur' (the default), validateOn controls only ERROR DISPLAY, not
// validity checking. Silent validation runs on every onChange, so isValid always
// reflects the true form validity — even before any blur event occurs.
// This means:
//   • isValid is false when the form has invalid data (even before blurring)
//   • isValid becomes true as soon as all fields contain valid data (even without blur)
//   • Error messages are NOT displayed until blur (that is the validateOn='blur' effect)
describe("when checking context isValid with validateOn='blur' and typing without blur", given(a_command_form_context, context => {
    let capturedIsValid: boolean | undefined;

    const ContextCapture = () => {
        const ctx = useCommandFormContext();
        capturedIsValid = ctx.isValid;
        return React.createElement('div');
    };

    describe("and typing invalid values", () => {
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

            // Wait for silent init validation to set isValid = false.
            await waitFor(() => { expect(capturedIsValid).toBe(false); }, { timeout: 2000 });

            // Type invalid values — name too short, email malformed — no blur.
            const nameInput = result.getByTestId('name-input') as HTMLInputElement;
            const emailInput = result.getByTestId('email-input') as HTMLInputElement;
            await act(async () => {
                fireEvent.change(nameInput, { target: { value: 'Jo' } });
            });
            await act(async () => {
                fireEvent.change(emailInput, { target: { value: 'not-an-email' } });
            });
        });

        it("should keep isValid false because form data is actually invalid", async () => {
            await waitFor(() => { expect(capturedIsValid).toBe(false); }, { timeout: 2000 });
        });
    });

    describe("and typing valid values into all fields", () => {
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

            // Wait for silent init validation to set isValid = false.
            await waitFor(() => { expect(capturedIsValid).toBe(false); }, { timeout: 2000 });

            // Type valid values into BOTH fields — no blur needed.
            const nameInput = result.getByTestId('name-input') as HTMLInputElement;
            const emailInput = result.getByTestId('email-input') as HTMLInputElement;
            await act(async () => {
                fireEvent.change(nameInput, { target: { value: 'John Doe' } });
            });
            await act(async () => {
                fireEvent.change(emailInput, { target: { value: 'john@example.com' } });
            });
        });

        it("should set isValid to true because silent validation runs on every change", async () => {
            // validateOn='blur' only defers error DISPLAY — isValid updates immediately.
            await waitFor(() => { expect(capturedIsValid).toBe(true); }, { timeout: 2000 });
        });
    });
}));
