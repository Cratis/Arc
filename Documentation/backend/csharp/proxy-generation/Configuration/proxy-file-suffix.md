# Proxy File Suffix

By default each generated file is named after the type it represents, or after its C# source file when
`CratisProxiesUseSourceFileAsOutputFile` is on:

```text
Features/Authors/RegisterAuthor.ts
Features/Authors/AllAuthors.ts
```

Set `CratisProxiesUseProxyFileSuffix` to name them `Name.proxy.ts` instead:

```xml
<PropertyGroup>
    <CratisProxiesUseProxyFileSuffix>true</CratisProxiesUseProxyFileSuffix>
</PropertyGroup>
```

```text
Features/Authors/RegisterAuthor.proxy.ts
Features/Authors/AllAuthors.proxy.ts
```

Use it when generated proxies share folders with hand-written TypeScript: the suffix tells the two apart at a
glance, and lets tooling - lint rules, code owners, coverage exclusions - match generated files with a single
`*.proxy.ts` pattern.

## What changes with it

- **File names.** Every generated file carries the suffix, including combined files named after a C# source file.
- **Imports between generated files.** A generated file imports another as `./RegisterAuthor.proxy`. Types mapped
  to a module of your own through a type mapping or a package mapping are imported exactly as mapped, never
  suffixed.
- **Barrels.** Generated `index.ts` files export the suffixed names, as in `export * from './RegisterAuthor.proxy';`,
  so imports through a folder barrel keep working unchanged.

Code that imports a generated file directly by path must use the suffixed name once the option is on.

## Switching an existing project

Turning the option on - or off - in a project that already has generated files replaces them: the next build writes
the new names and removes the previously generated ones, like any other generated file that is no longer produced.
Files you wrote yourself are never removed.
