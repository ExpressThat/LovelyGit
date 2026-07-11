import {
	Columns2,
	FileText,
	ListCollapse,
	Minus,
	Plus,
	Rows3,
	WrapText,
} from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import { Switch } from "@/components/ui/switch";
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
	const ignoreWhitespace = useSetting("CommitDiffIgnoreWhitespace");
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
			<SettingGroup
				description="Hide whitespace-only edits when comparing file content."
				title="Whitespace"
			>
				<div className="flex max-w-xl items-center justify-between gap-4 rounded-md border bg-background px-3 py-2">
					<span className="min-w-0">
						<span
							className="block text-sm font-medium"
							id="commit-diff-ignore-whitespace-label"
						>
							Ignore whitespace-only changes
						</span>
						<span
							className="block text-xs text-muted-foreground"
							id="commit-diff-ignore-whitespace-description"
						>
							Applies to commit and working-tree file diffs.
						</span>
					</span>
					<Switch
						aria-describedby="commit-diff-ignore-whitespace-description"
						aria-label="Ignore whitespace-only changes"
						checked={ignoreWhitespace}
						id="commit-diff-ignore-whitespace"
						onCheckedChange={(checked) =>
							void setSetting("CommitDiffIgnoreWhitespace", checked)
						}
					/>
				</div>
			</SettingGroup>
		</div>
	);
}

function updateContextLines(value: number) {
	const nextValue = Math.max(0, Math.min(99, Math.trunc(value)));
	void setSetting("CommitDiffContextLines", nextValue);
}
