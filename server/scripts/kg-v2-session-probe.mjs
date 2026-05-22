/**
 * kg-v2-session-probe.mjs — appelle `kg_session_info` (RevitMCPKgCommandSet)
 * via le socket Revit BRUT (port 8080), sans Claude, sans serveur TS, sans
 * harness. But unique : vérifier que la projection KG v2 a tourné après un
 * commit Revit (gate P2 du DESIGN-kg-v2 §9).
 *
 * Prérequis :
 *   1. plugin v1 + commandset-kg DÉPLOYÉS (commandset-kg-tests passent /
 *      RevitMCPKgCommandSet.dll présent dans Addins\<ver>\revit_mcp_plugin\
 *      Commands\RevitMCPKgCommandSet\<ver>\).
 *   2. `commandRegistry.json` (étape A) contient l'entrée `kg_session_info`
 *      pointant vers `RevitMCPKgCommandSet\{VERSION}\RevitMCPKgCommandSet.dll`.
 *   3. Revit 2025 ouvert sur un `.rvt` (stage2-bench.rvt fait l'affaire),
 *      plugin chargé, **Switch** cliqué.
 *
 * Usage (depuis le node de l'env conda revitmcp) :
 *
 *   node server/scripts/kg-v2-session-probe.mjs              # juste lire
 *   node server/scripts/kg-v2-session-probe.mjs --watch=10   # lire 10 fois,
 *                                                            # 2s d'intervalle
 *
 * Sortie : 1 ligne par appel, `project_id / doc_title / turn / nodes / edges /
 * last_action`. Si la commande renvoie une erreur, on l'affiche en clair.
 *
 * Gate P2 que ce probe instrumente :
 *   - Au démarrage (bootstrap eager L-7) : nodes > 0, edges > 0 si le .rvt
 *     contient déjà des Levels/Walls/WindowTypes etc.
 *   - Après un `create_line_based_element` (Wall) : turn incrémenté, nodes +1,
 *     edges +2 (at_level + is_type), last_action ≈ "turn N: create wall_NNN"
 *   - Après un `delete_element` : last_action ≈ "turn N: delete wall_NNN",
 *     node soft-deleted (toujours compté dans nodes).
 */
import net from "node:net";

const ARGS = process.argv.slice(2);
const PORT = Number(ARGS.find((a) => /^\d+$/.test(a)) || 8080);
const HOST = "127.0.0.1";
const optN = (k, d) => {
  const a = ARGS.find((x) => x.startsWith(`--${k}=`));
  return a ? Number(a.split("=")[1]) : d;
};
const WATCH = optN("watch", 0);
const INTERVAL_MS = Math.max(500, optN("interval-ms", 2000));

function rpc(method, params = {}) {
  return new Promise((resolve, reject) => {
    const sock = net.connect(PORT, HOST);
    let buf = "";
    const to = setTimeout(() => {
      sock.destroy();
      reject(new Error(`timeout ${method}`));
    }, 15000);
    sock.on("connect", () =>
      sock.write(JSON.stringify({ jsonrpc: "2.0", method, params, id: "p1" }))
    );
    sock.on("data", (d) => {
      buf += d.toString();
      try {
        const m = JSON.parse(buf);
        clearTimeout(to);
        sock.end();
        m.error
          ? reject(new Error(m.error.message || "Revit error"))
          : resolve(m.result);
      } catch {
        /* trame incomplète */
      }
    });
    sock.on("error", (e) => {
      clearTimeout(to);
      reject(e);
    });
  });
}

function fmtLine(res) {
  const success = res?.Success ?? res?.success;
  if (success === false) {
    return `✖ ${res?.Message ?? res?.message ?? "(no message)"}`;
  }
  const r = res?.Response ?? res?.response ?? {};
  const pid = r.ProjectId ?? r.projectId ?? "(empty)";
  const title = r.DocTitle ?? r.docTitle ?? "(empty)";
  const turn = r.Turn ?? r.turn ?? 0;
  const nodes = r.NodeCount ?? r.nodeCount ?? 0;
  const edges = r.EdgeCount ?? r.edgeCount ?? 0;
  const pending = r.PendingDeltaCount ?? r.pendingDeltaCount ?? 0;
  const esLen = r.EsJournalLength ?? r.esJournalLength ?? 0;
  const last = r.LastActionSummary ?? r.lastActionSummary ?? "(none)";
  return [
    `pid=${pid.slice(0, 12)}…`,
    `doc="${title}"`,
    `turn=${turn}`,
    `nodes=${nodes}`,
    `edges=${edges}`,
    `pending=${pending}`,
    `es=${esLen}B`,
    `last="${last}"`,
  ].join("  ");
}

async function once() {
  const res = await rpc("kg_session_info", {});
  console.log(new Date().toISOString().slice(11, 19), fmtLine(res));
}

async function main() {
  console.log(`[kg-v2-session-probe] -> ${HOST}:${PORT}  watch=${WATCH}\n`);
  if (WATCH <= 1) {
    await once();
    return;
  }
  for (let i = 0; i < WATCH; i++) {
    try {
      await once();
    } catch (e) {
      console.error(`[err] ${e.message}`);
    }
    if (i < WATCH - 1) await new Promise((r) => setTimeout(r, INTERVAL_MS));
  }
}

main().catch((e) => {
  console.error(`\n[kg-v2-session-probe] ERREUR FATALE: ${e.message}`);
  console.error(
    "Vérifier : Revit ouvert + Switch + plugin chargé + commandRegistry.json wiré (étape A)."
  );
  process.exit(2);
});
