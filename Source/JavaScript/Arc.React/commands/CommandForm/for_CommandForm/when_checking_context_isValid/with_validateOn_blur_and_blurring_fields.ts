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

// When the user blurs ALL fields and every value is valid, the onBlur handler fires
// real client validation (commandResult) and calls markUserInteracted() + setFieldValidity().
// At that point silentValidationResult is superseded by commandResult and isValid = true.
describe("when checking context isValid with validateOn='blur' after blurring all fields with valid values", given(a_command_form_context, context => {
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

        // Confirm form starts invalid.
        await waitFor(() => { expect(capturedIsValid).toBe(false); }, { timeout: 2000 });

        // Type valid values and blur both fields.
        const nameInput = result.getByTestId('name-input') as HTMLInputElement;
        const emailInput = result.getByTestId('email-input') as HTMLInputElement;

        fireEvent.change(nameInput, { target: { value: 'John Doe' } });
        fireEvent.blur(nameInput);
        fireEvent.change(emailInput, { target: { value: 'john@example.com' } });
        fireEvent.blur(emailInput);
    });

    it("should set isValid to true", async () => {
        await waitFor(() => {
            expect(capturedIsValid).toBe(true);
        }, { timeout: 2000 });
    });
}));

// Blurring only one field (while the other still has an empty/invalid value) runs
// validation that produces errors for the second field. With validateAllFieldsOnChange=true
// the full commandResult (including the email error) is stored, so isValid must remain false.
// Note: with the default per-field merge (validateAllFieldsOnChange=false), only touched-field
// errors are tracked, so blurring name alone would produce an empty-error commandResult and
// isValid would be true — this is intentional per-field UX, not a bug.
describe("when checking context isValid with validateOn='blur' and validateAllFieldsOnChange after blurring only the first field with a valid value", given(a_command_form_context, context => {
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
                    initialValues: { name: '', email: '' },
                    // All-fields mode: every blur shows all errors so commandResult
                    // reflects the full form state, not just touched fields.
                    validateAllFieldsOnChange: true
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

        await waitFor(() => { expect(capturedIsValid).toBe(false); }, { timeout: 2000 });

        // Only type + blur the name field; email remains empty.
        const nameInput = result.getByTestId('name-input') as HTMLInputElement;
        fireEvent.change(nameInput, { target: { value: 'John Doe' } });
        fireEvent.blur(nameInput);
    });

    it("should keep isValid as false because the other field is still invalid", async () => {
        // After the blur, commandResult is set with ALL field errors (including email).
        await waitFor(() => {
            expect(capturedIsValid).not.toBeUndefined();
        }, { timeout: 2000 });
        expect(capturedIsValid).toBe(false);
    });
}));
