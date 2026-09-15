// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act } from '@testing-library/react';
import { CommandResult } from '@cratis/arc/commands';
import sinon from 'sinon';
import { given } from '../../../../given';
import { a_command_form_with_exception_feedback } from '../given/a_command_form_with_exception_feedback';

describe('when displaying exception feedback with a descendant result', given(a_command_form_with_exception_feedback, context => {
    let result: CommandResult;
    let originalResult: string;
    let onFailed: sinon.SinonSpy;
    let onException: sinon.SinonSpy;

    beforeEach(async () => {
        result = context.exceptionResult();
        originalResult = JSON.stringify(result);
        Object.freeze(result.exceptionMessages);
        Object.freeze(result);
        onFailed = sinon.spy();
        onException = sinon.spy();
        await context.renderForm({ onFailed, onException });
        await act(async () => context.setResult(result));
    });

    it('should show the safe default for results supplied through useSetCommandResult', () => {
        context.rendered.getByRole('alert').textContent!.should.equal(context.defaultMessage);
    });
    it('should keep private diagnostics and the old heading out of the DOM', () => {
        const markup = context.rendered.container.innerHTML;
        markup.should.not.contain(context.privateMessage);
        markup.should.not.contain(context.privateStack);
        markup.should.not.contain('The server responded with');
    });
    it('should preserve the exact result without mutation in context', () => {
        context.formContext.commandResult!.should.equal(result);
        JSON.stringify(context.formContext.commandResult).should.equal(originalResult);
    });
    it('should not invoke execution callbacks for a descendant result', () => {
        onFailed.called.should.be.false;
        onException.called.should.be.false;
    });
}));
