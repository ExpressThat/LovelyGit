import { Download, GitPullRequestArrow } from "lucide-react";
import type { RemotePrimaryAction } from "@/generated/types";
import { setSetting, useSetting } from "@/lib/settings/settingsStore";
import {
	SegmentedButton,
	SegmentedControl,
	SettingGroup,
} from "./SettingsControls";

const primaryActions: Array<{
	icon: typeof Download;
	label: string;
	value: RemotePrimaryAction;
}> = [
	{ icon: Download, label: "Fetch", value: "Fetch" },
	{ icon: GitPullRequestArrow, label: "Pull", value: "Pull" },
	{
		icon: GitPullRequestArrow,
		label: "Pull ff-only",
		value: "PullFastForwardOnly",
	},
	{ icon: GitPullRequestArrow, label: "Pull rebase", value: "PullRebase" },
];

export function RemoteOperationSettings() {
	const primaryAction = useSetting("RemotePrimaryAction");

	return (
		<div className="space-y-5">
			<SettingGroup
				description="Choose the fetch or pull operation used by the toolbar split button."
				title="Default Fetch/Pull Action"
			>
				<SegmentedControl>
					{primaryActions.map((action) => (
						<SegmentedButton
							icon={<action.icon aria-hidden="true" className="size-4" />}
							isActive={primaryAction === action.value}
							key={action.value}
							label={action.label}
							onClick={() =>
								void setSetting("RemotePrimaryAction", action.value)
							}
						/>
					))}
				</SegmentedControl>
			</SettingGroup>
		</div>
	);
}
