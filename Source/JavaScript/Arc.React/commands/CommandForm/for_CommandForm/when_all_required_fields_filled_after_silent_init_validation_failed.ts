// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, fireEvent, waitFor } from '@testing-library/react';
import { CommandForm, useCommandFormContext } from '../CommandForm';
import { asCommandFormField } from '../asCommandFormField';
import { Command, CommandValidator } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

// Command where ALL fields are required (isOptional = false) and no initial values are provided.
// The class defaults leave all fields as undefined, which triggers the silent init validation to
// fail immediately on mount. This reproduces the deadlock where silentValidationResult locks
// isValid = false even after the user has filled every field via onChange.
class RequiredFieldsCommandValidator extends CommandValidator<RequiredFieldsCommand> {
    constructor() {
        super();
        this.ruleFor(c => c.name).notEmpty().minLength(3);
        this.ruleFor(c => c.email).notEmpty().emailAddress();
    }
}

class RequiredFieldsCommand extends Command {
    readonly route = '/api/test';
    readonly validation = new RequiredFieldsCommandValidator();
    readonly propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('name', String, false),
        new PropertyDescriptor('email', String, false)
    ];

    // Fields left as undefined intentionally — no initialValues will be provided,
    // so the silent validation on mount sees undefined for both properties and fails
    // validateRequiredProperties(), locking isValid = false.
    name?: string;
    email?: string;

    get properties(): string[] {
        return ['name', 'email'];
    }

    get requestParameters(): string[] {
        return [];
    }

    constructor() {
        super(Object, false);
    }
}

const SimpleTextField = asCommandFormField<{ value: string; onChange: (value: unknown) => void; onBlur?: () => void; invalid: boolean; required: boolean; errors: string[]; 'data-testid'?: string }>(
    (props) => {
        return React.createElement('input', {
            type: 'text',
            value: props.value ?? '',
            onChange: props.onChange,
            onBlur: props.onBlur,
            'data-testid': props['data-testid']
        });
    },
    {
        defaultValue: '',
        extractValue: (e: unknown) => (e as React.ChangeEvent<HTMLInputElement>).target.value
    }
);

describe("when all required fields are filled by the user after silent init validation failed", given(a_command_form_context, context => {
    let capturedIsValid: boolean | undefined;

    const ContextCapture = () => {
        const ctx = useCommandFormContext();
        capturedIsValid = ctx.isValid;
        return React.createElement('div');
    };

    beforeEach(async () => {
        capturedIsValid = undefined;

        // No initialValues — all fields start as undefined on the command instance.
        // The silent init validation will call validateRequiredProperties() and fail,
        // setting silentValidationResult with errors for both name and email.
        const result = render(
            React.createElement(
                CommandForm,
                {
                    command: RequiredFieldsCommand
                    // validateOn defaults to 'blur', so onChange alone does NOT update commandResult.
                    // This is the scenario where the deadlock occurs: silentValidationResult is set
                    // to a failing result, and commandResult is never updated to override it.
                },
                React.createElement(SimpleTextField, {
                    value: (c: RequiredFieldsCommand) => c.name,
                    title: 'Name',
                    'data-testid': 'name-input'
                }),
                React.createElement(SimpleTextField, {
                    value: (c: RequiredFieldsCommand) => c.email,
                    title: 'Email',
                    'data-testid': 'email-input'
                }),
                React.createElement(ContextCapture)
            ),
            { wrapper: context.createWrapper() }
        );

        // Wait for the silent init validation to complete and record isValid = false.
        await waitFor(() => { expect(capturedIsValid).toBe(false); }, { timeout: 2000 });

        // Simulate the user filling every required field via onChange + blur.
        // With validateOn = 'blur' (default) the onChange alone does NOT trigger client
        // validation, so only setCommandValues is called there. The blur fires the real
        // validation run which sets commandResult and populates fieldValidities, finally
        // replacing silentValidationResult as the validity authority.
        const nameInput = result.getByTestId('name-input') as HTMLInputElement;
        const emailInput = result.getByTestId('email-input') as HTMLInputElement;

        fireEvent.change(nameInput, { target: { value: 'John Doe' } });
        fireEvent.blur(nameInput);
        fireEvent.change(emailInput, { target: { value: 'john@example.com' } });
        fireEvent.blur(emailInput);
    });

    // Once the user has typed valid values AND blurred each field, real validation
    // (commandResult) has run and replaced the stale silentValidationResult.
    // isValid must now reflect the true state of the form — all fields are valid.
    it("should be valid", async () => {
        await waitFor(() => {
            expect(capturedIsValid).toBe(true);
        }, { timeout: 2000 });
    });
}));
