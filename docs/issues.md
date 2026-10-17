# Planned work (GitHub Issues)

How issues are written and worked (moved from `AGENTS.md`).

Planned work lives in [GitHub Issues](https://github.com/vmihalachi/laching/issues), not in files: `gh issue list`,
`gh issue view N`. One issue per piece of work, saying what's already known (measurements, what was tried, links to the
code or the `plans/` record), the options when there's a choice to make, and what done looks like. Labels: `windows`,
`hand-check` (needs a person at a real machine), `decision` (the owner chooses before work starts), `performance`,
`accessibility`, `enhancement`, `bug`.

- Read the issue before starting, and work only on what it says. A `decision` issue waits for the owner's choice in
  its comments.
- Write what you found as a comment as you go, tick its checkboxes as they're done, and close it from the commit or
  PR that finishes it (`Fixes #N`).
- Something found along the way that isn't this change's job is a new issue, not a note in a file.
- A release-sized design is one issue with its pieces as sub-issues, each written to be started cold.
