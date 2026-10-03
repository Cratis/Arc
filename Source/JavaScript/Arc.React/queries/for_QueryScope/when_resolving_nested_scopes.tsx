// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderHook } from '@testing-library/react';
import { QueryScope, QueryScopeContext, useQueryScope } from '../index.js';

describe('when resolving nested query scopes through public exports', () => {
    it('should resolve the nearest scope and retain its parent', () => {
        const { result } = renderHook(() => useQueryScope(), {
            wrapper: ({ children }) => <QueryScope><QueryScope><>{children}</></QueryScope></QueryScope>
        });

        expect(result.current.parent).toBeDefined();
        expect(result.current.parent).not.toBe(result.current);
        expect(result.current.parent!.parent).toBeUndefined();
    });

    it('should share the exported context with the hook without a provider', () => {
        const { result } = renderHook(() => ({ scope: useQueryScope(), context: React.useContext(QueryScopeContext) }));

        expect(result.current.scope).toBe(result.current.context);
        expect(result.current.scope.parent).toBeUndefined();
        expect(result.current.scope.isPerforming).toBe(false);
    });
});
