// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// @vitest-environment jsdom

import { act, renderHook } from '@testing-library/react';
import { vi } from 'vitest';
import { Paging } from '@cratis/arc/queries';
import { useQuery, useQueryWithPaging } from '../useQuery.js';
import { FakeQuery } from './FakeQuery.js';
import { a_shared_query_instance_cache } from './given/a_shared_query_instance_cache.js';

[false, true].forEach(paged => describe(`when a read deadline is configured with paging ${paged}`, () => {
    const context = new a_shared_query_instance_cache();
    let resolveResponse: (response: Response) => void;

    const mount = (timeoutMs?: number) => renderHook(() => paged
        ? useQueryWithPaging(FakeQuery, new Paging(0, 10), undefined, undefined, { timeoutMs })
        : useQuery(FakeQuery, undefined, undefined, { timeoutMs }), { wrapper: context.wrapper });

    const advance = async (ms: number) => {
        await act(async () => { await vi.advanceTimersByTimeAsync(ms); });
    };

    beforeEach(() => {
        vi.useFakeTimers();
        context.establish();
        context.fetchStub.callsFake(() => new Promise<Response>(resolve => { resolveResponse = resolve; }));
    });

    afterEach(() => {
        context.cleanup();
        vi.useRealTimers();
    });

    it('should settle a hung request as a failed timeout result and abort it', async () => {
        const { result } = mount(50);
        await advance(50);
        result.current[0].isPerforming.should.be.false;
        result.current[0].isSuccess.should.be.false;
        result.current[0].hasExceptions.should.be.true;
        result.current[0].exceptionMessages.should.deep.equal(['Query timed out after 50 ms.']);
        context.fetchStub.firstCall.args[1].signal.aborted.should.be.true;
    });

    it('should ignore a late response even if the transport ignores abort', async () => {
        const { result } = mount(50);
        await advance(50);
        const failed = result.current[0];
        await act(async () => resolveResponse(context.successfulResponseWith([{ id: 'late' }])));
        result.current[0].should.equal(failed);
    });

    it('should leave a hung request unchanged without a deadline', async () => {
        const { result } = mount();
        await advance(100);
        result.current[0].isPerforming.should.be.true;
        context.fetchStub.firstCall.args[1].signal.aborted.should.be.false;
    });

    it('should cancel the deadline after success', async () => {
        const { result } = mount(50);
        const signal = context.fetchStub.firstCall.args[1].signal;
        await act(async () => resolveResponse(context.successfulResponseWith([])));
        await advance(100);
        result.current[0].isSuccess.should.be.true;
        signal.aborted.should.be.false;
    });

    it('should cancel the deadline on unmount', async () => {
        const { unmount } = mount(50);
        const signal = context.fetchStub.firstCall.args[1].signal;
        unmount();
        await advance(100);
        signal.aborted.should.be.false;
    });

    it('should allow a subsequent perform with its own deadline to succeed', async () => {
        const { result } = mount(50);
        await advance(50);
        context.fetchStub.resolves(context.successfulResponseWith([]));
        await act(async () => { await result.current[1](undefined, { timeoutMs: 10 }); });
        await advance(100);
        result.current[0].isPerforming.should.be.false;
        result.current[0].isSuccess.should.be.true;
        context.fetchStub.secondCall.args[1].signal.aborted.should.be.false;
    });

    it('should retain the timeout error when the transport rejects on abort', async () => {
        context.fetchStub.callsFake((_url: string, init: RequestInit) => context.untilAborted(init));
        const { result } = mount(50);
        await advance(50);
        result.current[0].exceptionMessages.should.deep.equal(['Query timed out after 50 ms.']);
    });

    it('should clear the deadline after a transport failure', async () => {
        context.fetchStub.rejects(new Error('Offline'));
        const { result } = mount(50);
        await advance(100);
        result.current[0].exceptionMessages.should.deep.equal(['Offline']);
        context.fetchStub.firstCall.args[1].signal.aborted.should.be.false;
    });

    it('should preserve a disabled query supplied through options', async () => {
        const { result } = renderHook(() => paged
            ? useQueryWithPaging(FakeQuery, new Paging(0, 10), undefined, undefined, { isEnabled: false, timeoutMs: 50 })
            : useQuery(FakeQuery, undefined, undefined, { isEnabled: false, timeoutMs: 50 }), { wrapper: context.wrapper });
        await advance(100);
        context.fetchStub.should.not.have.been.called;
        result.current[0].isPerforming.should.be.false;
    });

    it('should allow a perform to disable the hook deadline', async () => {
        const { result } = mount(50);
        await advance(50);
        let performed!: Promise<void>;
        act(() => { performed = result.current[1](undefined, { timeoutMs: 0 }); });
        await advance(100);
        result.current[0].isPerforming.should.be.true;
        await act(async () => {
            resolveResponse(context.successfulResponseWith([]));
            await performed;
        });
        result.current[0].isSuccess.should.be.true;
    });

    it('should not let a superseded deadline abort the newer request', async () => {
        const { result } = mount(50);
        await advance(25);
        let performed!: Promise<void>;
        act(() => { performed = result.current[1](undefined, { timeoutMs: 100 }); });
        const signal = context.fetchStub.secondCall.args[1].signal;
        await advance(25);
        signal.aborted.should.be.false;
        result.current[0].isPerforming.should.be.true;
        await act(async () => {
            resolveResponse(context.successfulResponseWith([]));
            await performed;
        });
        await advance(100);
        result.current[0].isSuccess.should.be.true;
        signal.aborted.should.be.false;
    });

    it('should bound adoption without aborting a co-subscriber request', async () => {
        context.fetchStub.callsFake((_url: string, init: RequestInit) => context.untilAborted(init));
        const first = mount(50);
        const second = mount();
        await advance(50);
        first.result.current[0].isPerforming.should.be.false;
        first.result.current[0].hasExceptions.should.be.true;
        second.result.current[0].isPerforming.should.be.true;
        context.fetchStub.secondCall.args[1].signal.aborted.should.be.false;
    });

    it('should honor a per-perform deadline instead of the hook deadline', async () => {
        const { result } = mount(50);
        await advance(50);
        let performed!: Promise<void>;
        act(() => { performed = result.current[1](undefined, { timeoutMs: 10 }); });
        await advance(10);
        await performed;
        result.current[0].exceptionMessages.should.deep.equal(['Query timed out after 10 ms.']);
    });
}));
