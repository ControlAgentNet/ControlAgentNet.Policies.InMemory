# Versioning Strategy

This repository uses `VersionPrefix` from `Directory.Build.props` as the source of truth.

## Rules

- local builds: `<VersionPrefix>-dev`
- pull requests: `<VersionPrefix>-preview.<run_number>`
- pushes to `main`: `<VersionPrefix>-alpha.<run_number>`
- tags like `v1.0.0`: exact release version `1.0.0`

## Release flow

1. Set `<VersionPrefix>` in `Directory.Build.props` to the intended release version.
2. Merge the release-ready commit to `main`.
3. Create the tag:

```bash
git tag v0.1.1
git push origin v0.1.1
```

The CI workflow will build and pack the tagged version exactly.

If the repository secret `NUGET_API_KEY` contains a valid NuGet.org API key, the publish workflow will also push the generated package automatically.
