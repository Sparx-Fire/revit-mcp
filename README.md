# Fork of `mcp-servers-for-revit` (Knowledge Graph layer)

## Overview

This is a fork of **mcp-servers-for-revit** that adds a **Knowledge Graph command
set** (`commandset-kg/`) on top of the base Revit MCP server. Where the upstream
tools let an agent read and edit a Revit model, the KG layer gives that agent a
persistent, queryable representation of the project as a **graph of nodes and
typed edges** — a structured memory it can reason over across a long session.

The graph stays faithful to the live model through a **`DocumentChanged` hook**:
every create / modify / delete in Revit is projected into the graph
automatically, so the BIM model and its graph view never drift apart — no manual
sync, no stale snapshot.

Its main value is to add an **annotative and relational layer that is independent
of Revit's native element structure**. On top of the geometry and parameters
Revit already exposes, the graph carries explicit typed relationships and
semantic annotations (e.g. `replaced_by`, `violates_rule`, hosting and dependency
chains). This unlocks a **semantic and relational level of reading** — multi-hop
queries, impact and provenance analysis, drift detection — that the flat Revit
API does not offer natively.

### Nodes: a structural base, then beyond it

The graph has two tiers of nodes. The **structural base** mirrors the Revit model:
node types derived from BIM elements (`Wall`, `Room`, `Level`, `Window`, `Door`,
`WallType`, …), each with a fixed schema and a `revit_id` binding, kept in sync by
the `DocumentChanged` hook. But the graph is not limited to that mirror — an agent
can also **create user-defined node types on the fly, with custom properties**
(`kg_v2_create_node`, e.g. a `Suite` or `Zone` carrying free-form attrs), and link
them to the structural nodes (`contains` membership edges). These nodes carry no
`revit_id`: they live purely in the graph, untouched by Revit's projection and
drift detection, and persist across sessions.

So the graph deliberately **steps outside Revit's base element schema** and evolves
toward something else — a richer, partly agent-authored domain model of the project
(programmatic units, design intent, semantic groupings) layered on top of the BIM
elements rather than constrained by them.

## Benchmark

An early benchmark pitted a **flat key-value store** (`store_*_data`) against an
**external KG sidecar**, with a large gap in the KG's favour. The cause was
**typing**: the flat store is schemaless (a generic *rooms* table), so to fake
persisting a BIM project the agent **mis-types walls/windows into the rooms
table** — fabrications a structural check catches (~10/17 scenarios), at ~3.5–7×
the cost per *correct* answer. The typed KG schema (Wall/Window/Level + relations)
fabricated none. But that was a *weak* baseline (a generic store, not Revit). A
later benchmark therefore re-tests the graph against the strongest baseline:
querying the **live Revit model directly** (A).

It compares **A** (Revit-direct, no-KG baseline) against **C** (the KG layer) on
a ~500-element model, over query, edit, relational and temporal scenarios.
Cost = real output tokens (Claude Code runs); correctness scored claim-vs-state.

- **Reliability** — C ≥ A on every scenario, **0 fabrication** on both sides.
- **Capability** — C does what the flat Revit API structurally cannot:
  cross-session diff, queryable audit trail (`replaced_by`), provenance through
  deleted elements, structural drift detection.
- **Cost** — with the indexed read-API, C is **~2–3× cheaper** on scoped
  query/edit; it costs more only when it does strictly more (e.g. 30/30 edits vs
  A's 25/30) or for capabilities A lacks.
- **Takeaway** — the graph's advantage is real on **relational/temporal**
  workloads and scales with **query scope, not model size**.

Full numbers, per-scenario verdicts and prompts: **`benchmark/`** (`VERDICT.md` +
the two PDFs).

## Install

> Prerequisites (Revit version, .NET SDK, Node, NuGet, base add-in setup) are
> described in the upstream project — see `README_MAIN.md`.

1. **Build the add-in + command sets** (Debug auto-deploys to
   `%AppData%\Autodesk\Revit\Addins\<ver>\`; `R25` = Revit 2025 — use your version):
   ```
   dotnet build plugin/RevitMCPPlugin.csproj             -c "Debug R25"
   dotnet build commandset/RevitMCPCommandSet.csproj     -c "Debug R25"
   dotnet build commandset-kg/RevitMCPKgCommandSet.csproj -c "Debug R25"
   ```
2. **Build the MCP server (TypeScript):**
   ```
   cd server && npm install && npm run build
   ```
3. **In Revit:** load the add-in → ribbon *Revit MCP Plugin* → *Settings* (enable
   the command sets) → **Switch** (starts the socket on `localhost:8080`) → open
   your `.rvt`.

4. **Open Claude Code from the repo root.** First **activate the virtual
   environment** from the upstream setup — it provides **Node.js**, which the MCP
   server runs on (no Node available ⇒ no MCP communication at all). A `.mcp.json`
   is committed here (runs `server/build/index.js`, `kg_v2_*` on by default), so
   launching Claude Code from this directory auto-registers the `revit` server.
   Approve it once; `/mcp` should then list the `revit` tools.

5. Happy prompting!

## Fork file tree

| Rendering | Marker | Meaning |
|---|---|---|
| ⚫ **black** (context) | ` ` (plain line) | **original** — upstream |
| 🔴 **red** | `-` at line start | **new** — KG layer |
| 🟢 **green** | `+` at line start | **modified** — by the KG layer |

> Relies on GitHub's `diff` coloring. The `-` / `+` markers are coloring
> artifacts (not a real git diff).

```diff
   .
   ├── commandset/                            Revit command set (core)
-  │   ├── Commands/BatchSetParametersCommand.cs
-  │   ├── Services/BatchSetParametersEventHandler.cs
+  │   ├── Services/*EventHandler.cs  (14: swallow-warnings)
-  │   ├── Models/Common/
-  │   ├── Utils/SwallowWarningsPreprocessor.cs
   │   └── … (Commands/Services/Models, upstream)
-  ├── commandset-kg/                         the KG — read-only C# projection
-  │   ├── Commands/
-  │   │     • kg_v2_query
-  │   │     • kg_v2_traverse
-  │   │     • kg_v2_diff_since
-  │   │     • kg_v2_get_by_revit_id
-  │   │     • kg_v2_session_info
-  │   │     • kg_v2_detect_drift
-  │   │     • kg_v2_resolve_drift
-  │   │     • kg_v2_annotate
-  │   ├── Core/
-  │   │     • ProjectKg
-  │   │     • Projection
-  │   │     • PathTraversal
-  │   │     • NodeQueryFilter
-  │   │     • NodeAggregator
-  │   │     • NodeJoiner
-  │   ├── Services/
-  │   │     • KgV2DocumentWatcher
-  │   │     • EsDeltaSink
-  │   │     • KgV2ExtensibleStorage
-  │   └── Models/
-  │         • KgQueryResult
-  │         • KgTraverseResult
-  │         • KgNodeView
-  │         • NodeViewBuilder
-  ├── commandset-kg-tests/                   KG C# tests
   ├── plugin/                                Revit add-in (ribbon, socket :8080)
+  │   └── Core/SocketService.cs  (socket frame reassembly)
   ├── server/                                MCP server (TypeScript)
-  │   ├── src/kg-v2/                         KG tools client/mode
-  │   ├── src/tools/  kg_v2_*.ts             KG MCP tools
   │   ├── src/tools/ … · src/ …              (tools + core, upstream)
-  │   ├── scripts/  kg-v2-*.mjs              KG probes
+  │   └── package.json · tsconfig.json  (KG deps + config)
   ├── tests/                                 upstream C# tests
-  ├── benchmark/                             benchmark results
-  │   ├── VERDICT.md
-  │   ├── all-scenarios.pdf
-  │   ├── llm-responses.pdf
-  │   └── prompts/  (13 .txt)
-  ├── reference/
   ├── assets/ · scripts/
   ├── README_MAIN.md                         upstream README
-  ├── README.md                              (this file)
+  ├── command.json
+  ├── .gitignore
   └── mcp-servers-for-revit.sln · global.json
```

## New (KG layer)

- **`commandset-kg/`** — a C# projection of the Revit model maintained by the
  `DocumentChanged` hook, exposed **read-only** (`kg_v2_*`). Its indexed read-API
  (`NodeAggregator` = count/sum/mean/min/max + group_by; `NodeJoiner` = edge-aware
  join-projection; `PathTraversal` = variable-depth reachability) runs
  relational/aggregated queries graph-side. The KG commands ship as a
  **dedicated command set** (`RevitMCPKgCommandSet`, with its own `command.json`),
  registered independently of the upstream one — the layer is purely additive.
- **`server/src/kg-v2/` + `tools/kg_v2_*.ts`** — the `kg_v2_*` MCP tools (the
  read-only API into the projection).
- **`commandset-kg-tests/`**, **`server/scripts/kg-v2-*.mjs`** — tests and probes.
- **`benchmark/`** — benchmark deliverables (verdict, PDFs, prompts).

## Modified (upstream touched by the KG layer)

- **`commandset/Services/…EventHandler.cs`** (14) — `SwallowWarningsPreprocessor`
  wired onto the write Transactions (suppresses geometric warnings).
- **`plugin/Core/SocketService.cs`** — reassembly of incoming JSON-RPC frames.
- **`server/package.json` · `tsconfig.json`** — dependencies and config for the
  KG tools.
- **`.gitignore`** — exclusion rules.

