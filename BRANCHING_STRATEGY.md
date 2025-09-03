# Branching Strategy and Release Process

## Branch Structure

### Main Branches

- **`main`** - Production-ready code, stable releases
- **`dev`** - Integration branch for features and pre-releases

### Feature Branches

- **`feature/feature-name`** - Individual features (branch from `dev`)
- **`bugfix/bug-description`** - Bug fixes (branch from `dev`)
- **`hotfix/issue-description`** - Critical fixes for production (branch from `main`)

## Release Process

### Stable Releases (v8.1.0, v8.2.0, etc.)

1. Ensure `dev` is stable and tested
2. Merge `dev` → `main`
3. Create and push tag: `git tag -a v8.1.0 -m "Release version 8.1.0 - .NET 8.0 compatible"`
4. Push tag: `git push origin v8.1.0`
5. GitHub Actions automatically:
   - Builds and tests
   - Publishes to NuGet
   - Creates GitHub Release

### Pre-releases (Beta, Alpha, RC)

1. Work on `dev` branch
2. Create pre-release tag: `git tag -a v8.1.1-beta.1 -m "Beta 1 for 8.1.1"`
3. Push tag: `git push origin v8.1.1-beta.1`
4. GitHub Actions automatically:
   - Builds and tests
   - Publishes pre-release to NuGet
   - Creates GitHub Pre-release

### Version Naming Convention

- **Stable**: `v8.1.0`, `v8.2.0` (aligned with .NET 8.0)
- **Beta**: `v8.1.1-beta.1`, `v8.1.1-beta.2`
- **Alpha**: `v8.1.1-alpha.1`
- **Release Candidate**: `v8.1.1-rc.1`

**Versioning Strategy:**
- **Major**: Aligns with .NET version (v8.x.x for .NET 8.0)
- **Minor**: Release within major version (v8.1.x)
- **Patch**: Hotfixes and bug fixes (v8.1.1)

## Workflow

### Daily Development

1. Create feature branch from `dev`
2. Develop and test feature
3. Create PR to `dev`
4. Merge after review and CI passes

### Pre-release Testing

1. Merge features to `dev`
2. Test thoroughly
3. Create beta/alpha tag
4. Deploy to NuGet for testing

### Production Release

1. Merge `dev` → `main`
2. Create release tag
3. Deploy to NuGet
4. Create GitHub Release

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

### Creating a pre-release

```bash
git checkout dev
git pull origin dev
git tag -a v8.1.1-beta.1 -m "Beta 1 for 8.1.1"
git push origin v8.1.1-beta.1
```

### Creating a stable release

```bash
git checkout main
git pull origin main
git merge dev
git tag -a v8.1.0 -m "Release version 8.1.0"
git push origin main
git push origin v8.1.0
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
git tag -a v8.1.1 -m "Hotfix release 8.1.1"
git push origin main
git push origin v8.1.1
git branch -d hotfix/critical-fix
```

## CI/CD Pipeline

### Triggers

- **Push to `main`**: Build, test, publish to NuGet, create GitHub Release
- **Push to `dev`**: Build, test, publish pre-release to NuGet
- **Push tags**: Build, test, publish to NuGet
- **Pull Requests**: Build, test, run code analysis

### Artifacts

- NuGet packages are uploaded as artifacts
- Retention: 30 days
- Automatic publishing to NuGet.org

## Security and Quality

- All builds treat warnings as errors
- Code analysis runs on all PRs
- Tests must pass before any deployment
- Manual approval required for production releases (can be configured)
