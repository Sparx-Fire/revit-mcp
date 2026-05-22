/**
 * kg-v2-probe.mjs — probe générique JSON-RPC pour les commandes KG v2.
 * Accepte un nom de méthode + un JSON de paramètres, dump la réponse.
 *
 * Usage :
 *   node server/scripts/kg-v2-probe.mjs <method> [params_json]
 *
 * Exemples :
 *   node server/scripts/kg-v2-probe.mjs kg_session_info
 *   node server/scripts/kg-v2-probe.mjs kg_query "{\"node_type\":\"Wall\"}"
 *   node server/scripts/kg-v2-probe.mjs kg_query "{\"node_type\":\"Level\",\"attrs_filter\":{\"name\":\"N0\"}}"
 *   node server/scripts/kg-v2-probe.mjs kg_diff_since "{\"since_turn\":0}"
 *   node server/scripts/kg-v2-probe.mjs kg_get_by_revit_id "{\"revit_id\":12345}"
 *   node server/scripts/kg-v2-probe.mjs kg_traverse "{\"start_id\":\"wall_001\",\"path\":[{\"edge_type\":\"at_level\",\"direction\":\"out\"}]}"
 *
 * Sortie : Success/Message ligne courte + Response en JSON indenté (limité à
 * ~50 nœuds pour éviter d'inonder la console).
 */
import net from "node:net";

const ARGS = process.argv.slice(2);
const METHOD = ARGS[0];
const PARAMS_JSON = ARGS[1] || "{}";
const PORT = 8080;
const HOST = "127.0.0.1";

if (!METHOD) {
  console.error(
    "Usage : node server/scripts/kg-v2-probe.mjs <method> [params_json]\n" +
      "Méthodes : kg_session_info | kg_query | kg_diff_since | kg_get_by_revit_id | kg_traverse"
  );
  process.exit(64);
}

let params;
try {
  params = JSON.parse(PARAMS_JSON);
} catch (e) {
  console.error(`[parse] JSON params invalide : ${e.message}`);
  process.exit(65);
}

function rpc(method, p) {
  return new Promise((resolve, reject) => {
    const sock = net.connect(PORT, HOST);
    let buf = "";
    const to = setTimeout(() => {
      sock.destroy();
      reject(new Error(`timeout ${method}`));
    }, 15000);
    sock.on("connect", () =>
      sock.write(JSON.stringify({ jsonrpc: "2.0", method, params: p, id: "p1" }))
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

function summarize(res) {
  const success = res?.Success ?? res?.success;
  const msg = res?.Message ?? res?.message ?? "(no message)";
  const resp = res?.Response ?? res?.response;
  return { success, msg, resp };
}

function cap(obj, key, n) {
  if (!obj || !Array.isArray(obj[key])) return obj;
  if (obj[key].length <= n) return obj;
  const truncated = { ...obj };
  truncated[key] = obj[key].slice(0, n);
  truncated[`_${key}_truncated_to`] = `${n}/${obj[key].length}`;
  return truncated;
}

async function main() {
  console.log(`[kg-v2-probe] ${METHOD}  ${PARAMS_JSON}\n`);
  const res = await rpc(METHOD, params);
  const { success, msg, resp } = summarize(res);
  console.log(`${success === false ? "✖" : "✓"}  ${msg}\n`);
  if (resp != null) {
    let capped = resp;
    capped = cap(capped, "Nodes", 50);
    capped = cap(capped, "nodes", 50);
    capped = cap(capped, "Entries", 50);
    capped = cap(capped, "entries", 50);
    capped = cap(capped, "Reached", 50);
    capped = cap(capped, "reached", 50);
    console.log(JSON.stringify(capped, null, 2));
  }
}

main().catch((e) => {
  console.error(`\n[kg-v2-probe] ERREUR FATALE: ${e.message}`);
  console.error(
    "Vérifier : Revit ouvert + Switch + plugin chargé + commandRegistry.json wiré pour cette commande."
  );
  process.exit(2);
});
