// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IQueryScope } from './IQueryScope';

/**
 * Represents an implementation of {@link IQueryScope}.
 */
export class QueryScopeImplementation extends IQueryScope {
    private _isPerforming = false;
    private _performingCount = 0;
    private _childScopes: IQueryScope[] = [];
    private _parent?: IQueryScope;

    constructor(
        private readonly _setIsPerforming?: (value: boolean) => void,
        parent?: IQueryScope
    ) {
        super();
        this._parent = parent;
        parent?.addChildScope(this);
    }

    /** @inheritdoc */
    get parent(): IQueryScope | undefined {
        return this._parent;
    }

    /** @inheritdoc */
    get isPerforming(): boolean {
        return this._isPerforming || this._childScopes.some(_ => _.isPerforming);
    }

    /** @inheritdoc */
    addChildScope(scope: IQueryScope): void {
        if (this._childScopes.some(_ => _ === scope)) {
            return;
        }
        this._childScopes.push(scope);
    }

    /** @inheritdoc */
    notifyPerformingStarted(): void {
        this._performingCount++;
        if (this._performingCount === 1) {
            this._isPerforming = true;
            this._setIsPerforming?.(true);
        }
    }

    /** @inheritdoc */
    notifyPerformingCompleted(): void {
        this._performingCount = Math.max(0, this._performingCount - 1);
        if (this._performingCount === 0) {
            this._isPerforming = false;
            this._setIsPerforming?.(false);
        }
    }
}
