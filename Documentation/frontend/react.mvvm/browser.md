# Browser

When working with view models in the MVVM pattern, you typically want to avoid direct dependencies on browser globals like `window`, `document`, or `localStorage`. This makes your code more testable and maintainable. Cratis Arc provides abstracted interfaces for common browser functionality that can be injected into your view models.

## Available Interfaces

### INavigation

The `INavigation` interface provides a way to observe URL changes in your application without directly accessing the browser's navigation APIs.

```typescript
export type UrlChangedCallback = (url: string, previousUrl: string) => void;

export abstract class INavigation {
    abstract onUrlChanged(callback: UrlChangedCallback): void;
}
```

#### Usage in a View Model

```typescript
import { injectable, inject } from 'tsyringe';
import { INavigation } from '@cratis/arc.react.mvvm/browser';

@injectable()
export class MyViewModel {
    constructor(@inject(INavigation) private readonly _navigation: INavigation) {
        this._navigation.onUrlChanged((url, previousUrl) => {
            console.log(`Navigated from ${previousUrl} to ${url}`);
            // Handle navigation change
        });
    }
}
```

The `Navigation` implementation uses a `MutationObserver` to detect URL changes in the DOM, making it compatible with client-side routing libraries like React Router.

### ILocalStorage

The `ILocalStorage` interface provides access to browser local storage following the standard `Storage` interface:

```typescript
export abstract class ILocalStorage implements Storage {
    abstract length: number;
    abstract clear(): void;
    abstract getItem(key: string): string | null;
    abstract key(index: number): string | null;
    abstract removeItem(key: string): void;
    abstract setItem(key: string, value: string): void;
}
```

#### Usage in a View Model

```typescript
import { injectable, inject } from 'tsyringe';
import { ILocalStorage } from '@cratis/arc.react.mvvm/browser';

@injectable()
export class PreferencesViewModel {
    constructor(@inject(ILocalStorage) private readonly _localStorage: ILocalStorage) {
    }

    savePreference(key: string, value: string): void {
        this._localStorage.setItem(key, value);
    }

    loadPreference(key: string): string | null {
        return this._localStorage.getItem(key);
    }

    clearAllPreferences(): void {
        this._localStorage.clear();
    }
}
```

## Dependency Injection Setup

These browser interfaces are automatically registered when you initialize the MVVM bindings. The registration happens in the `Bindings.initialize()` method:

- `INavigation` is registered as a singleton with the `Navigation` implementation
- `ILocalStorage` is registered as an instance pointing to the browser's native `localStorage`

You don't need to manually register these - they're available as soon as you set up your MVVM context.

## Benefits

Using these abstracted interfaces provides several advantages:

1. **Testability**: You can easily mock these interfaces in your unit tests
2. **Decoupling**: Your view models don't directly depend on browser globals
3. **Consistency**: All browser interactions go through well-defined interfaces
4. **Type Safety**: Full TypeScript support with proper typing

## Example: Complete View Model

Here's a complete example showing both interfaces in use:

```typescript
import { injectable, inject } from 'tsyringe';
import { INavigation, ILocalStorage } from '@cratis/arc.react.mvvm/browser';
import { makeObservable, observable } from 'mobx';

@injectable()
export class NavigationHistoryViewModel {
    @observable currentUrl: string = '';
    @observable visitedUrls: string[] = [];

    constructor(
        @inject(INavigation) private readonly _navigation: INavigation,
        @inject(ILocalStorage) private readonly _localStorage: ILocalStorage
    ) {
        makeObservable(this);
        
        // Load previously visited URLs from storage
        const stored = this._localStorage.getItem('visitedUrls');
        if (stored) {
            this.visitedUrls = JSON.parse(stored);
        }

        // Track URL changes
        this._navigation.onUrlChanged((url, previousUrl) => {
            this.currentUrl = url;
            this.visitedUrls.push(url);
            this._localStorage.setItem('visitedUrls', JSON.stringify(this.visitedUrls));
        });
    }

    clearHistory(): void {
        this.visitedUrls = [];
        this._localStorage.removeItem('visitedUrls');
    }
}
```
