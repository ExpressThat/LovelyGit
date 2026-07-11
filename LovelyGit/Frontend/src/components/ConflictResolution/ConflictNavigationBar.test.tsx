// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ConflictNavigationBar } from "./ConflictNavigationBar";

describe("ConflictNavigationBar", () => {
	it("navigates, resets, omits, and supports keyboard resizing", async () => {
		const user = userEvent.setup();
		const onNavigate = vi.fn();
		const onOmit = vi.fn();
		const onReset = vi.fn();
		const onResizeBy = vi.fn();
		render(
			<ConflictNavigationBar
				active={1}
				count={3}
				disabled={false}
				onNavigate={onNavigate}
				onOmit={onOmit}
				onPointerDown={vi.fn()}
				onReset={onReset}
				onResizeBy={onResizeBy}
				resetDisabled={false}
				unresolved={2}
			/>,
		);

		await user.click(screen.getByRole("button", { name: "Previous conflict" }));
		await user.click(screen.getByRole("button", { name: "Next conflict" }));
		await user.click(screen.getByRole("button", { name: "Omit conflict" }));
		await user.click(screen.getByRole("button", { name: "Reset" }));
		await user.type(
			screen.getByRole("button", { name: "Resize source and output panels" }),
			"{ArrowUp}{ArrowDown}",
		);

		expect(onNavigate).toHaveBeenNthCalledWith(1, 0);
		expect(onNavigate).toHaveBeenNthCalledWith(2, 2);
		expect(onOmit).toHaveBeenCalledOnce();
		expect(onReset).toHaveBeenCalledOnce();
		expect(onResizeBy).toHaveBeenNthCalledWith(1, -2);
		expect(onResizeBy).toHaveBeenNthCalledWith(2, 2);
	});
});
