// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, render } from '@testing-library/react';
import { Command, CommandResult, type ICommandResult } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';
import { CommandForm, useCommandFormContext } from '../../../CommandForm.js';
import type { CommandFormValidationState } from '../../../commandFormValidationState.js';
import { a_command_form_context } from '../../given/a_command_form_context.js';

export const accepted = (): ICommandResult<unknown> => CommandResult.empty as ICommandResult<unknown>;

class ReportingCommand extends Command {
    readonly route = '/api/reporting';
    readonly propertyDescriptors: PropertyDescriptor[] = [new PropertyDescriptor('name', String, true)];

    name = 'Something';

    get requestParameters(): string[] {
        return [];
    }

    constructor() {
        super(Object, false);
    }
}

/**
 * Drives validation through the token seam and reads the validation metadata the form publishes,
 * so each rule of freshness and pending is observable without a field, a keystroke or a network.
 */
export class a_command_form_reporting_validation_state {
    private readonly _arc = new a_command_form_context();

    beginSilentValidation: (channel?: 'client' | 'server') => number = () => -1;
    applySilentValidationResult: (result: ICommandResult<unknown>, issue?: number) => boolean = () => false;
    setCommandValues: (values: object) => void = () => {};

    /** The validation metadata as of the last render. */
    validation: CommandFormValidationState | undefined;

    readonly Probe: React.FC;

    constructor() {
        this.Probe = () => {
            const commandForm = useCommandFormContext();
            this.beginSilentValidation = commandForm.beginSilentValidation;
            this.applySilentValidationResult = commandForm.setSilentValidationResult;
            this.setCommandValues = commandForm.setCommandValues as (values: object) => void;
            this.validation = commandForm.validation;
            return React.createElement('div');
        };
    }

    async renderForm() {
        const result = render(
            React.createElement(CommandForm, { command: ReportingCommand }, React.createElement(this.Probe)),
            { wrapper: this._arc.createWrapper() });
        await this.settle();
        return result;
    }

    async act(action: () => void) {
        await act(async () => action());
    }

    async settle() {
        await act(async () => {
            await new Promise(resolve => setTimeout(resolve, 0));
        });
    }
}
