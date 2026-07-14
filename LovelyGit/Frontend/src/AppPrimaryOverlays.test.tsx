// @vitest-environment jsdom

import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import {
	type DeferredComponentLoader,
	DeferredPrimaryOverlay,
} from "./AppPrimaryOverlays";

describe("DeferredPrimaryOverlay", () => {
	it("renders a resolved component without Suspense retention", async () => {
		const loader = loaderFrom(() =>
			Promise.resolve(({ label }: { label: string }) => <div>{label}</div>),
		);

		render(
			<DeferredPrimaryOverlay
				fallback={<div>Loading</div>}
				loader={loader}
				props={{ label: "Ready" }}
			/>,
		);

		expect(screen.getByText("Loading")).toBeVisible();
		expect(await screen.findByText("Ready")).toBeVisible();
	});

	it("surfaces a failed chunk and retries it", async () => {
		const load = vi
			.fn<DeferredComponentLoader<{ label: string }>["load"]>()
			.mockRejectedValueOnce(new Error("unavailable"))
			.mockResolvedValueOnce(({ label }) => <div>{label}</div>);
		const loader = loaderFrom(load);

		render(
			<DeferredPrimaryOverlay
				fallback={<div>Loading</div>}
				loader={loader}
				props={{ label: "Recovered" }}
			/>,
		);

		fireEvent.click(
			await screen.findByRole("button", { name: "Retry opening tool" }),
		);
		expect(await screen.findByText("Recovered")).toBeVisible();
		expect(load).toHaveBeenCalledTimes(2);
	});
});

function loaderFrom(
	load: DeferredComponentLoader<{ label: string }>["load"],
): DeferredComponentLoader<{ label: string }> {
	return { get: () => null, load };
}
