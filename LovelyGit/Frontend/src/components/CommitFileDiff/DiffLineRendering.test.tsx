// @vitest-environment jsdom
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { CodeCell } from "./DiffLineRendering";
import { estimateCodeWidth } from "./DiffLineViewport";

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

	it("bounds unwrapped text while preserving the complete horizontal range", () => {
		const text = "x".repeat(20_000);
		const { container } = render(
			<CodeCell scrollLeft={0} text={text} variant="plain" wrapLines={false} />,
		);

		expect(container.textContent).toHaveLength(4_096);
		expect(estimateCodeWidth([text])).toBeGreaterThan(140_000);
	});

	it("keeps far-away syntax and change spans when horizontally scrolling", () => {
		const tokenStart = 8_000;
		const text = `${"x".repeat(tokenStart)}TOKEN${"z".repeat(8_000)}`;
		render(
			<CodeCell
				changeSpans={[{ changeType: "Inserted", length: 5, start: tokenStart }]}
				scrollLeft={tokenStart * 7.25}
				spans={[{ length: 5, scope: "Keyword", start: tokenStart }]}
				text={text}
				variant="inserted"
				wrapLines={false}
			/>,
		);

		expect(screen.getByText("TOKEN")).toHaveClass(
			"text-blue-600",
			"bg-emerald-500/25",
		);
	});

	it("keeps the complete line when wrapping is enabled", () => {
		const text = "x".repeat(10_000);
		const { container } = render(
			<CodeCell scrollLeft={0} text={text} variant="plain" wrapLines />,
		);

		expect(container.textContent).toHaveLength(text.length);
	});
});
