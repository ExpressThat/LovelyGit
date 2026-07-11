// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { CommitSignatureBadge } from "./CommitSignatureBadge";

describe("CommitSignatureBadge", () => {
	it("labels signature presence without claiming verification", () => {
		render(<CommitSignatureBadge kind="Ssh" />);

		const badge = screen.getByLabelText("Signed commit (SSH)");
		expect(badge).toHaveTextContent("SSH");
		expect(badge).toHaveAttribute(
			"title",
			"Signed commit (SSH). Signature presence has not been cryptographically verified.",
		);
	});

	it("renders a compact accessible badge for graph rows", () => {
		render(<CommitSignatureBadge compact kind="OpenPgp" />);

		expect(screen.getByLabelText("Signed commit (OpenPGP)")).toBeVisible();
		expect(screen.queryByText("OpenPGP")).not.toBeInTheDocument();
	});

	it("does not render for unsigned commits", () => {
		const { container } = render(<CommitSignatureBadge kind="None" />);
		expect(container).toBeEmptyDOMElement();
	});
});
