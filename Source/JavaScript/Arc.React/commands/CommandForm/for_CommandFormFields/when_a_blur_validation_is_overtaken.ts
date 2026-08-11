// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, fireEvent, render } from '@testing-library/react';
import { Command } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';
import { vi } from 'vitest';
import { CommandForm, useCommandFormContext } from '../CommandForm';
import { asCommandFormField } from '../asCommandFormField';
import { a_command_form_fields_context } from './given/a_command_form_fields_context';
import { given } from '../../../given';

/**
 * The blur path with autoServerValidate is the run most likely to be overtaken: it crosses the
 * network, and a keystroke issued after it is answered client-side without leaving the browser. So
 * the interleaving here is real rather than arranged - the request is genuinely still in flight when
 * the keystroke lands, which the recorded order below asserts rather than assumes.
 *
 * What is being pinned is the message, not the validity: the blur result is discarded from isValid
 * by the same token either way, but the message it carries describes the value that was in the field
 * before the keystroke, and rendering it puts a rejection under a field that no longer holds
 * anything wrong.
 */

const CORRELATION_ID = '2f1e0d9c-8b7a-4650-9d3c-1a2b3c4d5e6f';
const REJECTION = 'Name is already taken';

class BlurCommand extends Command {
    readonly route = '/api/blur';

    // No client validator, so validate() cannot answer without reaching the server - a run that
    // never leaves the browser cannot be in flight long enough to be overtaken by anything.
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

const responseBody = (messages: string[]) => JSON.stringify({
    correlationId: CORRELATION_ID,
    isSuccess: messages.length === 0,
    isAuthorized: true,
    isValid: messages.length === 0,
    hasExceptions: false,
    validationResults: messages.map(message => ({ severity: 2, message, members: ['name'], state: {} })),
    exceptionMessages: [],
    exceptionStackTrace: '',
    authorizationFailureReason: ''
});

describe('when a blur validation is overtaken', given(a_command_form_fields_context, context => {
    let result: ReturnType<typeof render>;
    let isValid: boolean | undefined;
    let messages: string[] = [];
    let recorded: string[] = [];

    const Probe = () => {
        const commandForm = useCommandFormContext();
        isValid = commandForm.isValid;
        messages = (commandForm.commandResult?.validationResults ?? []).map(validationResult => validationResult.message);
        return React.createElement('div');
    };

    const settle = async (milliseconds = 0) => {
        await act(async () => {
            await new Promise(resolve => setTimeout(resolve, milliseconds));
        });
    };

    // Waits on what actually happened rather than on a clock, so the keystroke lands inside the
    // window the request is out for however long the round trip really takes.
    const until = async (hasHappened: () => boolean) => {
        for (let attempt = 0; attempt < 200 && !hasHappened(); attempt++) {
            await settle(10);
        }
    };

    const blurTheFieldAndThen = async (whileTheRequestIsInFlight?: () => void) => {
        isValid = undefined;
        messages = [];
        recorded = [];
        let validateRequests = 0;

        vi.spyOn(global, 'fetch').mockImplementation(async (url) => {
            if (!url.toString().includes('/validate')) {
                return new Response(responseBody([]), { status: 200, headers: { 'Content-Type': 'application/json' } });
            }

            // The first is the run the form makes on mount; the second is the blur.
            const isBlurRun = ++validateRequests === 2;
            if (!isBlurRun) {
                return new Response(responseBody([]), { status: 200, headers: { 'Content-Type': 'application/json' } });
            }

            recorded.push('blur request issued');
            await new Promise(resolve => setTimeout(resolve, 200));
            recorded.push('blur request answered');
            return new Response(responseBody([REJECTION]), { status: 200, headers: { 'Content-Type': 'application/json' } });
        });

        result = render(
            React.createElement(
                CommandForm,
                {
                    command: BlurCommand,
                    validateOn: 'blur',
                    autoServerValidate: true,
                    // Parks the form's own throttled server run outside this window. It writes to the
                    // same slot, so letting it land would repair the verdict on its own.
                    autoServerValidateThrottle: 5000
                },
                React.createElement(TextField, { value: (command: BlurCommand) => command.name }),
                React.createElement(Probe)),
            { wrapper: context.createWrapper() });

        await settle(20);

        fireEvent.blur(result.getByTestId('name-input'));
        await until(() => recorded.includes('blur request issued'));

        if (whileTheRequestIsInFlight) whileTheRequestIsInFlight();

        await until(() => recorded.includes('blur request answered'));
        await settle(50);
    };

    afterEach(async () => {
        vi.restoreAllMocks();
        if (result) result.unmount();
        await settle(20);
    });

    describe('and a keystroke is answered while the request is in flight', () => {
        beforeEach(async () => await blurTheFieldAndThen(() => {
            recorded.push('keystroke');
            fireEvent.change(result.getByTestId('name-input'), { target: { value: 'Updated' } });
        }));

        // Both were genuinely in flight together. Without this the assertions below could hold
        // because the runs never overlapped at all.
        it('should have answered the request after the keystroke', () =>
            recorded.should.deep.equal(['blur request issued', 'keystroke', 'blur request answered']));

        it('should not display the message the overtaken request carried', () => messages.join(' ').should.not.contain(REJECTION));

        it('should keep the verdict of the run that overtook it', () => isValid!.should.be.true);
    });

    // The control. Same request, same rejection, nothing overtaking it - so it is the current
    // verdict and belongs on screen. Without this the assertion above would hold just as well for a
    // form that never renders a server rejection at all.
    describe('and nothing overtakes it', () => {
        beforeEach(async () => await blurTheFieldAndThen());

        it('should display the message the request carried', () => messages.join(' ').should.contain(REJECTION));

        it('should apply the verdict the request carried', () => isValid!.should.be.false);
    });
}));
