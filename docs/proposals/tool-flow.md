# Proposal: tool flow

**Status:** idea, not designed (recorded 2026-09-12). "Tool flow" is a working name.

## What it is

A pipeline that runs tools in a defined order, with the output of one step feeding the input of the next, plus control flow around the steps: run in parallel, loop over items, branch on a condition. Today the CLI gets a small version of this from the shell (`dev-tools base64-encoder < file | dev-tools base64-decoder`); a flow would make it a first-class feature that any frontend can define and run.

## Why it matters now

`[PipeInput]` and `[PipeOutput]` are not only a CLI convenience. They are the contract a flow connects tools through: the one value a tool takes in and the one value it hands on. Work on the tool contract should keep that contract sound, even before a flow exists:

- **One in, one out.** A tool has at most one `[PipeInput]` and one `[PipeOutput]` (analyzer rules DT1009 and DT1010). A flow step's other arguments are configured, not piped.
- **Values must travel between tools unchanged.** No frontend may reshape a pipe value on the way through. The CLI used to wrap its output at 80 columns, which put newlines into piped data; a flow engine would have hit the same class of bug.
- **Pipe types must connect.** Today pipe values are `string` in and `string` or `string[]` out. Phase 5 adds `ToolContent` for file and binary data. Whatever the set of pipe types becomes, each output type needs a defined conversion to each input type: `string` → `ToolContent` is implicit, `ToolContent` → `string` is a text decode that can fail, and a collection output is a natural input to a loop.
- **The tool does not care where its input came from** — typed on the command line, uploaded, loaded from a file, or produced by the previous step.
- **Failure and cancellation already have a shape**: a step that fails returns `ToolError`s a flow can stop or branch on, and a cancellation token passes through every tool.

## Questions for when it is designed

- How a flow is defined (a file format, a builder API, a Web UI) and which frontends can run one.
- The name.
- How a step's non-piped arguments are set, and whether a flow can feed a step's result field other than its `[PipeOutput]`.
- Type checking of a flow before it runs, from the catalog's pipe input and output types.
