// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { fireEvent, render, waitFor } from '@testing-library/react';
import { CommandForm, useCommandFormContext } from '../CommandForm.js';
import { asCommandFormField } from '../asCommandFormField.js';
import { Command, CommandValidator } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';
import { ValidationResultSeverity } from '@cratis/arc/validation';
import { a_command_form_context } from './given/a_command_form_context.js';
import { given } from '../../../given.js';

class WarningValidator extends CommandValidator<{ name: string }> {
    constructor() {
        super();
        this.ruleFor(c => c.name).notEmpty().withSeverity(ValidationResultSeverity.Warning);
    }
}

class WarningCommand extends Command {
    readonly route = '/api/test';
    readonly validation = new WarningValidator();
    readonly propertyDescriptors = [new PropertyDescriptor('name', String, true)];
    name = 'valid';
    get requestParameters(): string[] { return []; }
    constructor() { super(Object, false); }
}

class WarningPolicyCommand extends WarningCommand {
    readonly blockOnValidationSeverity = ValidationResultSeverity.Warning;
}

const NameField = asCommandFormField<{ value: string; onChange: (value: unknown) => void; onBlur?: () => void; invalid: boolean; required: boolean; errors: string[] }>(
    props => React.createElement('input', { value: props.value ?? '', onChange: props.onChange, onBlur: props.onBlur, 'data-testid': 'name' }),
    { defaultValue: '', extractValue: (event: unknown) => (event as React.ChangeEvent<HTMLInputElement>).target.value }
);

describe('when a command form has a warning-only rule', given(a_command_form_context, context => {
    let capturedIsValid: boolean | undefined;
    let validationResults: number | undefined;

    const Capture = () => {
        const form = useCommandFormContext();
        capturedIsValid = form.isValid;
        validationResults = form.commandResult?.validationResults.length;
        return React.createElement('div');
    };

    const mount = (command: typeof WarningCommand) => render(
        React.createElement(CommandForm, { command, initialValues: { name: 'valid' } },
            React.createElement(NameField, { value: (c: WarningCommand) => c.name, title: 'Name' }),
            React.createElement(Capture)),
        { wrapper: context.createWrapper() }
    );

    it('keeps the form valid after change and blur without a declared policy', async () => {
        const view = mount(WarningCommand);
        fireEvent.change(view.getByTestId('name'), { target: { value: '' } });
        fireEvent.blur(view.getByTestId('name'));
        await waitFor(() => {
            expect(validationResults).to.equal(1);
            expect(capturedIsValid).to.equal(true);
            expect(view.queryByText("'name' must not be empty.")).not.to.be.null;
        });
    });

    it('marks the form invalid when the warning is declared blocking', async () => {
        const view = mount(WarningPolicyCommand);
        fireEvent.change(view.getByTestId('name'), { target: { value: '' } });
        fireEvent.blur(view.getByTestId('name'));
        await waitFor(() => {
            expect(capturedIsValid).to.equal(false);
            expect(view.queryByText("'name' must not be empty.")).not.to.be.null;
        });
    });
}));
