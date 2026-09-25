# Project backlog

This directory is the project's source-of-truth backlog. Each task is one Markdown
file in `tasks/`, with a small YAML front matter header and a free-form body.

Common commands:

```console
backlog add "Describe the task"
backlog add "Describe the task" --template standard --context "Why now" --criteria "It is tested"
backlog add "Fix regression" --tag bug --tag web
backlog upgrade
backlog add-batch --file tasks.json --json
backlog list
backlog show T0001
backlog update T0001 --state in_progress
backlog update T0001 --state done
backlog check
backlog web
```

Metadata rules:

- `release` is `next`, `null`, or any release string.
- `priority` is the release ordering mechanism: `1` required, `2` important,
  `3` normal/default, `4` can move later, `5` parked idea. Do not use unique
  numbers to simulate a manually ordered list.
- `size` is `S` for an isolated, readily testable change; `M` for related changes
  in one layer; `L` for a cross-cutting feature or new app; `null` when it is not
  understood yet. Work larger than `L` is usually a release theme—split it.
- `state` is `todo`, `in_progress`, or `done`. Keep most work in `todo`, normally
  only one main task (occasionally two) in `in_progress`, and use `done` only after
  tests, documentation, and manual validation are complete.
- The body is free-form Markdown.
- `tags` is an optional ordered list of short labels, such as `["bug", "ios"]`.
  Omit it when a task does not need categorization; `list --tag TAG` is repeatable
  and requires every requested tag.

Prefer the CLI or local web app for changes so IDs, timestamps, and validation stay
consistent. The files remain intentionally readable and editable without the tool.

`backlog/.version` records the Personal Backlog version that last generated or
upgraded this integration. It is tracked alongside the backlog and is separate from
the `schema` value in `backlog.toml`. Run `backlog upgrade` to preview generated
README/AGENTS updates, then `backlog upgrade --apply` to write them and refresh the
version marker. On an interactive terminal, the preview ends with `Apply now? [y/N]`.

## Turning conversation context into backlog tasks

Ask an AI agent to read `AGENTS.md` before extracting context. The agent should
inspect existing tasks, update rather than duplicate them, and create one task per
independently actionable outcome. The task body should summarize why the work
exists, its desired outcome, established decisions and constraints, current
progress, and the next useful action. It should not copy an entire transcript.
