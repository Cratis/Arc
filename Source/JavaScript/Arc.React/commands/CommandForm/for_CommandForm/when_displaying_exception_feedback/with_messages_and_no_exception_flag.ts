// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act } from '@testing-library/react';
import sinon from 'sinon';
import { given } from '../../../../given';
import { a_command_form_with_exception_feedback } from '../given/a_command_form_with_exception_feedback';

describe('when displaying exception feedback with messages and no exception flag', given(a_command_form_with_exception_feedback, context => {
    let onException: sinon.SinonSpy;

    beforeEach(async () => {
        onException = sinon.spy();
        await context.renderForm({ onException });
        context.formContext.commandInstance.execute = async () => context.exceptionResult(false);
        await act(async () => { await context.formContext.onExecute!(); });
    });

    it('should display the safe default despite the inconsistent flag', () => {
        context.rendered.getByRole('alert').textContent!.should.equal(context.defaultMessage);
    });
    it('should not expose the diagnostic message', () => {
        context.rendered.container.innerHTML.should.not.contain(context.privateMessage);
    });
    it('should preserve the flag based exception callback behavior', () => {
        onException.called.should.be.false;
    });
}));
