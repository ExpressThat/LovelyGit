import { PanelLeft } from "lucide-react";
import { Button } from "@/components/ui/button";
import { setSetting, useSetting } from "@/lib/settings/settingsStore";
import { SettingGroup } from "./SettingsControls";

export function GraphViewSettings() {
	const refsPanelOpen = useSetting("CommitGraphRefsPanelOpen");

	return (
		<div className="space-y-5">
			<SettingGroup
				description="Choose whether branch, tag, and stash refs are shown beside the graph."
				title="Refs Panel"
			>
				<Button
					onClick={() =>
						void setSetting("CommitGraphRefsPanelOpen", !refsPanelOpen)
					}
					variant={refsPanelOpen ? "secondary" : "outline"}
				>
					<PanelLeft aria-hidden="true" className="size-4" />
					{refsPanelOpen ? "Refs panel shown" : "Refs panel hidden"}
				</Button>
			</SettingGroup>
		</div>
	);
}
