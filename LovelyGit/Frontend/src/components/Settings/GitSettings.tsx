import { BadgeCheck } from "lucide-react";
import { Switch } from "@/components/ui/switch";
import { setSetting, useSetting } from "@/lib/settings/settingsStore";
import { SettingGroup } from "./SettingsControls";

export function GitSettings() {
	const signCommits = useSetting("SignCommitsByDefault");

	return (
		<div className="space-y-5">
			<SettingGroup
				description="Ask Git to sign new and amended commits with your configured GPG or SSH key."
				title="Commit Signing"
			>
				<label
					className="flex cursor-pointer items-center justify-between gap-4 rounded-lg border bg-card p-3"
					htmlFor="sign-commits-by-default"
				>
					<span className="flex min-w-0 items-start gap-3">
						<BadgeCheck
							aria-hidden="true"
							className="mt-0.5 size-4 shrink-0 text-primary"
						/>
						<span className="grid gap-0.5">
							<span className="text-sm font-medium">
								Sign commits by default
							</span>
							<span className="text-xs text-muted-foreground">
								Uses Git&apos;s user.signingKey and gpg.format configuration.
							</span>
						</span>
					</span>
					<Switch
						checked={signCommits}
						id="sign-commits-by-default"
						onCheckedChange={(checked) =>
							void setSetting("SignCommitsByDefault", checked)
						}
					/>
				</label>
			</SettingGroup>
		</div>
	);
}
