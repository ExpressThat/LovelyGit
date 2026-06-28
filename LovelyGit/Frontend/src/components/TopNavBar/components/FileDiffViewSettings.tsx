import {
	Columns2,
	FileText,
	ListCollapse,
	Minus,
	Plus,
	Rows3,
	WrapText,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import type {
	CommitDiffLineDisplayMode,
	CommitDiffViewMode,
} from "@/generated/types";
import { setSetting, useSetting } from "@/lib/settings/settingsStore";
import {
	SegmentedButton,
	SegmentedControl,
	SettingGroup,
} from "./SettingsControls";

export function FileDiffViewSettings() {
	const viewMode = useSetting("CommitDiffViewMode");
	const lineDisplayMode = useSetting("CommitDiffLineDisplayMode");
	const contextLines = useSetting("CommitDiffContextLines");
	const wrapLines = useSetting("CommitDiffWrapLines");
	return (
		<div className="space-y-5">
			<SettingGroup
				description="Choose how file changes are arranged."
				title="Layout"
			>
				<SegmentedControl>
					<SegmentedButton
						icon={<Columns2 aria-hidden="true" className="size-4" />}
						isActive={viewMode === "SideBySide"}
						label="Side by side"
						onClick={() =>
							void setSetting(
								"CommitDiffViewMode",
								"SideBySide" satisfies CommitDiffViewMode,
							)
						}
					/>
					<SegmentedButton
						icon={<Rows3 aria-hidden="true" className="size-4" />}
						isActive={viewMode === "Combined"}
						label="Combined"
						onClick={() =>
							void setSetting(
								"CommitDiffViewMode",
								"Combined" satisfies CommitDiffViewMode,
							)
						}
					/>
				</SegmentedControl>
			</SettingGroup>
			<SettingGroup
				description="Switch between changed hunks and the whole file."
				title="Line Display"
			>
				<SegmentedControl>
					<SegmentedButton
						icon={<ListCollapse aria-hidden="true" className="size-4" />}
						isActive={lineDisplayMode === "Changes"}
						label="Changes"
						onClick={() =>
							void setSetting(
								"CommitDiffLineDisplayMode",
								"Changes" satisfies CommitDiffLineDisplayMode,
							)
						}
					/>
					<SegmentedButton
						icon={<FileText aria-hidden="true" className="size-4" />}
						isActive={lineDisplayMode === "FullFile"}
						label="Full file"
						onClick={() =>
							void setSetting(
								"CommitDiffLineDisplayMode",
								"FullFile" satisfies CommitDiffLineDisplayMode,
							)
						}
					/>
				</SegmentedControl>
			</SettingGroup>
			<SettingGroup
				description="Set how many unchanged lines surround each change."
				title="Context Lines"
			>
				<div className="inline-flex h-9 overflow-hidden rounded-lg border bg-background">
					<Button
						aria-label="Decrease context lines"
						className="h-full rounded-none border-0"
						disabled={contextLines <= 0}
						onClick={() => updateContextLines(contextLines - 1)}
						size="icon-sm"
						variant="ghost"
					>
						<Minus aria-hidden="true" className="size-4" />
					</Button>
					<div className="flex min-w-12 items-center justify-center border-x px-3 font-mono text-sm">
						{contextLines}
					</div>
					<Button
						aria-label="Increase context lines"
						className="h-full rounded-none border-0"
						disabled={contextLines >= 99}
						onClick={() => updateContextLines(contextLines + 1)}
						size="icon-sm"
						variant="ghost"
					>
						<Plus aria-hidden="true" className="size-4" />
					</Button>
				</div>
			</SettingGroup>
			<SettingGroup
				description="Wrap long diff lines inside the viewport."
				title="Line Wrapping"
			>
				<Button
					onClick={() => void setSetting("CommitDiffWrapLines", !wrapLines)}
					variant={wrapLines ? "secondary" : "outline"}
				>
					<WrapText aria-hidden="true" className="size-4" />
					{wrapLines ? "Wrapping on" : "Wrapping off"}
				</Button>
			</SettingGroup>
		</div>
	);
}

function updateContextLines(value: number) {
	const nextValue = Math.max(0, Math.min(99, Math.trunc(value)));
	void setSetting("CommitDiffContextLines", nextValue);
}
