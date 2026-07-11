import { Archive, Copy, Download } from "@/components/icons/lovelyIcons";
import { ContextMenuItem } from "@/components/ui/context-menu";
import type { CommitGraphRow } from "@/generated/types";

export function CommitExportMenuItems({
	archiveBusy,
	copyPatchBusy,
	onCopyPatch,
	onSaveArchive,
	onSavePatch,
	row,
	savePatchBusy,
}: {
	archiveBusy: boolean;
	copyPatchBusy: boolean;
	onCopyPatch: (row: CommitGraphRow) => void;
	onSaveArchive: (row: CommitGraphRow) => void;
	onSavePatch: (row: CommitGraphRow) => void;
	row: CommitGraphRow;
	savePatchBusy: boolean;
}) {
	const busy = archiveBusy || copyPatchBusy || savePatchBusy;
	return (
		<>
			<ContextMenuItem disabled={busy} onClick={() => onCopyPatch(row)}>
				<Copy aria-hidden="true" />
				{copyPatchBusy ? "Creating commit patch…" : "Copy commit as patch"}
			</ContextMenuItem>
			<ContextMenuItem disabled={busy} onClick={() => onSavePatch(row)}>
				<Download aria-hidden="true" />
				{savePatchBusy ? "Saving commit patch…" : "Save commit as patch…"}
			</ContextMenuItem>
			<ContextMenuItem disabled={busy} onClick={() => onSaveArchive(row)}>
				<Archive aria-hidden="true" />
				{archiveBusy ? "Exporting commit archive…" : "Export commit archive…"}
			</ContextMenuItem>
		</>
	);
}
