// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { Command } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';
import { CommandForm, useCommandInstance, useCommandFormContext, type CommandFormProps } from '../../../CommandForm';
import { a_command_form_context } from '../../given/a_command_form_context';

/** What the command class itself declares, and therefore what survives when nothing supplies a value. */
export const NAME_FROM_THE_COMMAND_CLASS = 'Name from the command class';

/** What the asynchronous lookup behind currentValues eventually answers with. */
export const NAME_FROM_THE_LOOKUP = 'Name from the lookup';

/**
 * Required - isOptional is false - so that losing the value has a consequence Command can object to:
 * validateRequiredProperties() rejects an undefined value and the form reads as invalid. The
 * TestCommand these specs sit next to declares every property optional, which would let a wiped
 * property pass validation and make every assertion below hold for the wrong reason.
 *
 * No client validator, so the only rule in play is the required one and the reason a run failed is
 * never ambiguous.
 */
export class RequiredNameCommand extends Command {
    readonly route = '/api/required-name';
    readonly propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('name', String, false)
    ];

    name: string | undefined = NAME_FROM_THE_COMMAND_CLASS;

    get requestParameters(): string[] {
        return [];
    }

    constructor() {
        super(Object, false);
    }
}

/** The layers a spec here seeds from, and whether the form is asked to show what it decided. */
export type SeedingOptions = Pick<CommandFormProps<RequiredNameCommand>, 'initialValues' | 'currentValues' | 'validateOnInit'>;

export class a_command_form_with_a_required_property {
    private readonly _arc = new a_command_form_context();

    /** The live command instance the form is bound to, as of the last render. */
    commandInstance: RequiredNameCommand | undefined;

    /** The validity the form reports, as of the last render. */
    isValid: boolean | undefined;

    /**
     * The messages the form has actually decided on, as of the last render.
     *
     * isValid is false before the first silent validation has answered as well as after a failed
     * one, so it cannot on its own tell "rejected" apart from "has not run yet" - asserting it is
     * false says nothing. These are only ever populated by a run that completed.
     */
    validationMessages: string[] = [];

    /**
     * Reads what the form actually holds, from the command instance.
     *
     * Deliberately not from the DOM: a field substitutes its own defaultValue - typically an empty
     * string - for an undefined command value, so the rendered input reads identically for a value
     * that was wiped and one that was never seeded, and an assertion on it would hold either way.
     */
    readonly Probe: React.FC;

    constructor() {
        this.Probe = () => {
            const commandForm = useCommandFormContext<RequiredNameCommand>();
            this.commandInstance = useCommandInstance<RequiredNameCommand>();
            this.isValid = commandForm.isValid;
            this.validationMessages = (commandForm.commandResult?.validationResults ?? []).map(validationResult => validationResult.message);
            return React.createElement('div');
        };
    }

    formWith(props: SeedingOptions, ...children: React.ReactNode[]): React.ReactElement {
        return React.createElement(
            CommandForm,
            { command: RequiredNameCommand, ...props },
            React.createElement(this.Probe),
            ...children);
    }

    renderForm(props: SeedingOptions, ...children: React.ReactNode[]) {
        this.commandInstance = undefined;
        this.isValid = undefined;
        this.validationMessages = [];
        return render(this.formWith(props, ...children), { wrapper: this._arc.createWrapper() });
    }
}
