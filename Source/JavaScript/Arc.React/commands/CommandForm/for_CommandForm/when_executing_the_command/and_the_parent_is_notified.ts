// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, act } from '@testing-library/react';
import { CommandForm, useCommandFormContext } from '../../CommandForm';
import type { CommandFormState } from '../../CommandFormContext';
import { Command } from '@cratis/arc/commands';
import { TestCommand } from '../TestCommand';
import { a_command_form_being_executed } from '../given/a_command_form_being_executed';
import { given } from '../../../../given';

// An imperative handle cannot re-render a parent, so a submit button outside the form would never
// follow execution without this channel. The callback is deliberately an inline arrow - the most
// common way a parent writes it, and the one that re-fires an effect that depends on it.
describe('when executing the command and the parent is notified', given(a_command_form_being_executed, context => {
    const notifications: CommandFormState[] = [];
    let contextValue: ReturnType<typeof useCommandFormContext> | null = null;
    let renderParentAgain: () => void = () => { /* replaced on first render */ };
    let notificationsBeforeTheParentRerendered = 0;
    let notificationsAfterTheParentRerendered = 0;

    const Recorder = () => {
        contextValue = useCommandFormContext();
        return React.createElement('div');
    };

    const Parent = () => {
        const [renderCount, setRenderCount] = React.useState(0);
        renderParentAgain = () => setRenderCount(count => count + 1);

        return React.createElement(
            'div',
            null,
            React.createElement('span', { 'data-testid': 'render-count' }, String(renderCount)),
            React.createElement(
                CommandForm,
                {
                    command: TestCommand,
                    onStateChange: (state: CommandFormState) => { notifications.push({ ...state }); }
                },
                React.createElement(Recorder)
            )
        );
    };

    beforeEach(async () => {
        context.reset();
        notifications.length = 0;
        contextValue = null;

        render(React.createElement(Parent), { wrapper: context.createWrapper() });
        await act(async () => { await Promise.resolve(); });

        notificationsBeforeTheParentRerendered = notifications.length;
        for (let i = 0; i < 3; i++) {
            await act(async () => {
                renderParentAgain();
            });
        }
        notificationsAfterTheParentRerendered = notifications.length;

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

    it('should tell the parent that execution started', () => notifications.some(state => state.isExecuting).should.be.true);
    it('should tell the parent that execution finished', () => notifications[notifications.length - 1].isExecuting.should.be.false);
    it('should report the transition in order', () => {
        const executionReadings = notifications.map(state => state.isExecuting);
        const changes = executionReadings.filter((value, index) => index === 0 || value !== executionReadings[index - 1]);
        expect(changes).to.eql([false, true, false]);
    });
    it('should not notify again for a parent re-render alone', () => expect(notificationsAfterTheParentRerendered).to.equal(notificationsBeforeTheParentRerendered));
}));
