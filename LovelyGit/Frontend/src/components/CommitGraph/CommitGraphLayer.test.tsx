// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import { CommitGraphLayer } from "./CommitGraphLayer";

let nextInstance = 0;
vi.mock("./CommitGraphView", () => ({
	CommitGraphView: () => {
		const [instance] = useState(() => ++nextInstance);
		return <div data-testid="graph-instance">{instance}</div>;
	},
}));

describe("CommitGraphLayer", () => {
	it("remounts the graph at the top when switching repository tabs", () => {
		const { rerender } = render(<Layer repositoryId="repo-a" />);
		expect(screen.getByTestId("graph-instance")).toHaveTextContent("1");

		rerender(<Layer repositoryId="repo-b" />);
		expect(screen.getByTestId("graph-instance")).toHaveTextContent("2");
	});
});

function Layer({ repositoryId }: { repositoryId: string }) {
	return (
		<CommitGraphLayer
			isDimmed={false}
			onOpenWorkingChanges={() => {}}
			onRepositoryChanged={() => {}}
			onSelectCommit={() => {}}
			refreshToken={0}
			repositoryId={repositoryId}
			selectedCommitHash={null}
		/>
	);
}
