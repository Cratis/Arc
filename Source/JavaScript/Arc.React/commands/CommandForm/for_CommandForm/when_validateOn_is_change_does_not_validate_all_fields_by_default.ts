// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, fireEvent } from '@testing-library/react';
import { CommandForm } from '../CommandForm';
import { asCommandFormField } from '../asCommandFormField';
import { Command, CommandValidator } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

class SimpleCommandValidator extends CommandValidator<SimpleCommand> {
    constructor() {
        super();
        this.ruleFor(c => c.name).notEmpty().minLength(3);
        this.ruleFor(c => c.email).notEmpty().emailAddress();
    }
}

class SimpleCommand extends Command {
    readonly route = '/api/test';
    readonly validation = new SimpleCommandValidator();
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

const SimpleTextField = asCommandFormField<{ value: string; onChange: (value: unknown) => void; onBlur?: () => void; invalid: boolean; required: boolean; errors: string[]; title?: string }>(
    (props) => {
        return React.createElement('input', {
            type: 'text',
            value: props.value,
            onChange: props.onChange,
            onBlur: props.onBlur,
            placeholder: props.title
        });
    },
    {
        defaultValue: '',
        extractValue: (e: unknown) => (e as React.ChangeEvent<HTMLInputElement>).target.value
    }
);

describe("when validateOn is change does not validate all fields by default", given(a_command_form_context, context => {
    let result: ReturnType<typeof render>;

    beforeEach(async () => {
        result = render(
            React.createElement(
                CommandForm,
                { 
                    command: SimpleCommand,
                    validateOn: 'change'
                },
                React.createElement(SimpleTextField, {
                    value: (c: SimpleCommand) => c.name,
                    title: 'Name'
                }),
                React.createElement(SimpleTextField, {
                    value: (c: SimpleCommand) => c.email,
                    title: 'Email'
                })
            ),
            { wrapper: context.createWrapper() }
        );
    });

    it("should only show validation error for the field that changed", async () => {
        const nameInput = result.getByPlaceholderText('Name') as HTMLInputElement;
        
        // Type invalid value in name field
        fireEvent.change(nameInput, { target: { value: 'ab' } });
        
        // Wait for validation
        await new Promise(resolve => setTimeout(resolve, 100));
        
        // Name error should appear
        const nameError = result.queryByText(/at least 3 characters/i);
        expect(nameError).not.toBeNull();
        
        // Email error should NOT appear (email field not touched)
        const emailEmptyError = result.queryByText(/cannot be empty/i);
        expect(emailEmptyError).toBeNull();
    });

    it("should preserve errors from other fields when a field changes", async () => {
        const nameInput = result.getByPlaceholderText('Name') as HTMLInputElement;
        const emailInput = result.getByPlaceholderText('Email') as HTMLInputElement;
        
        // Type invalid value in name field
        fireEvent.change(nameInput, { target: { value: 'ab' } });
        await new Promise(resolve => setTimeout(resolve, 100));
        
        // Type invalid value in email field
        fireEvent.change(emailInput, { target: { value: 'invalid' } });
        await new Promise(resolve => setTimeout(resolve, 100));
        
        // Both errors should be present
        expect(result.queryByText(/at least 3 characters/i)).not.toBeNull();
        expect(result.queryByText(/valid email/i)).not.toBeNull();
        
        // Fix name field
        fireEvent.change(nameInput, { target: { value: 'Valid Name' } });
        await new Promise(resolve => setTimeout(resolve, 100));
        
        // Name error should be gone
        expect(result.queryByText(/at least 3 characters/i)).toBeNull();
        
        // Email error should still be present
        expect(result.queryByText(/valid email/i)).not.toBeNull();
    });

    it("should update field error when field value changes", async () => {
        const nameInput = result.getByPlaceholderText('Name') as HTMLInputElement;
        
        // Type invalid value
        fireEvent.change(nameInput, { target: { value: 'ab' } });
        await new Promise(resolve => setTimeout(resolve, 100));
        
        // Error should appear 
        expect(result.queryByText(/at least 3 characters/i)).not.toBeNull();
        
        // Fix the value
        fireEvent.change(nameInput, { target: { value: 'Valid Name' } });
        await new Promise(resolve => setTimeout(resolve, 100));
        
        // Error should disappear
        expect(result.queryByText(/at least 3 characters/i)).toBeNull();
    });
}));
