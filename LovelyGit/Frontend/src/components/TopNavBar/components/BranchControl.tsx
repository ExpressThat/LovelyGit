import { useMemo, useState } from "react";
import { toast } from "sonner";
import {
	ChevronDown,
	GitBranch,
	GitMerge,
	ListRestart,
	Plus,
} from "@/components/icons/lovelyIcons";
import {
	DropdownMenu,
	DropdownMenuContent,
	DropdownMenuGroup,
	DropdownMenuItem,
	DropdownMenuSeparator,
	DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { NativeMessageType } from "@/lib/nativeMessaging";
import type { BranchIntegrationMode } from "./BranchIntegrationDialog";
import { BranchMenuContent } from "./BranchMenuContent";
import {
	LazyBranchIntegrationDialog,
	LazyCreateBranchDialog,
} from "./LazyRepositoryDialogs";
import { useLocalBranches } from "./useLocalBranches";

type BranchControlProps = {
	currentBranchName: string | null;
	onBranchChanged: (branchName: string) => void;
	onOpenWorkingChanges: () => void;
	onRepositoryChanged: () => void;
	repositoryId: string | null;
};

export function BranchControl({
	currentBranchName,
	onBranchChanged,
	onOpenWorkingChanges,
	onRepositoryChanged,
	repositoryId,
}: BranchControlProps) {
	const [busyBranch, setBusyBranch] = useState<string | null>(null);
	const [activeIndex, setActiveIndex] = useState(0);
	const [createOpen, setCreateOpen] = useState(false);
	const [integrationMode, setIntegrationMode] =
		useState<BranchIntegrationMode | null>(null);
	const [menuOpen, setMenuOpen] = useState(false);
	const [query, setQuery] = useState("");
	const { branches, error, isLoading } = useLocalBranches(
		menuOpen,
		repositoryId,
	);

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
	const branchNames = useMemo(
		() => branches.map((branch) => branch.name),
		[branches],
	);
	const canIntegrate =
		currentBranchName !== null &&
		branches.some((branch) => branch.name !== currentBranchName);

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
			<DropdownMenu
				open={menuOpen}
				onOpenChange={(open) => {
					setMenuOpen(open);
					if (open) setActiveIndex(0);
				}}
			>
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
					<BranchMenuContent
						activeIndex={activeIndex}
						branches={filteredBranches}
						busy={busyBranch !== null}
						currentBranchName={currentBranchName}
						error={error}
						isLoading={isLoading}
						onActiveIndexChange={setActiveIndex}
						onCheckout={(branchName) => void checkoutBranch(branchName)}
						onQueryChange={(value) => {
							setQuery(value);
							setActiveIndex(0);
						}}
						query={query}
					/>
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
						<DropdownMenuItem
							className="min-h-8 px-2"
							disabled={!canIntegrate}
							onClick={() => {
								setMenuOpen(false);
								setIntegrationMode("merge");
							}}
						>
							<GitMerge aria-hidden="true" className="size-4" />
							Merge into current branch…
						</DropdownMenuItem>
						<DropdownMenuItem
							className="min-h-8 px-2"
							disabled={!canIntegrate}
							onClick={() => {
								setMenuOpen(false);
								setIntegrationMode("rebase");
							}}
						>
							<ListRestart aria-hidden="true" className="size-4" />
							Rebase current branch…
						</DropdownMenuItem>
					</DropdownMenuGroup>
				</DropdownMenuContent>
			</DropdownMenu>
			<LazyCreateBranchDialog
				currentBranchName={currentBranchName}
				existingBranchNames={branchNames}
				onBranchChanged={onBranchChanged}
				onOpenChange={setCreateOpen}
				onRepositoryChanged={onRepositoryChanged}
				open={createOpen}
				repositoryId={repositoryId}
			/>
			<LazyBranchIntegrationDialog
				branches={branches}
				currentBranchName={currentBranchName}
				mode={integrationMode}
				onOpenChange={setIntegrationMode}
				onOpenWorkingChanges={onOpenWorkingChanges}
				onRepositoryChanged={onRepositoryChanged}
				repositoryId={repositoryId}
			/>
		</>
	);
}
