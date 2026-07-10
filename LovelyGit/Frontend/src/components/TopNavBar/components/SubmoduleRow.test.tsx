// @vitest-environment jsdom
import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { SubmoduleRow } from "./SubmoduleRow";

describe("SubmoduleRow", () => {
	it("names and invokes the destructive action for its submodule", () => {
		const onDeinitialize = vi.fn();
		render(
			<SubmoduleRow
				busy={false}
				disabled={false}
				onDeinitialize={onDeinitialize}
				onRun={vi.fn()}
				submodule={{
					branch: "main",
					currentCommit: "1".repeat(40),
					expectedCommit: "1".repeat(40),
					name: "library",
					path: "deps/library",
					state: "Current",
					url: "../library.git",
				}}
			/>,
		);

		fireEvent.click(
			screen.getByRole("button", { name: "Deinitialize library" }),
		);

		expect(onDeinitialize).toHaveBeenCalledOnce();
	});
});
