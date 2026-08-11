// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, fireEvent, render } from '@testing-library/react';
import { Command, CommandResult, CommandValidator, type ICommandResult } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';
import { CommandForm, useCommandFormContext } from '../CommandForm';
import { asCommandFormField } from '../asCommandFormField';
import { a_command_form_fields_context } from './given/a_command_form_fields_context';
import { given } from '../../../given';

/**
 * The token decides what isValid says, and it has to decide what the screen says as well: a message
 * describing values the field no longer holds is as wrong rendered as it is in isValid, and it is
 * the half a reader notices. Nothing asserted on it, so the gate could be dropped and every spec
 * would stay green while a stale rejection sat under a field that no longer had anything wrong.
 *
 * Two keystrokes cannot produce the inversion: the per-keystroke run is client-side and
 * Command.validateClientSide is synchronous, so two of them resolve in the order they were issued
 * no matter what a spec does. The overtaking writer here is the one that genuinely can arrive out of
 * band - a downstream custom field writing its verdict straight through the context, with no token
 * to order it by, which is taken as current and makes everything still in flight stale.
 */

class OvertakenCommandValidator extends CommandValidator<OvertakenCommand> {
    constructor() {
        super();
        this.ruleFor(command => command.name).notEmpty();
    }
}

class OvertakenCommand extends Command {
    readonly route = '/api/overtaken';
    readonly validation = new OvertakenCommandValidator();
    readonly propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('name', String, true)
    ];

    name = 'Something';

    get requestParameters(): string[] {
        return [];
    }

    constructor() {
        super(Object, false);
    }
}

const TextField = asCommandFormField<{ value: string; onChange: (value: unknown) => void; onBlur?: () => void; invalid: boolean; required: boolean; errors: string[] }>(
    (props) => React.createElement('input', {
        type: 'text',
        value: props.value,
        onChange: props.onChange,
        onBlur: props.onBlur,
        'data-testid': 'name-input'
    }),
    {
        defaultValue: '',
        extractValue: (event: unknown) => (event as React.ChangeEvent<HTMLInputElement>).target.value
    }
);

describe('when a change validation is overtaken', given(a_command_form_fields_context, context => {
    let result: ReturnType<typeof render>;
    let isValid: boolean | undefined;
    let messages: string[] = [];
    let applySilentValidationResult: (validationResult: ICommandResult<unknown>, issue?: number) => boolean;

    const Probe = () => {
        const commandForm = useCommandFormContext();
        applySilentValidationResult = commandForm.setSilentValidationResult;
        isValid = commandForm.isValid;
        messages = (commandForm.commandResult?.validationResults ?? []).map(validationResult => validationResult.message);
        return React.createElement('div');
    };

    const settle = async (milliseconds = 0) => {
        await act(async () => {
            await new Promise(resolve => setTimeout(resolve, milliseconds));
        });
    };

    const emptyTheFieldAndThen = async (whileTheRunIsInFlight?: () => void) => {
        isValid = undefined;
        messages = [];

        result = render(
            React.createElement(
                CommandForm,
                { command: OvertakenCommand, validateOn: 'change' },
                React.createElement(TextField, { value: (command: OvertakenCommand) => command.name }),
                React.createElement(Probe)),
            { wrapper: context.createWrapper() });

        await settle();

        // Claims a token and suspends on its await. Nothing has drained yet - microtasks only run
        // once this synchronous block ends - so whatever happens next happens while it is in flight.
        fireEvent.change(result.getByTestId('name-input'), { target: { value: '' } });
        if (whileTheRunIsInFlight) act(whileTheRunIsInFlight);

        await settle();
    };

    afterEach(async () => {
        if (result) result.unmount();
        await settle();
    });

    describe('and a custom field writes a verdict while it is in flight', () => {
        beforeEach(async () => await emptyTheFieldAndThen(
            () => applySilentValidationResult(CommandResult.empty as ICommandResult<unknown>)));

        it('should not display the message the overtaken run carried', () => messages.join(' ').should.not.contain('must not be empty'));

        it('should keep the verdict the custom field wrote', () => isValid!.should.be.true);
    });

    // The control. Same keystroke, same field, nothing overtaking it - so the message this run
    // carries is the current one and belongs on screen. Without this the assertion above would hold
    // just as well for a form that never renders a message at all.
    describe('and nothing overtakes it', () => {
        beforeEach(async () => await emptyTheFieldAndThen());

        it('should display the message the run carried', () => messages.join(' ').should.contain('must not be empty'));

        it('should apply the verdict the run carried', () => isValid!.should.be.false);
    });
}));
