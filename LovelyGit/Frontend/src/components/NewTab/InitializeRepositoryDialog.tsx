import { FolderGit2, FolderOpen, GitBranch, LoaderCircle } from "lucide-react";
import { motion, useReducedMotion } from "motion/react";
import { Button } from "@/components/ui/button";
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
import { useInitializeRepository } from "./useInitializeRepository";

export function InitializeRepositoryDialog() {
	const repository = useInitializeRepository();
	const reduceMotion = useReducedMotion();

	return (
		<Dialog
			onOpenChange={(open) => !repository.isBusy && repository.setOpen(open)}
			open={repository.open}
		>
			<DialogTrigger render={<Button size="sm" variant="outline" />}>
				<FolderGit2 aria-hidden="true" />
				New Repository
			</DialogTrigger>
			<DialogContent
				className="gap-0 overflow-hidden p-0 sm:max-w-lg"
				showCloseButton={!repository.isBusy}
			>
				<DialogHeader className="border-b px-5 py-4">
					<DialogTitle>Create a repository</DialogTitle>
					<DialogDescription>
						Initialize a Git repository with its first commit and open it in
						LovelyGit.
					</DialogDescription>
				</DialogHeader>
				<motion.form
					animate={{ opacity: 1, y: 0 }}
					initial={{ opacity: 0, y: reduceMotion ? 0 : 5 }}
					onSubmit={(event) => {
						event.preventDefault();
						void repository.initializeRepository();
					}}
					transition={{ duration: reduceMotion ? 0 : 0.18 }}
				>
					<div className="grid gap-4 px-5 py-4">
						<label className="grid gap-2 text-sm" htmlFor="init-name">
							<span className="font-medium">Repository name</span>
							<Input
								autoFocus
								disabled={repository.isBusy}
								id="init-name"
								onChange={(event) =>
									repository.setDirectoryName(event.currentTarget.value)
								}
								onInput={(event) =>
									repository.setDirectoryName(event.currentTarget.value)
								}
								placeholder="my-project"
								spellCheck={false}
								value={repository.directoryName}
							/>
						</label>
						<div className="grid gap-2 text-sm">
							<label className="font-medium" htmlFor="init-parent-path">
								Location
							</label>
							<div className="flex gap-2">
								<Input
									disabled={repository.isBusy}
									id="init-parent-path"
									onChange={(event) =>
										repository.setParentPath(event.currentTarget.value)
									}
									onInput={(event) =>
										repository.setParentPath(event.currentTarget.value)
									}
									placeholder="Choose a parent folder"
									spellCheck={false}
									value={repository.parentPath}
								/>
								<Button
									aria-label="Browse for repository location"
									disabled={repository.isBusy}
									onClick={() => void repository.chooseDestination()}
									size="icon"
									title="Browse for repository location"
									type="button"
									variant="outline"
								>
									<FolderOpen aria-hidden="true" />
								</Button>
							</div>
						</div>
						<label className="grid gap-2 text-sm" htmlFor="init-branch">
							<span className="flex items-center gap-1.5 font-medium">
								<GitBranch aria-hidden="true" className="size-3.5" />
								Initial branch
							</span>
							<Input
								disabled={repository.isBusy}
								id="init-branch"
								onChange={(event) =>
									repository.setInitialBranchName(event.currentTarget.value)
								}
								onInput={(event) =>
									repository.setInitialBranchName(event.currentTarget.value)
								}
								spellCheck={false}
								value={repository.initialBranchName}
							/>
						</label>
					</div>
					<DialogFooter className="mx-0 mb-0 px-5 pb-4">
						<Button disabled={!repository.canInitialize} type="submit">
							{repository.isBusy ? (
								<LoaderCircle aria-hidden="true" className="animate-spin" />
							) : (
								<FolderGit2 aria-hidden="true" />
							)}
							{repository.isBusy ? "Creating" : "Create and open"}
						</Button>
					</DialogFooter>
				</motion.form>
			</DialogContent>
		</Dialog>
	);
}
