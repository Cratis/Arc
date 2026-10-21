// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act } from '@testing-library/react';
import { QueryResultWithState } from '@cratis/arc/queries';
import { a_shared_query_instance_cache } from './a_shared_query_instance_cache.js';
import { FakeQueryWithRequiredParameters, FakeQueryWithRequiredParametersResult } from '../FakeQueryWithRequiredParameters.js';

export class a_query_with_changing_arguments extends a_shared_query_instance_cache {
    requests: { url: string; resolve: (response: Response) => void }[] = [];

    establish(): void {
        super.establish();
        this.requests = [];
        // Deliberately allow late responses even after an abort, to exercise the hook's version guard.
        this.fetchStub.callsFake((url: URL) => new Promise<Response>(resolve => {
            this.requests.push({ url: url.toString(), resolve });
        }));
    }

    keyFor(userId: string): string {
        return this.queryCache.buildKey(FakeQueryWithRequiredParameters.name, { userId, category: 'x' });
    }

    cacheResult(userId: string, data: FakeQueryWithRequiredParametersResult[]): void {
        const key = this.keyFor(userId);
        this.queryCache.getOrCreate(key, () => new FakeQueryWithRequiredParameters());
        this.queryCache.setLastResult(key, new QueryResultWithState(data, { page: 0, size: 0, totalItems: 0, totalPages: 0 }, true, true, true, [], false, [], '', false));
    }

    async completeRequest(index: number, data: FakeQueryWithRequiredParametersResult[]): Promise<void> {
        await act(async () => {
            this.requests[index].resolve(this.successfulResponseWith(data));
            await this.nextTurn();
        });
    }

    cleanup(): void {
        this.queryCache.dispose();
        super.cleanup();
    }
}
