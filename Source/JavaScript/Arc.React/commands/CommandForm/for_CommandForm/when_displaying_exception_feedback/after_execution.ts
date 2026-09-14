// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act } from '@testing-library/react';
import { CommandResult, type ICommandResult } from '@cratis/arc/commands';
import sinon from 'sinon';
import { given } from '../../../../given';
import { a_command_form_with_exception_feedback } from '../given/a_command_form_with_exception_feedback';

describe('when displaying exception feedback after execution', given(a_command_form_with_exception_feedback, context => {
    let result: CommandResult;
    let returnedResult: ICommandResult<unknown>;
    let originalResult: string;
    let originalMessages: string[];
    let onFailed: sinon.SinonSpy;
    let onException: sinon.SinonSpy;

    beforeEach(async () => {
        onFailed = sinon.spy();
        onException = sinon.spy();
        result = context.exceptionResult();
        originalMessages = result.exceptionMessages;
        originalResult = JSON.stringify(result);
        Object.freeze(originalMessages);
        Object.freeze(result);
        await context.renderForm({ onFailed, onException });
        context.formContext.commandInstance.execute = async () => result;

        await act(async () => {
            returnedResult = await context.formContext.onExecute!();
        });
    });

    it('should display only the safe default in accessible feedback', () => {
        context.rendered.getByRole('alert').textContent!.should.equal(context.defaultMessage);
    });
    it('should not put private diagnostics or the old heading anywhere in the DOM', () => {
        const markup = context.rendered.container.innerHTML;
        markup.should.not.contain(context.privateMessage);
        markup.should.not.contain(context.privateStack);
        markup.should.not.contain('The server responded with');
    });
    it('should pass the exact original result to onFailed', () => {
        onFailed.calledOnce.should.be.true;
        onFailed.firstCall.args[0].should.equal(result);
    });
    it('should pass the exact original diagnostic array and stack to onException', () => {
        onException.calledOnce.should.be.true;
        onException.firstCall.args[0].should.equal(originalMessages);
        onException.firstCall.args[1].should.equal(context.privateStack);
    });
    it('should retain the exact original result in context and return it', () => {
        context.formContext.commandResult!.should.equal(result);
        returnedResult.should.equal(result);
    });
    it('should not mutate the result or diagnostic array', () => {
        JSON.stringify(result).should.equal(originalResult);
        result.exceptionMessages.should.equal(originalMessages);
    });
}));
