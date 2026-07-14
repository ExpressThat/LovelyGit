import type { CommitChangedFile } from "@/generated/types";

export function CommitChangedFileStats({
	file,
	visible,
}: {
	file: CommitChangedFile;
	visible: boolean;
}) {
	return visible ? (
		<div className="shrink-0 font-mono text-xs">
			<span className="text-emerald-600 dark:text-emerald-400">
				+{file.additions}
			</span>{" "}
			<span className="text-red-600 dark:text-red-400">-{file.deletions}</span>
		</div>
	) : null;
}
