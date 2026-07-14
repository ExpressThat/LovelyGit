// @vitest-environment jsdom
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { CodeCell } from "./DiffLineRendering";

describe("CodeCell", () => {
	it("scrolls wide content without creating a transformed WebView layer", () => {
		render(
			<CodeCell
				scrollLeft={40}
				text="wide line"
				variant="plain"
				wrapLines={false}
			/>,
		);

		const text = screen.getByText("line");
		expect(text.textContent).toBe("line");
		expect(text.parentElement?.style.transform).toBe("");
		expect(text.parentElement?.style.marginLeft).toBe("");
	});

	it("preserves syntax and change styling after horizontally slicing text", () => {
		render(
			<CodeCell
				changeSpans={[{ changeType: "Inserted", length: 5, start: 6 }]}
				scrollLeft={44}
				spans={[{ length: 5, scope: "Keyword", start: 6 }]}
				text="prefixTOKEN"
				variant="inserted"
				wrapLines={false}
			/>,
		);

		const token = screen.getByText("TOKEN");
		expect(token).toHaveClass("text-blue-600", "bg-emerald-500/25");
		expect(screen.queryByText("prefix")).not.toBeInTheDocument();
	});
});
