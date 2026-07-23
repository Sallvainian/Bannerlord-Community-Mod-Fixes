# Repository agent instructions

These instructions apply to every automated agent working in this repository.

## Preserve the working tree

- Inspect `git status --short --branch` before editing or staging.
- Treat existing tracked changes and untracked files as user-owned unless the
  current task explicitly places them in scope.
- Stage explicit paths. Do not use `git add -A` in a mixed working tree.
- Do not delete or stage the local `mods/*/assets/` directories unless the task
  explicitly concerns those assets.
- Never use destructive Git commands such as `git reset --hard`.

## Conventional Commit messages

Every commit subject must use this format:

```text
<type>: <short description>
```

An optional scope may identify the affected mod or system:

```text
<type>(<scope>): <short description>
```

Use the most specific applicable type:

- `feat`: add user-visible functionality
- `fix`: correct a bug, crash, or compatibility problem
- `docs`: change documentation only
- `refactor`: restructure code without changing behavior
- `test`: add or change tests and verification tools
- `build`: change build scripts, dependencies, or packaging
- `ci`: change GitHub Actions or other automation
- `perf`: improve performance
- `style`: change formatting without changing behavior
- `chore`: perform repository maintenance that fits no type above
- `revert`: revert an earlier commit

Examples:

```text
docs: document mod dependencies
fix(captivity-events): guard Harmony initialization
ci: generate versioned release notes
chore(release): prepare Better Troop HUD 1.1.1
```

Write the description in the imperative mood, keep it concise, do not
capitalize the first word unnecessarily, and do not end the subject with a
period. For a breaking change, add `!` after the type or scope and explain the
break in a `BREAKING CHANGE:` commit footer.

## Commit and push workflow

1. Fetch `origin`, switch to `main`, and fast-forward to `origin/main`.
2. Create a focused branch such as `agent/<short-description>`.
3. Make the smallest coherent change required by the task.
4. Run validation proportional to the change.
5. Review `git diff` and `git diff --check`.
6. Stage only the intended files by explicit path.
7. Commit using the Conventional Commit format above.
8. Push with upstream tracking:

   ```powershell
   git push -u origin HEAD
   ```

9. Open a pull request targeting `main`. The description must explain what
   changed, why it changed, and how it was validated.
10. After the pull request is merged, switch back to `main`, fast-forward it,
    and delete the finished branch locally and remotely.

Do not push directly to `main`, force-push shared branches, or bypass a failing
check unless the user explicitly authorizes that exact action.

## Release safeguards

- An ordinary commit or push must not publish a mod release.
- Do not create or push release tags unless the user explicitly requests a
  release.
- Supported release tags are:
  - `bk-diplomacy-v*`
  - `better-troop-hud-v*`
  - `relentless-smith-bk-redux-v*`
  - `captivity-events-1.4.7-v*`
- Before releasing a mod, keep its manifest version, `CHANGELOG.md`, README
  version, archive name, and release tag consistent.
- Preserve dependency and original-author links in the root README and release
  notes.

