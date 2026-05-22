/**
 * kg-v2-register-commands.mjs — script idempotent qui synchronise les
 * commandes du commandset-kg dans le commandRegistry.json du plugin.
 *
 * Lit `commandset-kg/command.json` (source de vérité v2) et s'assure que
 * chaque commande qui y figure a une entrée correspondante dans
 * `%APPDATA%/Autodesk/Revit/Addins/2025/revit_mcp_plugin/Commands/commandRegistry.json`.
 *
 * Dédup par commandName. Idempotent : re-runs sont safe, n'ajoute que les
 * manquants. Modifie le fichier en place + dump un résumé.
 *
 * Usage :
 *   node server/scripts/kg-v2-register-commands.mjs
 *
 * Pré-conditions : plugin déployé (commandRegistry.json existe).
 * Post : Revit doit être (re)démarré ou Switch off→on pour relire le fichier.
 */
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..");
const SOURCE_JSON = path.join(REPO_ROOT, "commandset-kg", "command.json");
const TARGET_JSON = path.join(
  process.env.APPDATA,
  "Autodesk",
  "Revit",
  "Addins",
  "2025",
  "revit_mcp_plugin",
  "Commands",
  "commandRegistry.json"
);

const REVIT_VERSION = "2025";

function readJson(p) {
  return JSON.parse(fs.readFileSync(p, "utf8"));
}

function writeJson(p, obj) {
  fs.writeFileSync(p, JSON.stringify(obj, null, 2) + "\n", "utf8");
}

function buildEntry(srcCommand) {
  return {
    commandName: srcCommand.commandName,
    assemblyPath: `RevitMCPKgCommandSet\\{VERSION}\\RevitMCPKgCommandSet.dll`,
    enabled: true,
    supportedRevitVersions: [REVIT_VERSION],
    developer: {
      name: "mcp-servers-for-revit-kg-poc",
      email: "",
      website: "",
      organization: "mcp-servers-for-revit-kg-poc",
    },
    description: srcCommand.description ?? "",
  };
}

function main() {
  if (!fs.existsSync(SOURCE_JSON)) {
    console.error(`[err] source manquante : ${SOURCE_JSON}`);
    process.exit(1);
  }
  if (!fs.existsSync(TARGET_JSON)) {
    console.error(
      `[err] registry manquante : ${TARGET_JSON}\n` +
        "Le plugin doit être déployé d'abord (Switch on dans Revit pour générer le fichier)."
    );
    process.exit(1);
  }

  const src = readJson(SOURCE_JSON);
  const reg = readJson(TARGET_JSON);
  if (!Array.isArray(reg.Commands)) reg.Commands = [];

  const existingNames = new Set(reg.Commands.map((c) => c.commandName));
  const added = [];
  const skipped = [];

  for (const cmd of src.commands ?? []) {
    if (existingNames.has(cmd.commandName)) {
      skipped.push(cmd.commandName);
      continue;
    }
    reg.Commands.push(buildEntry(cmd));
    added.push(cmd.commandName);
  }

  if (added.length === 0) {
    console.log(`[ok] aucune entrée à ajouter (${skipped.length} déjà présentes).`);
    return;
  }

  // Validation JSON puis écriture
  const out = JSON.stringify(reg, null, 2);
  JSON.parse(out); // throws si invalide
  writeJson(TARGET_JSON, reg);

  console.log(`[ok] ${added.length} entrée(s) ajoutée(s) :`);
  for (const n of added) console.log(`     + ${n}`);
  if (skipped.length > 0) {
    console.log(`     (${skipped.length} déjà présente(s) : ${skipped.join(", ")})`);
  }
  console.log(
    `\nFichier : ${TARGET_JSON}\n` +
      "Action : redémarre Revit, ou Switch off → Switch on pour relire."
  );
}

main();
