// @vitest-environment jsdom

import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import { BranchPicker } from "./BranchPicker";

describe("BranchPicker", () => {
	it("keeps a maximum-size branch set out of the DOM until opened", async () => {
		const user = userEvent.setup();
		renderPicker({ options: manyBranches() });

		expect(screen.queryAllByRole("option")).toHaveLength(0);
		await user.click(screen.getByRole("combobox", { name: "Local branch" }));
		expect(await screen.findAllByRole("option")).toHaveLength(10);
		expect(
			document.querySelector("[data-branch-picker-list='virtual']"),
		).toBeInTheDocument();
	});

	it("filters to and selects an option outside the initial virtual window", async () => {
		const user = userEvent.setup();
		const onValueChange = vi.fn();
		renderPicker({ onValueChange, options: manyBranches() });

		await user.click(screen.getByRole("combobox", { name: "Local branch" }));
		await user.type(
			await screen.findByRole("combobox", { name: "Filter local branch" }),
			"branch-09999",
		);
		await user.click(
			await screen.findByRole("option", { name: "perf/branch-09999" }),
		);
		expect(onValueChange).toHaveBeenCalledWith("perf/branch-09999");
	});

	it("filters from the native input event used by the desktop WebView", async () => {
		const user = userEvent.setup();
		renderPicker({ options: manyBranches() });
		await user.click(screen.getByRole("combobox", { name: "Local branch" }));

		fireEvent.input(
			await screen.findByRole("combobox", { name: "Filter local branch" }),
			{ target: { value: "branch-09999" } },
		);

		expect(
			await screen.findByRole("option", { name: "perf/branch-09999" }),
		).toBeVisible();
	});

	it("supports keyboard selection from the focused search input", async () => {
		const user = userEvent.setup();
		const onValueChange = vi.fn();
		renderPicker({ onValueChange, options: ["main", "release"] });

		await user.click(screen.getByRole("combobox", { name: "Local branch" }));
		const input = await screen.findByRole("combobox", {
			name: "Filter local branch",
		});
		await user.click(input);
		await user.keyboard("{ArrowDown}{ArrowDown}{Enter}");
		expect(onValueChange).toHaveBeenCalledWith("release");
	});

	it("surfaces an empty filter and prevents opening while disabled", async () => {
		const user = userEvent.setup();
		const view = renderPicker({ options: ["main"] });
		await user.click(screen.getByRole("combobox", { name: "Local branch" }));
		await user.type(
			await screen.findByRole("combobox", { name: "Filter local branch" }),
			"missing",
		);
		expect(screen.getByText("No matching local branches.")).toBeVisible();

		view.unmount();
		renderPicker({ disabled: true, options: ["main"] });
		const trigger = screen.getByRole("combobox", { name: "Local branch" });
		expect(trigger).toBeDisabled();
		await user.click(trigger);
		expect(
			screen.queryByRole("combobox", { name: "Filter local branch" }),
		).not.toBeInTheDocument();
	});
});

function manyBranches() {
	return Array.from(
		{ length: 10_000 },
		(_, index) => `perf/branch-${index.toString().padStart(5, "0")}`,
	);
}

function renderPicker({
	disabled = false,
	onValueChange = vi.fn(),
	options,
}: {
	disabled?: boolean;
	onValueChange?: (value: string) => void;
	options: string[];
}) {
	function ControlledPicker() {
		const [value, setValue] = useState("");
		return (
			<BranchPicker
				ariaLabel="Local branch"
				disabled={disabled}
				emptyMessage="No matching local branches."
				onValueChange={(nextValue) => {
					setValue(nextValue);
					onValueChange(nextValue);
				}}
				options={options}
				placeholder="Choose a branch"
				value={value}
			/>
		);
	}
	return render(<ControlledPicker />);
}
