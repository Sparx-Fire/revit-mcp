/**
 * KG v2 client — thin JSON-RPC wrapper sur withRevitConnection.
 * Pas de KgService (whole-blob v1) ; chaque appel = 1 socket round-trip
 * direct vers la commande C# correspondante (RevitMCPKgCommandSet).
 *
 * Read-only : aucune écriture côté agent (la projection embarquée mute
 * le KG via DocumentChanged hook, l'agent n'a aucune surface mutation).
 */
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function unwrapAi<T>(res: any, op: string): T {
  if (res == null || typeof res !== "object") {
    throw new Error(`${op}: empty Revit response`);
  }
  const success = res.Success ?? res.success;
  const message = res.Message ?? res.message;
  const response = res.Response ?? res.response;
  if (success === false) {
    throw new Error(message || `${op} failed (Revit)`);
  }
  return response as T;
}

export async function callV2<T>(method: string, params: Record<string, unknown>): Promise<T> {
  const res = await withRevitConnection((c) => c.sendCommand(method, params));
  return unwrapAi<T>(res, method);
}

export function mcpResult(payload: unknown) {
  return {
    content: [
      { type: "text" as const, text: JSON.stringify(payload, null, 2) },
    ],
  };
}

export function mcpError(error: unknown) {
  const msg = error instanceof Error ? error.message : String(error);
  return {
    content: [{ type: "text" as const, text: `Error: ${msg}` }],
    isError: true,
  };
}
