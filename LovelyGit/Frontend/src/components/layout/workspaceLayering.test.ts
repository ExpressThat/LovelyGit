import { describe, expect, it } from "vitest";
import {
	horizontalPanelHandleClassName,
	workspaceDrillInLayerClassName,
} from "./workspaceLayering";

describe("workspace layering", () => {
	it("keeps drill-in surfaces above background panel resize handles", () => {
		expect(zIndex(workspaceDrillInLayerClassName)).toBeGreaterThan(
			zIndex(horizontalPanelHandleClassName),
		);
	});
});

function zIndex(className: string) {
	const token = className.split(" ").find((value) => /^z-\d+$/.test(value));
	if (!token) throw new Error(`Missing numeric z-index in ${className}`);
	return Number(token.slice(2));
}
