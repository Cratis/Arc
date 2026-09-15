---
applyTo: "**/*"
---

## Commands

```bash
dotnet build
dotnet test
yarn install && yarn build   # JavaScript/React packages
```

CI runs the .NET and JavaScript builds, package-graph verification, and
markdown verification. Releasing a package happens only through a labeled
merge to `main` (`major`/`minor`/`patch`).
