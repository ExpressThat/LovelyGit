// @vitest-environment jsdom

import { toast } from "sonner";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { copyToClipboard } from "./clipboard";

vi.mock("sonner", () => ({
	toast: { error: vi.fn(), success: vi.fn() },
}));

describe("copyToClipboard", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		Object.defineProperty(navigator, "clipboard", {
			configurable: true,
			value: { writeText: vi.fn() },
		});
	});

	it("uses the asynchronous clipboard and reports success", async () => {
		await copyToClipboard("C:/repo", "Repository path");
		expect(navigator.clipboard.writeText).toHaveBeenCalledWith("C:/repo");
		expect(toast.success).toHaveBeenCalledWith("Repository path copied");
	});

	it("falls back to a temporary selection when WebView permission fails", async () => {
		vi.mocked(navigator.clipboard.writeText).mockRejectedValueOnce(
			new Error("Denied"),
		);
		document.execCommand = vi.fn(() => true);
		await copyToClipboard("C:/repo", "Repository path");
		expect(document.execCommand).toHaveBeenCalledWith("copy");
		expect(document.querySelector("textarea")).toBeNull();
		expect(toast.success).toHaveBeenCalledWith("Repository path copied");
	});

	it("surfaces failure when both clipboard paths are unavailable", async () => {
		vi.mocked(navigator.clipboard.writeText).mockRejectedValueOnce(
			new Error("Denied"),
		);
		document.execCommand = vi.fn(() => false);
		await copyToClipboard("C:/repo", "Repository path");
		expect(toast.error).toHaveBeenCalledWith("Could not copy repository path");
	});
});
