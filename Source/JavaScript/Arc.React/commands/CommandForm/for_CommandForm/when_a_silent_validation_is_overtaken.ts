// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, act } from '@testing-library/react';
import { Command } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';
import { vi } from 'vitest';
import { CommandForm, useCommandFormContext } from '../CommandForm';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

/**
 * Silent validation is what decides isValid, and with autoServerValidate every run of it is a round
 * trip. The effect that issues them re-runs on every values change, so overlapping runs are normal
 * rather than exceptional - values arriving in close succession, from a fast typist or from a form
 * filled programmatically, leave several in flight at once.
 *
 * Writing whichever one resolved last makes the winner arrival order rather than issue order, so a
 * slower run describing values the form no longer holds lands after a faster run describing the
 * current ones and overwrites it. isValid is derived from that single slot and nothing recomputes it,
 * which makes the stale verdict terminal: submit greys out and stays that way with every field valid
 * and no message shown anywhere, until some unrelated interaction happens to schedule another run.
 *
 * So these pin issue order, in both directions. Discarding an overtaken result is only half of it -
 * the newest run has to land whether it is the fast one or the slow one, and a guard that simply
 * preferred whatever arrived first would pass the first of these and fail the second.
 */

const CORRELATION_ID = '9f6e5d4c-3b2a-4190-8c7d-6e5f4a3b2c1d';

class RaceCommand extends Command {
    readonly route = '/api/race';

    // Optional, and with no client validator, so every run reaches the server rather than being
    // answered client-side - two runs cannot overlap if one of them never leaves the browser.
    readonly propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('name', String, true)
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

interface PlannedResponse {
    delayMs: number;
    messages: string[];
}

describe('when a silent validation is overtaken', given(a_command_form_context, context => {
    let result: ReturnType<typeof render>;
    let capturedIsValid: boolean | undefined;
    let validateCalls: number;

    const ContextCapture = () => {
        capturedIsValid = useCommandFormContext().isValid;
        return React.createElement('div');
    };

    const settle = async (milliseconds: number) => {
        await act(async () => {
            await new Promise(resolve => setTimeout(resolve, milliseconds));
        });
    };

    const form = (name: string) => React.createElement(
        CommandForm,
        {
            command: RaceCommand,
            currentValues: { name },
            autoServerValidate: true,
            // Long enough that the throttled server run cannot fire inside the window these assert in.
            // It writes to the same slot, so letting it land would repair a stale verdict on its own
            // and hide the very thing being pinned.
            autoServerValidateThrottle: 5000
        },
        React.createElement(ContextCapture));

    // Two runs, issued in order, resolving in whichever order the plan dictates. The first describes
    // values that are already gone by the time it answers; the second describes the current ones.
    const issueTwoRunsWith = async (plan: PlannedResponse[]) => {
        capturedIsValid = undefined;
        validateCalls = 0;

        vi.spyOn(global, 'fetch').mockImplementation(async (url) => {
            const planned = url.toString().includes('/validate')
                ? plan[Math.min(validateCalls++, plan.length - 1)]
                : { delayMs: 0, messages: [] };

            await new Promise(resolve => setTimeout(resolve, planned.delayMs));
            return new Response(responseBody(planned.messages), {
                status: 200,
                headers: { 'Content-Type': 'application/json' }
            });
        });

        result = render(form('stale'), { wrapper: context.createWrapper() });

        // New values before the first run has answered - this is the whole scenario.
        await act(async () => { result.rerender(form('current')); });

        await settle(600);
    };

    afterEach(async () => {
        vi.restoreAllMocks();
        if (result) result.unmount();
        await settle(50);
    });

    describe('and the run it overtakes is the slow one', () => {
        beforeEach(async () => await issueTwoRunsWith([
            { delayMs: 300, messages: ['Name is already taken'] },
            { delayMs: 0, messages: [] }
        ]));

        // Both had to actually be in flight together, or there was no race to resolve and the
        // assertion below would hold for a reason that has nothing to do with ordering.
        it('should have issued both runs', () => validateCalls.should.equal(2));

        // The rejection describes 'stale'. The form holds 'current', which the server accepted.
        it('should not let the overtaken rejection decide validity', () => capturedIsValid!.should.be.true);
    });

    describe('and the run that overtakes is the slow one', () => {
        beforeEach(async () => await issueTwoRunsWith([
            { delayMs: 0, messages: ['Name is already taken'] },
            { delayMs: 300, messages: [] }
        ]));

        it('should have issued both runs', () => validateCalls.should.equal(2));

        // Nothing was overtaken here - the newest run is simply slow. Discarding it because a
        // stale verdict got there first would be the same defect facing the other way.
        it('should apply the newest run even though it answered last', () => capturedIsValid!.should.be.true);
    });
}));
