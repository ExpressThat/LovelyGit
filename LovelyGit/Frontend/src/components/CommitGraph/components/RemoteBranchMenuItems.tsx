import { GitBranch } from "lucide-react";
import { ContextMenuItem } from "@/components/ui/context-menu";

export function RemoteBranchMenuItems({
	canCheckoutRemote,
	onCheckout,
}: {
	canCheckoutRemote: boolean;
	onCheckout: () => void;
}) {
	if (!canCheckoutRemote) {
		return null;
	}

	return (
		<ContextMenuItem onClick={onCheckout}>
			<GitBranch />
			Checkout as local branch
		</ContextMenuItem>
	);
}
