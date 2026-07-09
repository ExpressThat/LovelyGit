# Team Task Planning Workflow

This note defines how Tom and the lead keep the LovelyGit team supplied with concrete work while preserving the workspace rules in `AGENTS.md`.

## Planning Cadence

- Review the active board at the start of each planning pass and whenever a teammate completes or blocks a task.
- Keep every available teammate assigned to at least two concrete TODO tasks whenever the backlog allows it.
- Prefer small, independently deliverable tasks that can move through implementation, verification, commit, PR, and review without waiting on unrelated work.
- Split broad goals into separate planning, implementation, QA, documentation, and review tasks when those steps can proceed in parallel.

## Task Quality

Each task should include:

- A clear outcome that can be verified.
- The expected scope boundary, especially when product behavior must not change.
- Relevant files, commands, or workflows to inspect first.
- Verification expectations, including CMG visual checks when user-facing UI is affected.
- Commit and PR expectations for code, relevant QA artifacts, and documentation updates.
- Review handoff notes, including Alice review via `ExpressThat-bot` with the provided `GH_TOKEN` when Alice is the intended reviewer.

Avoid assigning vague reminders such as "look into docs" when a concrete target is known. If the right location is not known, the task should ask the owner to triage the location before editing and record the chosen target in a task comment.

## Ownership

- The lead owns the team objective, priority order, and final call on scope.
- Tom helps convert the objective into enough ready tasks for the team to stay busy.
- Task owners are responsible for validating the current repo state before editing, keeping changes scoped, and reporting blockers on the task rather than silently waiting.
- Owners should avoid product behavior changes in documentation-only tasks unless the task is explicitly expanded.

## Review And QA Handoff

- Documentation changes that are relevant to delivery should be committed and opened as PRs.
- Use merge commits only for branch integration and PR merge guidance in this workspace.
- Handoff notes should summarize what changed, what was verified, and what remains unverified.
- If Alice is expected to review, state that the PR should be reviewed with the `ExpressThat-bot` GitHub identity using the provided `GH_TOKEN`.
- Runtime or UI changes need verification appropriate to their risk. For real LovelyGit visual checks, use CMG against the WebView2 app as described in `AGENTS.md`.

## Avoiding Idle Teammates

- When a teammate has fewer than two actionable TODO tasks, create or refine tasks before asking them to wait.
- If all useful work is blocked, record the blocker on the task and create an unblocked follow-up such as docs, QA, triage, or review preparation when one is genuinely useful.
- Do not create filler tasks. A task is ready only when the owner can start from the prompt and make visible progress.
- Rebalance ownership when one teammate is overloaded and another is idle, but preserve existing task context through comments rather than relying on side-channel memory.
