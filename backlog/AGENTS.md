# Backlog instructions for AI agents

The Markdown files in `tasks/` are the project's backlog and source of truth.

## Normal workflow

1. Run `backlog list --json` before planning or changing tasks. `--toon` is also
   available when compact LLM context is more useful than JSON interoperability.
2. Inspect likely matches with `backlog show ID --json`.
3. Update an existing task instead of creating a duplicate.
4. Create tasks with `backlog add TITLE` and update them with `backlog update ID`.
5. `add` accepts `--release`, `--priority 1` through `5`, `--size S|M|L|none`,
  `--state`, repeatable `--tag TAG`, and either `--body`, `--body-file PATH`, or `--template NAME`.
   Use `--context`, `--outcome`, and repeatable `--criteria` to fill a template.
6. For scripts, pass `--json` or `--toon` to `add` and `update`; their output is
   the final task object. `add-batch --file PATH` reads an atomic JSON task list.
   `list --tag TAG` is repeatable and returns tasks carrying every requested tag.
7. Finish with `backlog check` and report the task IDs changed.
8. Use `backlog upgrade` to preview generated integration updates. It writes only
   with `--apply` (or an interactive `Apply now? [y/N]` confirmation) and refreshes
   the tracked `backlog/.version` marker last.

Examples:

```console
backlog add "Add offline cache" --release next --priority 1 --size M   --template standard --context "The app needs offline reads"   --outcome "Cached data remains available" --criteria "Offline launch works" --json
backlog update T0001 --title "Add resilient offline cache" --release next --priority 2   --size M --state in_progress --body "## Current state

Started" --toon
```

## Extracting context from a conversation

When asked to dump, capture, or remember conversation context, do not copy the
transcript wholesale. Convert it into durable, actionable backlog information:

- Create one task per independently actionable outcome.
- Preserve why the task exists and what successful completion looks like.
- Preserve decisions, rejected alternatives, constraints, and known risks.
- Record current progress and the next useful action.
- Include relevant file paths, links, commands, or error messages.
- Omit greetings, repetition, speculative ideas not adopted, and conversational filler.
- Never store credentials, tokens, private keys, or unrelated personal information.
- Clearly label uncertainty; do not turn guesses into established facts.

Suggested Markdown body headings are `Context`, `Desired outcome`, `Decisions and
constraints`, `Current state`, and `Notes`. Use only the headings that add value.

## Format invariants

- Never change an existing task's `id` or `created` timestamp.
- When setting `state` to `done`, set `done` to the current ISO 8601 timestamp.
- When reopening a task, set `done` to `null`.
- Keep task context and decisions in the free-form Markdown body.
- Do not introduce sprint, assignee, label, or dependency metadata into the header.
- Run `backlog check` after any direct file edit.

## Side-project conventions

- Use priority `1` for work required for its assigned release, `2` for important
  release work, `3` for normal work, `4` for a candidate that can move later, and
  `5` for a parked idea. Priorities are buckets, not a substitute for drag ordering.
- Size `S` is isolated and readily testable; `M` is multiple related changes in one
  layer; `L` is cross-cutting or a new app; `null` means insufficiently understood.
  Split work that grows beyond `L`.
- Keep most tasks in `todo`; normally have one main `in_progress` task, occasionally
  two. Mark `done` only after tests, documentation, and manual validation. No sprint
  ceremony is needed.

Use `backlog --help` for the complete command reference.
