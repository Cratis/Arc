// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, act } from '@testing-library/react';
import { CommandForm } from '../../CommandForm';
import type { CommandFormHandle } from '../../CommandFormContext';
import { TestCommand } from '../TestCommand';
import { a_command_form_being_executed } from '../given/a_command_form_being_executed';
import { given } from '../../../../given';

// The regression guard for the re-attach loop. A handle rebuilt on every render re-attaches the
// callback ref on every render, and a parent that stores what the ref hands it re-renders in response -
// which rebuilds the handle again. The ref callback itself is stable here, so the only thing that can
// re-invoke it is the handle changing identity.
describe('when executing the command and the handle is held across a parent render', given(a_command_form_being_executed, context => {
    let attachments = 0;
    let detachments = 0;
    let handle: CommandFormHandle | null = null;
    let renderParentAgain: () => void = () => { /* replaced on first render */ };

    const Parent = () => {
        const [renderCount, setRenderCount] = React.useState(0);
        renderParentAgain = () => setRenderCount(count => count + 1);

        const formRef = React.useCallback((formHandle: CommandFormHandle | null) => {
            if (formHandle) {
                attachments++;
                handle = formHandle;
            } else {
                detachments++;
            }
        }, []);

        return React.createElement(
            'div',
            null,
            React.createElement('span', { 'data-testid': 'render-count' }, String(renderCount)),
            React.createElement(CommandForm, { command: TestCommand, formRef })
        );
    };

    beforeEach(async () => {
        context.reset();
        attachments = 0;
        detachments = 0;
        handle = null;

        render(React.createElement(Parent), { wrapper: context.createWrapper() });
        await act(async () => { await Promise.resolve(); });

        for (let i = 0; i < 3; i++) {
            await act(async () => {
                renderParentAgain();
            });
        }
    });

    it('should attach the handle exactly once', () => expect(attachments).to.equal(1));
    it('should not detach the handle while the form is mounted', () => expect(detachments).to.equal(0));
    it('should expose an execute function on the handle', () => expect(typeof handle!.execute).to.equal('function'));
    it('should report the form as not executing', () => handle!.isExecuting.should.be.false);
}));
