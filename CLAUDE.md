# Claude instructions

Read and follow [`AGENTS.md`](AGENTS.md) before modifying this repository.
`AGENTS.md` is authoritative if these instructions ever differ.

## Required Git behavior

- Preserve unrelated tracked changes and untracked files.
- Stage only explicit files belonging to the current task.
- Work on a focused branch and submit changes through a pull request to `main`.
- Use Conventional Commit subjects:

  ```text
  <type>: <short description>
  <type>(<scope>): <short description>
  ```

- Select the appropriate type: `feat`, `fix`, `docs`, `refactor`, `test`,
  `build`, `ci`, `perf`, `style`, `chore`, or `revert`.
- Review and validate the diff before committing and pushing.
- Delete the finished branch locally and remotely after its pull request is
  merged.
- Never push a release tag unless the user explicitly requests a mod release.

Examples:

```text
docs: clarify installation requirements
fix(bk-diplomacy): restore reward hooks
ci: update GitHub release packaging
chore: clean obsolete repository metadata
```
