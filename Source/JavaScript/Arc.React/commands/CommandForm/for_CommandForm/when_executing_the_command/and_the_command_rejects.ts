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

// The count is given back in a finally, not a catch: a form that is left permanently executing after a
// network failure is unusable, and swallowing the rejection to achieve that would hide the failure from
// the caller. Both halves are asserted here.
describe('when executing the command and the command rejects', given(a_command_form_being_executed, context => {
    const failure = new Error('the command could not be reached');
    let isExecuting = false;
    let contextValue: ReturnType<typeof useCommandFormContext> | null = null;
    let caught: unknown;

    const Recorder = () => {
        contextValue = useCommandFormContext();
        isExecuting = useIsCommandExecuting();
        return React.createElement('div');
    };

    beforeEach(async () => {
        context.reset();
        contextValue = null;
        caught = undefined;

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
            pending = contextValue!.onExecute!().catch(reason => { caught = reason; });
        });

        await act(async () => {
            context.executions[0].fail(failure);
            await pending;
        });
    });

    it('should stop reporting executing', () => isExecuting.should.be.false);
    it('should let the rejection propagate unchanged', () => expect(caught).to.equal(failure));
}));
