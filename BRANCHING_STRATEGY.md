# Branching Strategy and Release Process

## Branch Structure

### Main Branches

- **`main`** - Production-ready code, stable releases
- **`dev`** - Integration branch for features and pre-releases

### Feature Branches

- **`feature/feature-name`** - Individual features (branch from `dev`)
- **`bugfix/bug-description`** - Bug fixes (branch from `dev`)
- **`hotfix/issue-description`** - Critical fixes for production (branch from `main`)

## Package Version

The package version is a UTC calendar stamp, `yyyy.M.d.Hmm`:

- Year, month, and day come from the clock.
- The fourth component is `hour * 100 + minute` (00:00 is `0`, 09:05 is `905`, 23:59 is `2359`).
- NuGet drops leading zeros. `2026.10.08.0905` is stored as `2026.10.8.905`. Ordering still follows the clock.
- The year is not a SemVer major. Breaking changes are called out in the release notes.
- Two packs produced in the same UTC minute collide. Release one package per minute.

CI sets `Version` once per run from the **committer timestamp of HEAD** (`eng/package-version.sh`). A rebuild of that commit produces the same package. Local `dotnet pack` without `Version` stamps the current UTC time instead.

This replaces MinVer and the `v8.x` tag scheme. A git tag can still trigger the publish jobs, but the tag name is not the package version.

Target frameworks are `net10.0` and `net11.0`, listed in `Directory.Build.props` as `FlumeTargetFrameworks`. They are not encoded in the version number.

## Release Process

### Building a release

1. Ensure `dev` is stable and tested
2. Merge `dev` → `main`
3. The package version is the UTC committer time of the commit that is packed
4. GitHub Actions, for that commit:
   - Builds and tests `net10.0` and `net11.0`
   - Sets `Version` from `eng/package-version.sh`
   - Packs the nupkg

Publishing to NuGet stays on the existing tag-triggered jobs. Do not publish a package that targets `net11.0` until you mean to: as of October 8, 2026, `net11.0` is compiled with SDK `11.0.100-rc.1.26425.128` (GA is November 10, 2026).

### Pre-releases

Pre-release tags (`-alpha`, `-beta`, `-rc`) still select the prerelease publish job. The nupkg version remains the calendar stamp, not the tag.

## Workflow

### Daily Development

1. Create feature branch from `dev`
2. Develop and test feature
3. Create PR to `dev`
4. Merge after review and CI passes

### Production Release

1. Merge `dev` → `main`
2. Let CI pack with the commit stamp
3. Tag only when you want the publish job to run
4. Put breaking changes in the GitHub release notes

## Commands Reference

### Creating a new feature

```bash
git checkout dev
git pull origin dev
git checkout -b feature/new-feature
# ... dev feature ...
git push origin feature/new-feature
# Create PR to dev
```

### Seeing the version CI will use

```bash
bash eng/package-version.sh
```

### Packing locally with an explicit stamp

```bash
dotnet pack src/Flume/Flume.csproj -c Release -p:Version="$(bash eng/package-version.sh)"
```

### Hotfix for production

```bash
git checkout main
git pull origin main
git checkout -b hotfix/critical-fix
# ... fix the issue ...
git commit -m "Fix critical issue"
git checkout main
git merge hotfix/critical-fix
git push origin main
git branch -d hotfix/critical-fix
```

## CI/CD Pipeline

### Triggers

- **Push to `main` or `dev`**: Build, test, pack. The package version is the commit stamp.
- **Push tags**: Build, test, and the publish jobs may push to NuGet.
- **Pull Requests**: Build, test, run code analysis.

### SDK

`global.json` pins SDK `11.0.100-rc.1.26425.128` (`allowPrerelease`). Workflows also install SDK `10.0.401` so `net10.0` can compile and the tests can run. The .NET 10 SDK alone cannot compile `net11.0`.

### Artifacts

- NuGet packages are uploaded as artifacts
- Retention: 30 days

## Security and Quality

- All builds treat warnings as errors
- Code analysis runs on all PRs
- Tests must pass on `net10.0` and `net11.0` before any deployment
- Manual approval required for production releases (can be configured)
