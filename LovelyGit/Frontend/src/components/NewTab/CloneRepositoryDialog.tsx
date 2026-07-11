import {
	Download,
	FolderOpen,
	LoaderCircle,
	X,
} from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogFooter,
	DialogHeader,
	DialogTitle,
	DialogTrigger,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { CloneProgress } from "./CloneProgress";
import { useCloneRepository } from "./useCloneRepository";

export function CloneRepositoryDialog() {
	const clone = useCloneRepository();
	const {
		canClone,
		cancelClone,
		chooseDestination,
		cloneRepository,
		directoryName,
		isBusy,
		open,
		parentPath,
		progress,
		recurseSubmodules,
		remoteUrl,
		setOpen,
		setParentPath,
		setRecurseSubmodules,
		setShallow,
		shallow,
		status,
		updateDirectoryName,
		updateRemoteUrl,
	} = clone;

	return (
		<Dialog
			onOpenChange={(nextOpen) => {
				if (!nextOpen && isBusy) {
					return;
				}
				setOpen(nextOpen);
			}}
			open={open}
		>
			<DialogTrigger render={<Button size="sm" />}>
				<Download aria-hidden="true" />
				Clone Repository
			</DialogTrigger>
			<DialogContent
				className="gap-0 overflow-hidden p-0 sm:max-w-lg"
				showCloseButton={!isBusy}
			>
				<DialogHeader className="border-b px-5 py-4">
					<DialogTitle>Clone repository</DialogTitle>
					<DialogDescription>
						Download a remote repository and open it in LovelyGit.
					</DialogDescription>
				</DialogHeader>
				<div className="grid gap-4 px-5 py-4">
					<label className="grid gap-2 text-sm" htmlFor="clone-remote-url">
						<span className="font-medium">Repository URL</span>
						<Input
							aria-label="Repository URL"
							autoFocus
							disabled={isBusy}
							id="clone-remote-url"
							onChange={(event) => updateRemoteUrl(event.currentTarget.value)}
							onInput={(event) => updateRemoteUrl(event.currentTarget.value)}
							placeholder="https://github.com/owner/repository.git"
							spellCheck={false}
							value={remoteUrl}
						/>
					</label>
					<div className="grid gap-2 text-sm">
						<label className="font-medium" htmlFor="clone-parent-path">
							Destination folder
						</label>
						<div className="flex gap-2">
							<Input
								aria-label="Destination folder"
								disabled={isBusy}
								id="clone-parent-path"
								onChange={(event) => setParentPath(event.currentTarget.value)}
								onInput={(event) => setParentPath(event.currentTarget.value)}
								placeholder="Choose or enter a folder"
								spellCheck={false}
								value={parentPath}
							/>
							<Button
								aria-label="Browse for destination folder"
								disabled={isBusy}
								onClick={() => void chooseDestination()}
								size="icon"
								title="Browse for destination folder"
								type="button"
								variant="outline"
							>
								<FolderOpen aria-hidden="true" />
							</Button>
						</div>
					</div>
					<label className="grid gap-2 text-sm" htmlFor="clone-directory-name">
						<span className="font-medium">Repository folder name</span>
						<Input
							aria-label="Repository folder name"
							disabled={isBusy}
							id="clone-directory-name"
							onChange={(event) =>
								updateDirectoryName(event.currentTarget.value)
							}
							placeholder="repository"
							spellCheck={false}
							value={directoryName}
						/>
					</label>
					<div className="flex flex-wrap gap-x-5 gap-y-2 rounded-lg border bg-card px-3 py-2.5">
						<label
							className="flex items-center gap-2 text-sm"
							htmlFor="clone-shallow"
						>
							<Checkbox
								aria-label="Shallow clone"
								checked={shallow}
								disabled={isBusy}
								id="clone-shallow"
								onCheckedChange={setShallow}
							/>
							Shallow clone
						</label>
						<label
							className="flex items-center gap-2 text-sm"
							htmlFor="clone-submodules"
						>
							<Checkbox
								aria-label="Initialize submodules"
								checked={recurseSubmodules}
								disabled={isBusy}
								id="clone-submodules"
								onCheckedChange={setRecurseSubmodules}
							/>
							Initialize submodules
						</label>
					</div>
					{isBusy && progress ? <CloneProgress progress={progress} /> : null}
				</div>
				<DialogFooter className="mx-0 mb-0 px-5 pb-4">
					{isBusy ? (
						<Button
							disabled={status === "canceling"}
							onClick={() => void cancelClone()}
							type="button"
							variant="outline"
						>
							{status === "canceling" ? (
								<LoaderCircle aria-hidden="true" className="animate-spin" />
							) : (
								<X aria-hidden="true" />
							)}
							{status === "canceling" ? "Canceling" : "Cancel clone"}
						</Button>
					) : (
						<Button
							disabled={!canClone}
							onClick={() => void cloneRepository()}
							type="button"
						>
							<Download aria-hidden="true" />
							Clone and open
						</Button>
					)}
				</DialogFooter>
			</DialogContent>
		</Dialog>
	);
}
