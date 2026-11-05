---
title: Cratis Arc MVVM dependency injection with TSyringe
description: Configure TypeScript decorator metadata, runtime reflection, and the Vite plugin so TSyringe can resolve dependencies in Cratis Arc view models.
---

Cratis Arc MVVM uses [Microsoft TSyringe](https://github.com/microsoft/tsyringe) for dependency injection: it creates a view model and supplies its constructor dependencies. Configure decorator metadata and runtime reflection before binding your first view. Otherwise, a constructor parameter's TypeScript annotation is not enough to tell the container what to create.

Enable these compiler options in the TypeScript configuration used by your application:

```json
{
    "compilerOptions": {
        "experimentalDecorators": true,
        "emitDecoratorMetadata": true
    }
}
```

The emitted metadata also needs the `Reflect` metadata API at runtime. Import `@cratis/arc` in your entry module before importing `App` or other modules containing decorated classes. That import loads the reflection polyfill from `@cratis/fundamentals`; a separate reflection package is not needed.

After that import, mount your application as usual. This entry-point excerpt assumes `React` and `ReactDOM` are already imported:

```tsx
import { App } from './App';

ReactDOM.createRoot(document.getElementById('root')!).render(
    <React.StrictMode>
        <App />
    </React.StrictMode>,
);
```

Mark a view model with constructor dependencies as `@injectable()`. Use runtime classes as dependency tokens, such as `IIdentityProvider`, which is an abstract class despite its interface-style name. A TypeScript interface disappears during compilation and cannot serve as a token by itself.

## Vite

Vite's default TypeScript transform does not emit the constructor metadata TSyringe needs. Add `@cratis/arc.vite` as a development dependency and include `EmitMetadataPlugin()` alongside your existing plugins:

```typescript title="vite.config.ts"
import { defineConfig } from 'vite';
import { EmitMetadataPlugin } from '@cratis/arc.vite';

export default defineConfig({
    plugins: [
        EmitMetadataPlugin()
    ]
});
```

Keep decorated view model classes in `.ts` files for this default configuration: the plugin's default file filter matches `.ts`, not `.tsx`. It reads `tsconfig.json` in the working directory by default. If your application uses a separate configuration file, pass its path through the plugin's `tsconfigPath` option.

## Bindings

Use the [MVVM context](./mvvm-context.md) in a React application. It calls `Bindings.initialize()` for you, registering the messenger, navigation, identity provider, and local storage services. Do not repeat that initialization in each view model.

For a setup that needs those service registrations without rendering `<MVVM>`, call the initializer at application startup:

```ts
import { Bindings } from '@cratis/arc.react.mvvm';

Bindings.initialize();
```

This manual call registers services in TSyringe's shared container. It does not configure MobX, provide React Router, or create the view-specific dialog service. For the normal React path, continue with [provider placement](./mvvm-context.md), then [bind a view model](./using-view-model.md).
