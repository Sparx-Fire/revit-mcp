import * as fs from "fs";
import * as os from "os";
import * as path from "path";

const TTL_MS = 60_000;

const INVALIDATION_DIR = path.join(
  os.homedir(),
  ".mcp-servers-for-revit",
  "model-cache-invalidation"
);

interface CacheEntry {
  data: Record<string, unknown>;
  cachedAt: number;
  projectName: string;
  includeDetailedTypes: boolean;
}

function buildKey(projectName: string, includeDetailedTypes: boolean): string {
  return `${projectName}:${includeDetailedTypes}`;
}

function sanitizeProjectName(projectName: string): string {
  return projectName.replace(/[<>:"/\\|?*]/g, "_");
}

function readInvalidationTimestamp(fileName: string): number {
  try {
    const filePath = path.join(INVALIDATION_DIR, fileName);
    if (!fs.existsSync(filePath)) {
      return 0;
    }

    const timestamp = parseInt(fs.readFileSync(filePath, "utf8").trim(), 10);
    return Number.isFinite(timestamp) ? timestamp : 0;
  } catch {
    return 0;
  }
}

class ModelStatisticsCache {
  private entries = new Map<string, CacheEntry>();
  private invalidationTimestamps = new Map<string, number>();
  private lastKnownProjectName: string | null = null;

  get(
    projectName: string,
    includeDetailedTypes: boolean
  ): Record<string, unknown> | null {
    const key = buildKey(projectName, includeDetailedTypes);
    const entry = this.entries.get(key);
    if (!entry) {
      return null;
    }

    if (Date.now() - entry.cachedAt > TTL_MS) {
      this.entries.delete(key);
      return null;
    }

    if (this.isInvalidatedSince(entry.projectName, entry.cachedAt)) {
      return null;
    }

    return entry.data;
  }

  set(
    projectName: string,
    includeDetailedTypes: boolean,
    data: Record<string, unknown>
  ): void {
    const key = buildKey(projectName, includeDetailedTypes);
    this.entries.set(key, {
      data,
      cachedAt: Date.now(),
      projectName,
      includeDetailedTypes,
    });
    this.lastKnownProjectName = projectName;
  }

  invalidate(projectName?: string): void {
    const timestamp = Date.now();
    if (projectName) {
      this.invalidationTimestamps.set(projectName, timestamp);
      for (const [key, entry] of this.entries) {
        if (entry.projectName === projectName) {
          this.entries.delete(key);
        }
      }
    } else {
      this.invalidationTimestamps.set("*", timestamp);
      this.entries.clear();
    }
  }

  invalidateAll(): void {
    this.invalidate();
  }

  setLastKnownProjectName(projectName: string): void {
    this.lastKnownProjectName = projectName;
  }

  getLastKnownProjectName(): string | null {
    return this.lastKnownProjectName;
  }

  updateLastKnownProjectNameFromResponse(result: unknown): void {
    if (
      result &&
      typeof result === "object" &&
      "projectName" in result &&
      typeof (result as { projectName: unknown }).projectName === "string"
    ) {
      this.lastKnownProjectName = (result as { projectName: string }).projectName;
    }
  }

  private isInvalidatedSince(projectName: string, cachedAt: number): boolean {
    const globalInvalidation = this.invalidationTimestamps.get("*") ?? 0;
    const projectInvalidation =
      this.invalidationTimestamps.get(projectName) ?? 0;
    const fileGlobalInvalidation = readInvalidationTimestamp("_global.ts");
    const fileProjectInvalidation = readInvalidationTimestamp(
      `${sanitizeProjectName(projectName)}.ts`
    );

    return (
      globalInvalidation > cachedAt ||
      projectInvalidation > cachedAt ||
      fileGlobalInvalidation > cachedAt ||
      fileProjectInvalidation > cachedAt
    );
  }
}

export const modelStatisticsCache = new ModelStatisticsCache();

const MUTATING_COMMANDS = new Set([
  "create_point_based_element",
  "create_line_based_element",
  "create_surface_based_element",
  "create_level",
  "create_grid",
  "create_room",
  "create_sheet",
  "create_schedule",
  "create_door_schedule",
  "create_window_schedule",
  "create_floor_schedule",
  "create_finish_schedule",
  "create_dimensions",
  "dimension_room_walls",
  "create_structural_framing_system",
  "delete_element",
  "operate_element",
  "color_splash",
  "tag_walls",
  "tag_rooms",
  "place_view_on_sheet",
  "send_code_to_revit",
]);

export function shouldInvalidateCacheForCommand(command: string): boolean {
  return MUTATING_COMMANDS.has(command);
}

export function invalidateCacheForBatchResults(result: unknown): void {
  if (!result || typeof result !== "object" || !("results" in result)) {
    return;
  }

  const results = (result as { results: unknown }).results;
  if (!Array.isArray(results)) {
    return;
  }

  for (const item of results) {
    if (
      item &&
      typeof item === "object" &&
      "success" in item &&
      (item as { success: unknown }).success === true &&
      "command" in item &&
      typeof (item as { command: unknown }).command === "string" &&
      shouldInvalidateCacheForCommand((item as { command: string }).command)
    ) {
      invalidateModelStatisticsCache();
      return;
    }
  }
}

export function invalidateModelStatisticsCache(projectName?: string): void {
  modelStatisticsCache.invalidate(projectName);
}
