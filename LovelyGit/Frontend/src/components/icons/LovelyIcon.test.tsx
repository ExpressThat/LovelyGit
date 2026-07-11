// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { getLovelyIconUrl, lovelyIconNames } from "./LovelyIcon";
import { LoaderCircle, Search } from "./lovelyIcons";

describe("LovelyIcon", () => {
	it("resolves every approved icon asset", () => {
		expect(lovelyIconNames).toHaveLength(100);
		expect(new Set(lovelyIconNames)).toHaveLength(100);
		const urls = lovelyIconNames.map(getLovelyIconUrl);
		expect(new Set(urls)).toHaveLength(100);
		expect(urls.every((url) => url.includes("#lovely-"))).toBe(true);
		expect(new Set(urls.map((url) => url.split("#")[0]))).toHaveLength(1);
	});

	it("uses the approved sprite as a current-color SVG", () => {
		render(
			<Search
				aria-label="Search commits"
				className="size-4 text-primary"
				size={18}
			/>,
		);

		const icon = screen.getByRole("img", { name: "Search commits" });
		expect(icon).toHaveClass("text-primary");
		expect(icon.tagName).toBe("svg");
		expect(icon.querySelector("use")).toHaveAttribute(
			"href",
			getLovelyIconUrl("search"),
		);
		expect(icon).toHaveAttribute("stroke", "currentColor");
		expect(icon.style.height).toBe("18px");
		expect(icon.style.width).toBe("18px");
	});

	it("keeps decorative and animated icons compatible with existing controls", () => {
		const { container } = render(<LoaderCircle className="animate-spin" />);
		const icon = container.querySelector("svg");

		expect(icon).toHaveAttribute("aria-hidden", "true");
		expect(icon).toHaveClass("animate-spin");
		expect(icon).not.toHaveAttribute("role");
	});
});
