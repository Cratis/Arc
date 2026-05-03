// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useEffect, useRef, useState } from 'react';
import { QueryScopeImplementation } from './QueryScopeImplementation';
import { IQueryScope } from './IQueryScope';
import { useQueryScope } from './useQueryScope';

/* eslint-disable @typescript-eslint/no-empty-function */
const defaultQueryScopeContext: IQueryScope = new class extends IQueryScope {
    get parent() { return undefined; }
    get isPerforming() { return false; }
    addChildScope() { }
    notifyPerformingStarted() { }
    notifyPerformingCompleted() { }
}();
/* eslint-enable @typescript-eslint/no-empty-function */

export const QueryScopeContext = React.createContext<IQueryScope>(defaultQueryScopeContext);

export interface IQueryScopeProps {
    children?: JSX.Element | JSX.Element[];
    setIsPerforming?: (isPerforming: boolean) => void;
}

export const QueryScope = (props: IQueryScopeProps) => {
    const parentScope = useQueryScope();
    const [, setIsPerforming] = useState(false);
    const [queryScope, setQueryScope] = useState<IQueryScope>(defaultQueryScopeContext);

    // Use ref to capture latest prop value without triggering re-creation of the scope
    const setIsPerformingRef = useRef(props.setIsPerforming);

    useEffect(() => {
        setIsPerformingRef.current = props.setIsPerforming;
    }, [props.setIsPerforming]);

    useEffect(() => {
        const queryScopeImplementation = new QueryScopeImplementation(
            (value) => {
                setIsPerforming(value);
                setIsPerformingRef.current?.(value);
            },
            parentScope !== defaultQueryScopeContext ? parentScope : undefined
        );
        setQueryScope(queryScopeImplementation);
    }, [parentScope]);

    return (
        <QueryScopeContext.Provider value={queryScope}>
            {props.children}
        </QueryScopeContext.Provider>
    );
};
