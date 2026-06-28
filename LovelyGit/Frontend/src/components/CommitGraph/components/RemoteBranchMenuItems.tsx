import { Copy, GitBranch } from "lucide-react";
import { ContextMenuItem } from "@/components/ui/context-menu";
import { copyToClipboard } from "../utils/clipboard";

export function RemoteBranchMenuItems({
	canCheckoutRemote,
	remoteBranchName,
	onCheckout,
}: {
	canCheckoutRemote: boolean;
	remoteBranchName: string;
	onCheckout: () => void;
}) {
	return (
		<>
			{canCheckoutRemote ? (
				<ContextMenuItem onClick={onCheckout}>
					<GitBranch />
					Checkout as local branch
				</ContextMenuItem>
			) : null}
			<ContextMenuItem
				onClick={() =>
					void copyToClipboard(remoteBranchName, "Remote branch name")
				}
			>
				<Copy />
				Copy remote branch name
			</ContextMenuItem>
		</>
	);
}
