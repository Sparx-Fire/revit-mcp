import { z } from "zod";
import { withRevitConnection } from "../utils/ConnectionManager.js";
export function registerLoadFamilyTool(server) {
    server.tool("load_family", "Load a .rfa family file into the Revit project. / 将.rfa族文件载入到Revit项目中。", {
        filePath: z.string().describe("Full path to the .rfa family file / .rfa族文件的完整路径"),
        familyName: z.string().optional().describe("Expected family name after loading (optional) / 载入后续的族名称（可选）"),
    }, async (args, extra) => {
        const params = args;
        try {
            const response = await withRevitConnection(async (revitClient) => {
                return await revitClient.sendCommand("load_family", params);
            });
            return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
        }
        catch (error) {
            return { content: [{ type: "text", text: `Load family failed: ${error instanceof Error ? error.message : String(error)}` }] };
        }
    });
}
