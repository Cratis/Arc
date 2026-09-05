// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, act } from '@testing-library/react';
import { CommandForm, useCommandFormContext } from '../../CommandForm';
import { useIsCommandExecuting } from '../../CommandFormContext';
import { Command } from '@cratis/arc/commands';
import { TestCommand } from '../TestCommand';
import { a_command_form_being_executed } from '../given/a_command_form_being_executed';
import { given } from '../../../../given';

// Asserting only "it stops reporting executing" is vacuous - a form that never reports executing at
// all passes it. The whole sequence is recorded and asserted instead, so the true in the middle is
// what the spec is really about.
describe('when executing the command and the state transitions', given(a_command_form_being_executed, context => {
    const readings: boolean[] = [];
    let contextValue: ReturnType<typeof useCommandFormContext> | null = null;

    const Recorder = () => {
        contextValue = useCommandFormContext();
        const isExecuting = useIsCommandExecuting();
        if (readings[readings.length - 1] !== isExecuting) {
            readings.push(isExecuting);
        }
        return React.createElement('div');
    };

    beforeEach(async () => {
        context.reset();
        readings.length = 0;
        contextValue = null;

        render(
            React.createElement(
                CommandForm,
                { command: TestCommand },
                React.createElement(Recorder)
            ),
            { wrapper: context.createWrapper() }
        );
        await act(async () => { await Promise.resolve(); });

        let pending: Promise<unknown> = Promise.resolve();
        await act(async () => {
            (contextValue!.commandInstance as unknown as Command).execute = context.executeDeferred as never;
            pending = contextValue!.onExecute!();
        });

        await act(async () => {
            context.executions[0].succeed(context.successfulResult());
            await pending;
        });
    });

    it('should go from not executing to executing and back', () => expect(readings).to.eql([false, true, false]));
}));
