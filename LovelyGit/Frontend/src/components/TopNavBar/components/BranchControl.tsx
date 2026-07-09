import {
	Check,
	ChevronDown,
	GitBranch,
	LoaderCircle,
	Plus,
	Search,
} from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogFooter,
	DialogHeader,
	DialogTitle,
} from "@/components/ui/dialog";
import {
	DropdownMenu,
	DropdownMenuContent,
	DropdownMenuGroup,
	DropdownMenuItem,
	DropdownMenuLabel,
	DropdownMenuSeparator,
	DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import type { RepositoryRefItem } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { NativeMessageType } from "@/lib/nativeMessaging";

type BranchControlProps = {
	currentBranchName: string | null;
	onBranchChanged: (branchName: string) => void;
	repositoryId: string | null;
};

export function BranchControl({
	currentBranchName,
	onBranchChanged,
	repositoryId,
}: BranchControlProps) {
	const [branches, setBranches] = useState<RepositoryRefItem[]>([]);
	const [busyBranch, setBusyBranch] = useState<string | null>(null);
	const [createOpen, setCreateOpen] = useState(false);
	const [error, setError] = useState<string | null>(null);
	const [isLoading, setIsLoading] = useState(false);
	const [menuOpen, setMenuOpen] = useState(false);
	const [query, setQuery] = useState("");

	useEffect(() => {
		if (!menuOpen || !repositoryId) {
			return;
		}

		let isActive = true;
		setIsLoading(true);
		setError(null);
		sendRequestWithResponse({
			arguments: { knownRepositoryId: repositoryId },
			commandType: NativeMessageType.GetRepositoryRefs,
		})
			.then((response) => {
				if (!isActive) {
					return;
				}
				setBranches(response.refs.filter((ref) => ref.kind === "Local"));
			})
			.catch((loadError) => {
				if (isActive) {
					setError(
						loadError instanceof Error
							? loadError.message
							: "Failed to load branches.",
					);
				}
			})
			.finally(() => {
				if (isActive) {
					setIsLoading(false);
				}
			});

		return () => {
			isActive = false;
		};
	}, [menuOpen, repositoryId]);

	const filteredBranches = useMemo(() => {
		const normalizedQuery = query.trim().toLocaleLowerCase();
		return branches
			.filter(
				(branch) =>
					normalizedQuery.length === 0 ||
					branch.name.toLocaleLowerCase().includes(normalizedQuery),
			)
			.sort((left, right) => {
				if (left.name === currentBranchName) {
					return -1;
				}
				if (right.name === currentBranchName) {
					return 1;
				}
				return left.name.localeCompare(right.name);
			});
	}, [branches, currentBranchName, query]);

	const checkoutBranch = async (branchName: string) => {
		if (!repositoryId || busyBranch || branchName === currentBranchName) {
			setMenuOpen(false);
			return;
		}

		setBusyBranch(branchName);
		const toastId = toast.loading(`Switching to ${branchName}`);
		try {
			await sendRequestWithResponse(
				{
					arguments: { branchName, repositoryId },
					commandType: NativeMessageType.CheckoutBranch,
				},
				{ timeoutMs: gitMutationTimeoutMs },
			);
			setMenuOpen(false);
			onBranchChanged(branchName);
			toast.success(`Switched to ${branchName}`, { id: toastId });
		} catch (checkoutError) {
			toast.error(
				checkoutError instanceof Error
					? checkoutError.message
					: `Could not switch to ${branchName}.`,
				{ id: toastId },
			);
		} finally {
			setBusyBranch(null);
		}
	};

	return (
		<>
			<DropdownMenu open={menuOpen} onOpenChange={setMenuOpen}>
				<DropdownMenuTrigger
					aria-label="Switch branch"
					className="flex h-9 min-w-0 max-w-full items-center gap-2 rounded-md px-2 text-sm text-muted-foreground outline-none hover:bg-accent hover:text-accent-foreground focus-visible:ring-2 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50"
					disabled={!repositoryId || busyBranch !== null}
					title={currentBranchName ?? "Detached HEAD"}
				>
					<GitBranch aria-hidden="true" className="size-5 shrink-0" />
					<span className="truncate">
						{currentBranchName ??
							(repositoryId ? "Detached HEAD" : "No repository")}
					</span>
					<ChevronDown aria-hidden="true" className="size-4 shrink-0" />
				</DropdownMenuTrigger>
				<DropdownMenuContent align="start" className="w-72 p-0">
					<DropdownMenuGroup>
						<DropdownMenuLabel className="px-3 pt-3 pb-2 text-sm normal-case">
							Switch branch
						</DropdownMenuLabel>
						<div className="px-2 pb-2">
							<div className="relative">
								<Search
									aria-hidden="true"
									className="pointer-events-none absolute left-2 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
								/>
								<Input
									aria-label="Filter branches"
									className="h-8 pl-8 text-sm"
									onChange={(event) => setQuery(event.currentTarget.value)}
									onKeyDown={(event) => event.stopPropagation()}
									placeholder="Filter branches"
									value={query}
								/>
							</div>
						</div>
						<DropdownMenuSeparator className="my-0" />
						<div className="custom-scrollbar max-h-64 overflow-y-auto p-1">
							{isLoading ? (
								<p className="flex items-center gap-2 px-2 py-3 text-sm text-muted-foreground">
									<LoaderCircle
										aria-hidden="true"
										className="size-4 animate-spin"
									/>
									Loading branches…
								</p>
							) : null}
							{!isLoading && error ? (
								<p className="px-2 py-3 text-sm text-destructive">{error}</p>
							) : null}
							{!isLoading && !error && filteredBranches.length === 0 ? (
								<p className="px-2 py-3 text-sm text-muted-foreground">
									No branches match this filter.
								</p>
							) : null}
							{!isLoading && !error
								? filteredBranches.map((branch) => (
										<DropdownMenuItem
											className="min-h-8 px-2"
											disabled={busyBranch !== null}
											key={branch.name}
											onClick={() => void checkoutBranch(branch.name)}
										>
											<GitBranch aria-hidden="true" className="size-4" />
											<span className="min-w-0 flex-1 truncate">
												{branch.name}
											</span>
											{branch.name === currentBranchName ? (
												<Check
													aria-label="Current branch"
													className="size-4 text-primary"
												/>
											) : null}
										</DropdownMenuItem>
									))
								: null}
						</div>
					</DropdownMenuGroup>
					<DropdownMenuSeparator className="my-0" />
					<DropdownMenuGroup className="p-1">
						<DropdownMenuItem
							className="min-h-8 px-2"
							onClick={() => {
								setMenuOpen(false);
								setCreateOpen(true);
							}}
						>
							<Plus aria-hidden="true" className="size-4" />
							Create new branch…
						</DropdownMenuItem>
					</DropdownMenuGroup>
				</DropdownMenuContent>
			</DropdownMenu>
			<CreateBranchDialog
				currentBranchName={currentBranchName}
				onBranchChanged={onBranchChanged}
				onOpenChange={setCreateOpen}
				open={createOpen}
				repositoryId={repositoryId}
			/>
		</>
	);
}

function CreateBranchDialog({
	currentBranchName,
	onBranchChanged,
	onOpenChange,
	open,
	repositoryId,
}: BranchControlProps & {
	onOpenChange: (open: boolean) => void;
	open: boolean;
}) {
	const [branchName, setBranchName] = useState("");
	const [isCreating, setIsCreating] = useState(false);

	useEffect(() => {
		if (!open) {
			setBranchName("");
		}
	}, [open]);

	const createBranch = async () => {
		const normalizedName = branchName.trim();
		if (!repositoryId || normalizedName.length === 0 || isCreating) {
			return;
		}

		setIsCreating(true);
		const toastId = toast.loading(`Creating ${normalizedName}`);
		try {
			await sendRequestWithResponse(
				{
					arguments: {
						branchName: normalizedName,
						repositoryId,
						startPoint: currentBranchName ?? "HEAD",
					},
					commandType: NativeMessageType.CreateBranch,
				},
				{ timeoutMs: gitMutationTimeoutMs },
			);
			onOpenChange(false);
			onBranchChanged(normalizedName);
			toast.success(`Created and switched to ${normalizedName}`, {
				id: toastId,
			});
		} catch (createError) {
			toast.error(
				createError instanceof Error
					? createError.message
					: `Could not create ${normalizedName}.`,
				{ id: toastId },
			);
		} finally {
			setIsCreating(false);
		}
	};

	return (
		<Dialog open={open} onOpenChange={onOpenChange}>
			<DialogContent>
				<form
					onSubmit={(event) => {
						event.preventDefault();
						void createBranch();
					}}
				>
					<DialogHeader>
						<DialogTitle>Create branch</DialogTitle>
						<DialogDescription>
							Create from {currentBranchName ?? "HEAD"} and switch to it.
						</DialogDescription>
					</DialogHeader>
					<div className="py-4">
						<label className="grid gap-2 text-sm" htmlFor="new-branch-name">
							<span className="font-medium">Branch name</span>
							<Input
								autoFocus
								id="new-branch-name"
								onChange={(event) => setBranchName(event.currentTarget.value)}
								onInput={(event) => setBranchName(event.currentTarget.value)}
								placeholder="feature/my-change"
								value={branchName}
							/>
						</label>
					</div>
					<DialogFooter className="mx-0 mb-0 px-0 pb-0">
						<Button
							disabled={branchName.trim().length === 0 || isCreating}
							type="submit"
						>
							{isCreating ? (
								<LoaderCircle aria-hidden="true" className="animate-spin" />
							) : (
								<GitBranch aria-hidden="true" />
							)}
							Create and switch
						</Button>
					</DialogFooter>
				</form>
			</DialogContent>
		</Dialog>
	);
}
