# Tsyringe

Cratis Arc relies on [Microsoft Tsyringe](https://github.com/microsoft/tsyringe) a lightweight IoC container
for TypeScript.

In order to use Tsyringe, your application needs to produce type metadata that it can use to resolve dependencies.
Tsyringe leverages decorators to bee able to do this, you will have to include this in your TypeScript configuration.
In your `tsconfig.json` make sure to enable the following compiler options:

```json
{
    "compilerOptions": {
        "experimentalDecorators": true,
        "emitDecoratorMetadata": true,
    }
}
```

In addition to this, Tsyringe needs the `Reflect` metadata API to be available at runtime. Cratis Arc ships a lightweight
polyfill for this (provided by `@cratis/fundamentals`) and loads it automatically when you import `@cratis/arc`, so there is
nothing extra to install or set up.

```tsx
import { App } from './App';

ReactDOM.createRoot(document.getElementById('root')!).render(
    <React.StrictMode>
        <App />
    </React.StrictMode>
);
```

This enables the necessary reflection metadata for Tsyringe to work properly.

## Vite

If you're using Vite you will find that things are not working as expected. This is because Vite during development with the dev server will not provide the
necessary metadata. Cratis Arc offers a vite plugin for emitting the necessary metadata.

Add a dev package reference to `@cratis/arc.vite`. Then in your `vite.config` you can add it to plugins:

```js
import { defineConfig } from 'vite';
import { EmitMetadataPlugin } from '@cratis/arc.vite';

export default defineConfig({
    plugins: [
        EmitMetadataPlugin() as any
    ]
});
```

## Bindings

In the Rect MVVM package you'll find services that will help you as a developer work with MVVM, these are typically services you would take
a dependency to in your view models. Within the package sits a type called `Bindings`, you can call this to get everything initialized:

> Note: If you're using the [MVVM context](./mvvm-context.md) all bindings are automatically configured for the MVVM scenario and you are good to go and you can skip this step.

```ts
import { Bindings } from '@cratis/arc.react.mvvm';

Bindings.initialize();
```
