/**
 * KG v2 tool gate. Distinct du v1 (`server/src/kg/mode.ts`) :
 *
 *   KG_V2_TOOLS = (unset) | on | 1 | true | yes  → kg_v2_* REGISTERED (default)
 *   KG_V2_TOOLS = off | 0 | false | no           → kg_v2_* ABSENT
 *
 * Les tools v2 sont read-only (la projection Revit→KG est embarquée dans
 * le commandset C# ; l'agent ne mute pas le KG, seul Revit le fait).
 */
function rawMode(): string {
  return (process.env.KG_V2_TOOLS ?? "on").trim().toLowerCase();
}

export function kgV2ToolsEnabled(): boolean {
  const m = rawMode();
  return !(m === "off" || m === "0" || m === "false" || m === "no");
}

let logged = false;
export function logKgV2ModeOnce(): void {
  if (logged) return;
  logged = true;
  const src =
    process.env.KG_V2_TOOLS !== undefined
      ? `KG_V2_TOOLS=${process.env.KG_V2_TOOLS}`
      : "default";
  process.stderr.write(
    `[kg-v2] tools ${kgV2ToolsEnabled() ? "ENABLED" : "DISABLED"} (${src})\n`
  );
}
