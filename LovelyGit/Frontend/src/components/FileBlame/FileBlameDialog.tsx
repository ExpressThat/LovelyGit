import { FileSearch, LoaderCircle } from "lucide-react";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogHeader,
	DialogTitle,
} from "@/components/ui/dialog";
import { shortHash } from "../CommitGraph/utils/format";
import { FileBlameContent } from "./FileBlameContent";
import { useFileBlame } from "./useFileBlame";

export type FileBlameTarget = { path: string; startCommitHash: string | null };
const SKELETON_WIDTHS = [72, 79, 86, 93, 74, 81, 88, 95, 76, 83, 90, 97];

export function FileBlameDialog({
	onOpenChange,
	onSelectCommit,
	repositoryId,
	target,
}: {
	onOpenChange: (open: boolean) => void;
	onSelectCommit: (commitHash: string) => void;
	repositoryId: string | null;
	target: FileBlameTarget | null;
}) {
	const [deep, setDeep] = useState(false);
	const open = Boolean(target && repositoryId);
	const { error, isLoading, response } = useFileBlame(
		repositoryId,
		target?.path ?? null,
		target?.startCommitHash ?? null,
		open,
		deep,
	);
	useEffect(() => {
		if (!open) setDeep(false);
	}, [open]);
	const selectCommit = (hash: string) => {
		onOpenChange(false);
		onSelectCommit(hash);
	};

	return (
		<Dialog open={open} onOpenChange={onOpenChange}>
			<DialogContent className="flex h-[min(86vh,900px)] flex-col gap-0 overflow-hidden p-0 sm:max-w-[min(94vw,1400px)]">
				<DialogHeader className="shrink-0 gap-1 border-b px-4 py-3 pr-12">
					<DialogTitle className="flex items-center gap-2">
						<FileSearch aria-hidden="true" className="size-5 text-primary" />
						Line blame
						{isLoading ? (
							<LoaderCircle
								aria-label="Loading file blame"
								className="size-4 animate-spin text-primary"
							/>
						) : null}
					</DialogTitle>
					<DialogDescription className="flex min-w-0 items-center gap-2">
						<span className="truncate font-mono" title={target?.path}>
							{target?.path}
						</span>
						{response ? (
							<span className="shrink-0">
								at {shortHash(response.startCommitHash)}
							</span>
						) : null}
					</DialogDescription>
				</DialogHeader>
				{error ? (
					<div className="m-4 rounded-md border border-destructive/40 bg-destructive/10 p-3 text-destructive">
						{error}
					</div>
				) : null}
				{!error && isLoading && !response ? <BlameSkeleton /> : null}
				{response ? (
					<FileBlameContent onSelectCommit={selectCommit} response={response} />
				) : null}
				<footer className="flex shrink-0 items-center justify-between gap-3 border-t bg-muted/30 px-4 py-2 text-[11px] text-muted-foreground">
					<span>
						{response
							? `${response.resolvedLineCount.toLocaleString()} of ${response.lineCount.toLocaleString()} lines · ${response.scannedCommitCount.toLocaleString()} commits scanned`
							: "Native line attribution"}
					</span>
					{response?.isPartial && !deep ? (
						<Button onClick={() => setDeep(true)} size="sm" variant="outline">
							Trace older lines
						</Button>
					) : (
						<span>Click attribution to open commit</span>
					)}
				</footer>
			</DialogContent>
		</Dialog>
	);
}

function BlameSkeleton() {
	return (
		<div className="flex min-h-0 flex-1 flex-col gap-2 p-4">
			{SKELETON_WIDTHS.map((width) => (
				<div
					className="h-5 animate-pulse rounded bg-muted"
					key={width}
					style={{ width: `${width}%` }}
				/>
			))}
		</div>
	);
}
