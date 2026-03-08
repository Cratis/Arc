// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, waitFor } from '@testing-library/react';
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

const SimpleTextField = asCommandFormField<{ value: string; onChange: (value: unknown) => void; onBlur?: () => void; invalid: boolean; required: boolean; errors: string[] }>(
    (props) => {
        return React.createElement('input', {
            type: 'text',
            value: props.value,
            onChange: props.onChange,
            onBlur: props.onBlur,
            'data-testid': `text-input-${props.value}`
        });
    },
    {
        defaultValue: '',
        extractValue: (e: unknown) => (e as React.ChangeEvent<HTMLInputElement>).target.value
    }
);

describe("when validateOnInit is true with currentValues", given(a_command_form_context, context => {
    let result: ReturnType<typeof render>;
    let currentValues: { name: string; email: string };

    beforeEach(() => {
        currentValues = { name: 'ab', email: 'invalid' };
        
        result = render(
            React.createElement(
                CommandForm,
                { 
                    command: ValidatedTestCommand,
                    validateOnInit: true,
                    currentValues: currentValues
                },
                React.createElement(SimpleTextField, {
                    value: (c: ValidatedTestCommand) => c.name,
                    title: 'Name'
                }),
                React.createElement(SimpleTextField, {
                    value: (c: ValidatedTestCommand) => c.email,
                    title: 'Email'
                })
            ),
            { wrapper: context.createWrapper() }
        );
    });

    it("should validate on initialization in reactive mode", async () => {
        await waitFor(() => {
            const nameErrorElement = result.queryByText(/at least 3 characters/i);
            const emailErrorElement = result.queryByText(/valid email/i);
            return nameErrorElement !== null && emailErrorElement !== null;
        }, { timeout: 2000 });
    });

    it("should show both validation errors from invalid currentValues", async () => {
        await waitFor(() => {
            expect(result.queryByText(/at least 3 characters/i)).not.toBeNull();
            expect(result.queryByText(/valid email/i)).not.toBeNull();
        }, { timeout: 2000 });
    });
}));
