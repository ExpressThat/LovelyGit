import { Moon, Sun, SunMoon } from "lucide-react";
import type { AppTheme } from "@/generated/types";
import { setSetting, useSetting } from "@/lib/settings/settingsStore";
import {
	SegmentedButton,
	SegmentedControl,
	SettingGroup,
} from "./SettingsControls";

const themeOptions: Array<{
	icon: typeof Sun;
	label: string;
	value: AppTheme;
}> = [
	{ icon: Sun, label: "Light", value: "Light" },
	{ icon: Moon, label: "Dark", value: "Dark" },
	{ icon: SunMoon, label: "System", value: "System" },
];

export function ThemeSettings() {
	const theme = useSetting("Theme");

	return (
		<div className="space-y-5">
			<SettingGroup
				description="Choose the colour theme used throughout LovelyGit."
				title="Colour Theme"
			>
				<SegmentedControl>
					{themeOptions.map((option) => (
						<SegmentedButton
							icon={<option.icon aria-hidden="true" className="size-4" />}
							isActive={theme === option.value}
							key={option.value}
							label={option.label}
							onClick={() => void setSetting("Theme", option.value)}
						/>
					))}
				</SegmentedControl>
			</SettingGroup>
		</div>
	);
}
