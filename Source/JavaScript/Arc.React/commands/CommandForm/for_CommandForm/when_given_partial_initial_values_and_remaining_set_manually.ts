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

class ValidatedCommandValidator extends CommandValidator<ValidatedCommand> {
    constructor() {
        super();
        this.ruleFor(c => c.name).notEmpty().minLength(3);
        this.ruleFor(c => c.email).notEmpty().emailAddress();
    }
}

class ValidatedCommand extends Command {
    readonly route = '/api/test';
    readonly validation = new ValidatedCommandValidator();
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

const SimpleTextField = asCommandFormField<{ value: string; onChange: (value: unknown) => void; onBlur?: () => void; invalid: boolean; required: boolean; errors: string[]; 'data-testid'?: string }>(
    (props) => {
        return React.createElement('input', {
            type: 'text',
            value: props.value,
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

describe("when given partial initial values and remaining set manually", given(a_command_form_context, context => {
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
                    command: ValidatedCommand,
                    initialValues: { name: 'John Doe', email: '' }
                },
                React.createElement(SimpleTextField, {
                    value: (c: ValidatedCommand) => c.name,
                    title: 'Name',
                    'data-testid': 'name-input'
                }),
                React.createElement(SimpleTextField, {
                    value: (c: ValidatedCommand) => c.email,
                    title: 'Email',
                    'data-testid': 'email-input'
                }),
                React.createElement(ContextCapture)
            ),
            { wrapper: context.createWrapper() }
        );

        const emailInput = result.getByTestId('email-input') as HTMLInputElement;
        fireEvent.change(emailInput, { target: { value: 'john@example.com' } });
    });

    it("should be valid", async () => {
        await waitFor(() => {
            return capturedIsValid === true;
        }, { timeout: 2000 });
    });
}));
