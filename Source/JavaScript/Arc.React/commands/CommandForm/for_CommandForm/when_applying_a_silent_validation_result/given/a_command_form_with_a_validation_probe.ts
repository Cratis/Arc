// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, render } from '@testing-library/react';
import { Command, CommandResult, type ICommandResult } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';
import { ValidationResult } from '@cratis/arc/validation';
import { ValidationResultSeverity } from '@cratis/arc/validation';
import { CommandForm, useCommandFormContext } from '../../../CommandForm';
import { a_command_form_context } from '../../given/a_command_form_context';

/** A verdict a validation run can come back with, distinguishable from every other one. */
export const REJECTION = 'Name is already taken';

export const rejected = (): ICommandResult<unknown> => CommandResult.validationFailed([
    new ValidationResult(ValidationResultSeverity.Error, REJECTION, ['name'], null)
]) as ICommandResult<unknown>;

export const accepted = (): ICommandResult<unknown> => CommandResult.empty as ICommandResult<unknown>;

class ProbedCommand extends Command {
    readonly route = '/api/probed';
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

/**
 * Drives the token seam the way a downstream custom field does - straight through the context -
 * rather than through a field, a keystroke and a network round trip.
 *
 * The seam answers with a boolean saying whether the result was applied, and a caller that only
 * watches isValid cannot tell "discarded" apart from "applied, and it happened to agree". Only a
 * caller holding the boolean can, which is what makes each half of the guard fail on its own.
 */
export class a_command_form_with_a_validation_probe {
    private readonly _arc = new a_command_form_context();

    /** Claims a token for a validation about to be issued. */
    beginSilentValidation: () => number = () => -1;

    /** Hands a result back, with the token it was issued under, or without one at all. */
    applySilentValidationResult: (result: ICommandResult<unknown>, issue?: number) => boolean = () => false;

    /** The validity the form reports, as of the last render. */
    isValid: boolean | undefined;

    readonly Probe: React.FC;

    constructor() {
        this.Probe = () => {
            const commandForm = useCommandFormContext();
            this.beginSilentValidation = commandForm.beginSilentValidation;
            this.applySilentValidationResult = commandForm.setSilentValidationResult;
            this.isValid = commandForm.isValid;
            return React.createElement('div');
        };
    }

    /**
     * Renders the form and lets the validation it runs on mount finish, so that every token the
     * specs then claim is genuinely later than everything the form itself issued.
     */
    async renderForm() {
        this.isValid = undefined;
        const result = render(
            React.createElement(CommandForm, { command: ProbedCommand }, React.createElement(this.Probe)),
            { wrapper: this._arc.createWrapper() });

        await this.settle();
        return result;
    }

    async settle(milliseconds = 0) {
        await act(async () => {
            await new Promise(resolve => setTimeout(resolve, milliseconds));
        });
    }
}
