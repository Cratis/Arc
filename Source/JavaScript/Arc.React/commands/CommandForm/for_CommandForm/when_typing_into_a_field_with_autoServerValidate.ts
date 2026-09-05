// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, fireEvent, act } from '@testing-library/react';
import { Command, CommandValidator } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';
import { vi } from 'vitest';
import { CommandForm } from '../CommandForm';
import { asCommandFormField } from '../asCommandFormField';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

/**
 * Counts the requests a form actually issues while someone types. Every other spec around
 * autoServerValidate asserts a loose upper bound, which is why the per-keystroke round trip could sit
 * under them unnoticed. These count exactly, and they count the same way for every throttle value, so
 * a throttle that governs the typing path would move the numbers and one that does not cannot.
 */

class TypingCommandValidator extends CommandValidator<TypingCommand> {
    constructor() {
        super();
        this.ruleFor(c => c.name).notEmpty();
    }
}

class TypingCommand extends Command {
    readonly route = '/api/typing';
    readonly validation = new TypingCommandValidator();
    readonly propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('name', String, false)
    ];

    name = '';

    get properties(): string[] {
        return ['name'];
    }

    get requestParameters(): string[] {
        return [];
    }

    constructor() {
        super(Object, false);
    }
}

const TextField = asCommandFormField<{ value: string; onChange: (value: unknown) => void; onBlur?: () => void; invalid: boolean; required: boolean; errors: string[]; title?: string }>(
    (props) => React.createElement('input', {
        type: 'text',
        value: props.value,
        onChange: props.onChange,
        onBlur: props.onBlur,
        placeholder: props.title
    }),
    {
        defaultValue: '',
        extractValue: (e: unknown) => (e as React.ChangeEvent<HTMLInputElement>).target.value
    }
);

const TYPED = 'ABCDEFGHIJ';

describe('when typing into a field with autoServerValidate', given(a_command_form_context, context => {
    let result: ReturnType<typeof render>;
    let validateRequests: number;

    const settle = async (milliseconds: number) => {
        await act(async () => {
            await new Promise(resolve => setTimeout(resolve, milliseconds));
        });
    };

    const typeTenCharactersWithThrottle = async (throttle: number | undefined) => {
        validateRequests = 0;
        vi.spyOn(global, 'fetch').mockImplementation(async (url) => {
            if (url.toString().includes('/validate')) validateRequests++;
            return new Response(JSON.stringify({}), { status: 200, headers: { 'Content-Type': 'application/json' } });
        });

        result = render(
            React.createElement(
                CommandForm,
                { command: TypingCommand, autoServerValidate: true, autoServerValidateThrottle: throttle, validateOn: 'change' },
                React.createElement(TextField, { value: (c: TypingCommand) => c.name, title: 'Name' })),
            { wrapper: context.createWrapper() });

        const input = result.getByPlaceholderText('Name') as HTMLInputElement;

        // One event per character, the way a person types.
        for (let i = 1; i <= TYPED.length; i++) {
            fireEvent.change(input, { target: { value: TYPED.slice(0, i) } });
            await settle(0);
        }

        // Long enough for any throttled work to have fired.
        await settle(800);
    };

    afterEach(async () => {
        vi.restoreAllMocks();
        if (result) result.unmount();
        await settle(50);
    });

    describe('and the throttle is left at its default', () => {
        beforeEach(async () => await typeTenCharactersWithThrottle(undefined));

        // Ten keystrokes, one request: the trailing one, once the typing stopped.
        it('should issue one request for the whole burst', () => validateRequests.should.equal(1));
    });

    describe('and the throttle is longer than the burst', () => {
        beforeEach(async () => await typeTenCharactersWithThrottle(2000));

        // The prop has to actually govern the typing path, so raising it past the settle window
        // must leave nothing at all to count.
        it('should issue no request at all', () => validateRequests.should.equal(0));
    });
}));
