// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderHook } from '@testing-library/react';
import { CommandScope, CommandScopeContext, useCommandScope } from '../index.js';

describe('when resolving nested command scopes through public exports', () => {
    it('should resolve the nearest scope and retain its parent', () => {
        const { result } = renderHook(() => useCommandScope(), {
            wrapper: ({ children }) => <CommandScope><CommandScope><>{children}</></CommandScope></CommandScope>
        });

        expect(result.current.parent).toBeDefined();
        expect(result.current.parent).not.toBe(result.current);
        expect(result.current.parent!.parent).toBeUndefined();
    });

    it('should share the exported context with the hook without a provider', () => {
        const { result } = renderHook(() => ({ scope: useCommandScope(), context: React.useContext(CommandScopeContext) }));

        expect(result.current.scope).toBe(result.current.context);
        expect(result.current.scope.parent).toBeUndefined();
        expect(result.current.scope.hasChanges).toBe(false);
    });
});
