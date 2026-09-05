// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, fireEvent, render } from '@testing-library/react';
import { Command } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';
import { vi } from 'vitest';
import { CommandForm, useCommandFormContext } from '../CommandForm';
import { asCommandFormField } from '../asCommandFormField';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

/**
 * The two runs a typing burst actually produces, racing each other: the throttled server round trip
 * the form makes once the typing settles, and the client-side run the next keystroke issues. They
 * are the pair furthest apart in cost - one crosses the network, the other never leaves the browser
 * - so the keystroke routinely answers first, and the round trip lands afterwards describing a value
 * that is already gone.
 *
 * Both halves of that matter. The verdict is one, and the message the round trip carries is the
 * other: it is written to the form's own result slot, which is what renders under the field, and it
 * is guarded by the same answer the token seam gives back rather than by a second check of its own.
 */

const CORRELATION_ID = '7c6b5a49-3827-4160-9e5d-4c3b2a190f8e';
const REJECTION = 'Name is already taken';

class ThrottledCommand extends Command {
    readonly route = '/api/throttled';

    // No client validator: validate() short-circuits before it ever reaches the server when the
    // client rules reject, and a run that does not reach the server cannot be the slow one.
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

describe('when a keystroke overtakes the throttled server validation', given(a_command_form_context, context => {
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

    const settle = async (milliseconds: number) => {
        await act(async () => {
            await new Promise(resolve => setTimeout(resolve, milliseconds));
        });
    };

    // Waits on what actually happened rather than on a clock, so the keystroke lands inside the
    // window the round trip is out for however long the throttle and the round trip really take.
    const until = async (hasHappened: () => boolean) => {
        for (let attempt = 0; attempt < 200 && !hasHappened(); attempt++) {
            await settle(10);
        }
    };

    const letTheThrottledRunGoOutAndThen = async (whileItIsInFlight?: () => void) => {
        isValid = undefined;
        messages = [];
        recorded = [];
        let validateRequests = 0;

        vi.spyOn(global, 'fetch').mockImplementation(async (url) => {
            if (!url.toString().includes('/validate')) {
                return new Response(responseBody([]), { status: 200, headers: { 'Content-Type': 'application/json' } });
            }

            // The first is the run the form makes on mount; the second is the throttled one. Any
            // later one is the throttle re-arming behind the keystroke and is answered at once.
            if (++validateRequests !== 2) {
                return new Response(responseBody([]), { status: 200, headers: { 'Content-Type': 'application/json' } });
            }

            recorded.push('throttled request issued');
            await new Promise(resolve => setTimeout(resolve, 300));
            recorded.push('throttled request answered');
            return new Response(responseBody([REJECTION]), { status: 200, headers: { 'Content-Type': 'application/json' } });
        });

        result = render(
            React.createElement(
                CommandForm,
                {
                    command: ThrottledCommand,
                    validateOn: 'change',
                    autoServerValidate: true,
                    autoServerValidateThrottle: 50
                },
                React.createElement(TextField, { value: (command: ThrottledCommand) => command.name }),
                React.createElement(Probe)),
            { wrapper: context.createWrapper() });

        await until(() => recorded.includes('throttled request issued'));

        if (whileItIsInFlight) whileItIsInFlight();

        await until(() => recorded.includes('throttled request answered'));
        await settle(50);
    };

    afterEach(async () => {
        vi.restoreAllMocks();
        if (result) result.unmount();
        await settle(20);
    });

    describe('and the keystroke is answered while the round trip is still out', () => {
        beforeEach(async () => await letTheThrottledRunGoOutAndThen(() => {
            recorded.push('keystroke');
            fireEvent.change(result.getByTestId('name-input'), { target: { value: 'Updated' } });
        }));

        it('should have answered the round trip after the keystroke', () =>
            recorded.should.deep.equal(['throttled request issued', 'keystroke', 'throttled request answered']));

        it('should not let the overtaken rejection decide validity', () => isValid!.should.be.true);

        it('should not display the message the overtaken round trip carried', () => messages.join(' ').should.not.contain(REJECTION));
    });

    // The control. The same round trip carrying the same rejection, with nothing issued after it,
    // is the current verdict and belongs both in isValid and on screen.
    describe('and nothing overtakes it', () => {
        beforeEach(async () => await letTheThrottledRunGoOutAndThen());

        it('should let the rejection decide validity', () => isValid!.should.be.false);

        it('should display the message the round trip carried', () => messages.join(' ').should.contain(REJECTION));
    });
}));
