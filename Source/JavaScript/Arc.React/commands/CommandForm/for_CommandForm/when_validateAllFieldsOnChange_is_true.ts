// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, fireEvent, waitFor } from '@testing-library/react';
import { CommandForm } from '../CommandForm';
import { asCommandFormField } from '../asCommandFormField';
import { Command, CommandValidator } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

class ValidatedTestCommandValidator extends CommandValidator<ValidatedTestCommand> {
    constructor() {
        super();
        this.ruleFor(c => c.name).notEmpty().minLength(3);
        this.ruleFor(c => c.email).notEmpty().emailAddress();
    }
}

class ValidatedTestCommand extends Command {
    readonly route = '/api/test';
    readonly validation = new ValidatedTestCommandValidator();
    readonly propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('name', String, false),
        new PropertyDescriptor('email', String, false)
    ];

    name = '';
    email = '';

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

const createTextField = (testId: string) => asCommandFormField<{ value: string; onChange: (value: unknown) => void; onBlur?: () => void; invalid: boolean; required: boolean; errors: string[] }>(
    (props) => {
        return React.createElement('input', {
            type: 'text',
            value: props.value,
            onChange: props.onChange,
            onBlur: props.onBlur,
            'data-testid': testId
        });
    },
    {
        defaultValue: '',
        extractValue: (e: unknown) => (e as React.ChangeEvent<HTMLInputElement>).target.value
    }
);

const NameField = createTextField('name-input');
const EmailField = createTextField('email-input');

describe("when validateAllFieldsOnChange is true", given(a_command_form_context, context => {
    let result: ReturnType<typeof render>;

    beforeEach(() => {
        result = render(
            React.createElement(
                CommandForm,
                { 
                    command: ValidatedTestCommand,
                    validateOn: 'blur',
                    validateAllFieldsOnChange: true,
                    initialValues: { name: '', email: '' }
                },
                React.createElement(NameField, {
                    value: (c: ValidatedTestCommand) => c.name,
                    title: 'Name'
                }),
                React.createElement(EmailField, {
                    value: (c: ValidatedTestCommand) => c.email,
                    title: 'Email'
                })
            ),
            { wrapper: context.createWrapper() }
        );
    });

    it("should validate all fields when one field loses focus", async () => {
        const nameInput = result.getByTestId('name-input') as HTMLInputElement;
        const emailInput = result.getByTestId('email-input') as HTMLInputElement;
        
        // Change both fields but only blur the name field
        fireEvent.change(nameInput, { target: { value: 'ab' } });
        fireEvent.change(emailInput, { target: { value: 'invalid' } });
        fireEvent.blur(nameInput);
        
        // Wait for validation to complete - both fields should show errors
        await waitFor(() => {
            const nameError = result.queryByText(/at least 3 characters/i);
            const emailError = result.queryByText(/valid email/i);
            return nameError !== null && emailError !== null;
        }, { timeout: 2000 });
    });
}));
