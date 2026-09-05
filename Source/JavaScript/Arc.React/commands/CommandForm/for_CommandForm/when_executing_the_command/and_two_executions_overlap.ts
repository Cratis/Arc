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

// Nothing stops a second submission while the first is still in flight. A flag would be cleared by
// whichever one settles first and the form would report itself idle with a command still running, so
// executions are counted: 0 -> 1 -> 2 -> 1 -> 0.
describe('when executing the command and two executions overlap', given(a_command_form_being_executed, context => {
    let isExecuting = false;
    let contextValue: ReturnType<typeof useCommandFormContext> | null = null;
    let whileBothAreInFlight = false;
    let afterTheFirstSettles = false;
    let afterTheLastSettles = false;

    const Recorder = () => {
        contextValue = useCommandFormContext();
        isExecuting = useIsCommandExecuting();
        return React.createElement('div');
    };

    beforeEach(async () => {
        context.reset();
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

        let first: Promise<unknown> = Promise.resolve();
        let second: Promise<unknown> = Promise.resolve();

        await act(async () => {
            (contextValue!.commandInstance as unknown as Command).execute = context.executeDeferred as never;
            first = contextValue!.onExecute!();
        });

        await act(async () => {
            second = contextValue!.onExecute!();
        });
        whileBothAreInFlight = isExecuting;

        await act(async () => {
            context.executions[0].succeed(context.successfulResult());
            await first;
        });
        afterTheFirstSettles = isExecuting;

        await act(async () => {
            context.executions[1].succeed(context.successfulResult());
            await second;
        });
        afterTheLastSettles = isExecuting;
    });

    it('should have started two executions', () => expect(context.executions.length).to.equal(2));
    it('should report executing while both are in flight', () => whileBothAreInFlight.should.be.true);
    it('should still report executing after the first one settles', () => afterTheFirstSettles.should.be.true);
    it('should stop reporting executing once the last one settles', () => afterTheLastSettles.should.be.false);
}));
