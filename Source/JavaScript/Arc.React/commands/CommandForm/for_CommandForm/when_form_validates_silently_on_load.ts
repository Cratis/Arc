// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, waitFor } from '@testing-library/react';
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

const SimpleTextField = asCommandFormField<{ value: string; onChange: (value: unknown) => void; onBlur?: () => void; invalid: boolean; required: boolean; errors: string[] }>(
    (props) => {
        return React.createElement('input', {
            type: 'text',
            value: props.value,
            onChange: props.onChange,
            onBlur: props.onBlur,
            'data-testid': 'text-input'
        });
    },
    {
        defaultValue: '',
        extractValue: (e: unknown) => (e as React.ChangeEvent<HTMLInputElement>).target.value
    }
);

describe("when form validates silently on load with invalid initial values and validateOnInit is false", given(a_command_form_context, context => {
    let result: ReturnType<typeof render>;
    let capturedIsValid: boolean | undefined;

    const ContextCapture = () => {
        const ctx = useCommandFormContext();
        capturedIsValid = ctx.isValid;
        return React.createElement('div');
    };

    beforeEach(() => {
        capturedIsValid = undefined;

        result = render(
            React.createElement(
                CommandForm,
                {
                    command: ValidatedCommand,
                    validateOnInit: false,
                    initialValues: { name: '', email: '' }
                },
                React.createElement(SimpleTextField, {
                    value: (c: ValidatedCommand) => c.name,
                    title: 'Name'
                }),
                React.createElement(SimpleTextField, {
                    value: (c: ValidatedCommand) => c.email,
                    title: 'Email'
                }),
                React.createElement(ContextCapture)
            ),
            { wrapper: context.createWrapper() }
        );
    });

    it("should report isValid as false in context after silent validation", async () => {
        await waitFor(() => {
            return capturedIsValid === false;
        }, { timeout: 2000 });
    });

    it("should not render any validation error messages", async () => {
        await waitFor(() => {
            return capturedIsValid === false;
        }, { timeout: 2000 });

        expect(result.queryByText(/cannot be empty/i)).toBeNull();
        expect(result.queryByText(/at least/i)).toBeNull();
        expect(result.queryByText(/valid email/i)).toBeNull();
    });
}));

describe("when form validates silently on load with valid initial values and validateOnInit is false", given(a_command_form_context, context => {
    let capturedIsValid: boolean | undefined;

    const ContextCapture = () => {
        const ctx = useCommandFormContext();
        capturedIsValid = ctx.isValid;
        return React.createElement('div');
    };

    beforeEach(() => {
        capturedIsValid = undefined;

        render(
            React.createElement(
                CommandForm,
                {
                    command: ValidatedCommand,
                    validateOnInit: false,
                    initialValues: { name: 'John Doe', email: 'john@example.com' }
                },
                React.createElement(SimpleTextField, {
                    value: (c: ValidatedCommand) => c.name,
                    title: 'Name'
                }),
                React.createElement(SimpleTextField, {
                    value: (c: ValidatedCommand) => c.email,
                    title: 'Email'
                }),
                React.createElement(ContextCapture)
            ),
            { wrapper: context.createWrapper() }
        );
    });

    it("should report isValid as true in context after silent validation", async () => {
        await waitFor(() => {
            return capturedIsValid === true;
        }, { timeout: 2000 });
    });
}));

describe("when form validates silently on load with invalid initial values and validateOnInit is true", given(a_command_form_context, context => {
    let result: ReturnType<typeof render>;

    beforeEach(() => {
        result = render(
            React.createElement(
                CommandForm,
                {
                    command: ValidatedCommand,
                    validateOnInit: true,
                    initialValues: { name: '', email: '' }
                },
                React.createElement(SimpleTextField, {
                    value: (c: ValidatedCommand) => c.name,
                    title: 'Name'
                }),
                React.createElement(SimpleTextField, {
                    value: (c: ValidatedCommand) => c.email,
                    title: 'Email'
                })
            ),
            { wrapper: context.createWrapper() }
        );
    });

    it("should render validation error messages", async () => {
        await waitFor(() => {
            const hasError = result.queryByText(/cannot be empty/i) !== null ||
                result.queryByText(/at least/i) !== null;
            return hasError;
        }, { timeout: 2000 });
    });
}));
