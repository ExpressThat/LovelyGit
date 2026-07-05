export type ThemeOption = {
	accent: string;
	aliases?: string;
	background: string;
	card: string;
	description: string;
	foreground: string;
	isDark: boolean;
	label: string;
	value: string;
	variables: ThemeVariables;
};

export type ThemeVariables = {
	accent: string;
	accentForeground: string;
	background: string;
	border: string;
	card: string;
	cardForeground: string;
	foreground: string;
	input: string;
	muted: string;
	mutedForeground: string;
	popover: string;
	popoverForeground: string;
	primary: string;
	primaryForeground: string;
	ring: string;
	secondary: string;
	secondaryForeground: string;
	sidebar: string;
	sidebarAccent: string;
	sidebarAccentForeground: string;
	sidebarBorder: string;
	sidebarForeground: string;
	sidebarPrimary: string;
	sidebarPrimaryForeground: string;
	sidebarRing: string;
};

type ThemeSeed = {
	accentHue: number;
	aliases?: string;
	baseHue: number;
	description: string;
	isDark: boolean;
	label: string;
	value: string;
};

const coreThemeOptions: ThemeOption[] = [
	createThemeOption({
		accentHue: 248,
		aliases: "light lite white bright morning glass",
		baseHue: 248,
		description: "Clean, bright, and low-friction.",
		isDark: false,
		label: "Morning Glass",
		value: "Morning",
	}),
	createThemeOption({
		accentHue: 250,
		aliases: "dark black night midnight ink",
		baseHue: 260,
		description: "A quiet dark workspace.",
		isDark: true,
		label: "Midnight Ink",
		value: "Midnight",
	}),
];

const designedThemeSeeds: ThemeSeed[] = [
	{
		accentHue: 356,
		aliases: "red ruby crimson light bright pale",
		baseHue: 4,
		description: "A light ruby palette with clear ruby controls.",
		isDark: false,
		label: "Ruby Light",
		value: "RubyLight",
	},
	{
		accentHue: 356,
		aliases: "red ruby crimson dark night deep",
		baseHue: 348,
		description: "A dark ruby palette with focused ruby accents.",
		isDark: true,
		label: "Ruby Dark",
		value: "RubyDark",
	},
	{
		accentHue: 14,
		aliases: "coral salmon shell light bright pale",
		baseHue: 22,
		description: "A light coral palette with clear coral controls.",
		isDark: false,
		label: "Coral Light",
		value: "CoralLight",
	},
	{
		accentHue: 14,
		aliases: "coral salmon shell dark night deep",
		baseHue: 6,
		description: "A dark coral palette with focused coral accents.",
		isDark: true,
		label: "Coral Dark",
		value: "CoralDark",
	},
	{
		accentHue: 24,
		aliases: "terracotta clay adobe light bright pale",
		baseHue: 32,
		description: "A light terracotta palette with clear terracotta controls.",
		isDark: false,
		label: "Terracotta Light",
		value: "TerracottaLight",
	},
	{
		accentHue: 24,
		aliases: "terracotta clay adobe dark night deep",
		baseHue: 16,
		description: "A dark terracotta palette with focused terracotta accents.",
		isDark: true,
		label: "Terracotta Dark",
		value: "TerracottaDark",
	},
	{
		accentHue: 34,
		aliases: "copper bronze metal light bright pale",
		baseHue: 42,
		description: "A light copper palette with clear copper controls.",
		isDark: false,
		label: "Copper Light",
		value: "CopperLight",
	},
	{
		accentHue: 34,
		aliases: "copper bronze metal dark night deep",
		baseHue: 26,
		description: "A dark copper palette with focused copper accents.",
		isDark: true,
		label: "Copper Dark",
		value: "CopperDark",
	},
	{
		accentHue: 46,
		aliases: "amber honey gold light bright pale",
		baseHue: 54,
		description: "A light amber palette with clear amber controls.",
		isDark: false,
		label: "Amber Light",
		value: "AmberLight",
	},
	{
		accentHue: 46,
		aliases: "amber honey gold dark night deep",
		baseHue: 38,
		description: "A dark amber palette with focused amber accents.",
		isDark: true,
		label: "Amber Dark",
		value: "AmberDark",
	},
	{
		accentHue: 58,
		aliases: "saffron turmeric yellow light bright pale",
		baseHue: 66,
		description: "A light saffron palette with clear saffron controls.",
		isDark: false,
		label: "Saffron Light",
		value: "SaffronLight",
	},
	{
		accentHue: 58,
		aliases: "saffron turmeric yellow dark night deep",
		baseHue: 50,
		description: "A dark saffron palette with focused saffron accents.",
		isDark: true,
		label: "Saffron Dark",
		value: "SaffronDark",
	},
	{
		accentHue: 70,
		aliases: "citrine lemon yellow light bright pale",
		baseHue: 78,
		description: "A light citrine palette with clear citrine controls.",
		isDark: false,
		label: "Citrine Light",
		value: "CitrineLight",
	},
	{
		accentHue: 70,
		aliases: "citrine lemon yellow dark night deep",
		baseHue: 62,
		description: "A dark citrine palette with focused citrine accents.",
		isDark: true,
		label: "Citrine Dark",
		value: "CitrineDark",
	},
	{
		accentHue: 86,
		aliases: "olive moss lichen light bright pale",
		baseHue: 94,
		description: "A light olive palette with clear olive controls.",
		isDark: false,
		label: "Olive Light",
		value: "OliveLight",
	},
	{
		accentHue: 86,
		aliases: "olive moss lichen dark night deep",
		baseHue: 78,
		description: "A dark olive palette with focused olive accents.",
		isDark: true,
		label: "Olive Dark",
		value: "OliveDark",
	},
	{
		accentHue: 98,
		aliases: "chartreuse lime electric light bright pale",
		baseHue: 106,
		description: "A light chartreuse palette with clear chartreuse controls.",
		isDark: false,
		label: "Chartreuse Light",
		value: "ChartreuseLight",
	},
	{
		accentHue: 98,
		aliases: "chartreuse lime electric dark night deep",
		baseHue: 90,
		description: "A dark chartreuse palette with focused chartreuse accents.",
		isDark: true,
		label: "Chartreuse Dark",
		value: "ChartreuseDark",
	},
	{
		accentHue: 112,
		aliases: "sage herb eucalyptus light bright pale",
		baseHue: 120,
		description: "A light sage palette with clear sage controls.",
		isDark: false,
		label: "Sage Light",
		value: "SageLight",
	},
	{
		accentHue: 112,
		aliases: "sage herb eucalyptus dark night deep",
		baseHue: 104,
		description: "A dark sage palette with focused sage accents.",
		isDark: true,
		label: "Sage Dark",
		value: "SageDark",
	},
	{
		accentHue: 126,
		aliases: "fern leaf garden light bright pale",
		baseHue: 134,
		description: "A light fern palette with clear fern controls.",
		isDark: false,
		label: "Fern Light",
		value: "FernLight",
	},
	{
		accentHue: 126,
		aliases: "fern leaf garden dark night deep",
		baseHue: 118,
		description: "A dark fern palette with focused fern accents.",
		isDark: true,
		label: "Fern Dark",
		value: "FernDark",
	},
	{
		accentHue: 142,
		aliases: "emerald jewel green light bright pale",
		baseHue: 150,
		description: "A light emerald palette with clear emerald controls.",
		isDark: false,
		label: "Emerald Light",
		value: "EmeraldLight",
	},
	{
		accentHue: 142,
		aliases: "emerald jewel green dark night deep",
		baseHue: 134,
		description: "A dark emerald palette with focused emerald accents.",
		isDark: true,
		label: "Emerald Dark",
		value: "EmeraldDark",
	},
	{
		accentHue: 154,
		aliases: "jade celadon green light bright pale",
		baseHue: 162,
		description: "A light jade palette with clear jade controls.",
		isDark: false,
		label: "Jade Light",
		value: "JadeLight",
	},
	{
		accentHue: 154,
		aliases: "jade celadon green dark night deep",
		baseHue: 146,
		description: "A dark jade palette with focused jade accents.",
		isDark: true,
		label: "Jade Dark",
		value: "JadeDark",
	},
	{
		accentHue: 166,
		aliases: "mint seafoam fresh light bright pale",
		baseHue: 174,
		description: "A light mint palette with clear mint controls.",
		isDark: false,
		label: "Mint Light",
		value: "MintLight",
	},
	{
		accentHue: 166,
		aliases: "mint seafoam fresh dark night deep",
		baseHue: 158,
		description: "A dark mint palette with focused mint accents.",
		isDark: true,
		label: "Mint Dark",
		value: "MintDark",
	},
	{
		accentHue: 178,
		aliases: "teal bluegreen lagoon light bright pale",
		baseHue: 186,
		description: "A light teal palette with clear teal controls.",
		isDark: false,
		label: "Teal Light",
		value: "TealLight",
	},
	{
		accentHue: 178,
		aliases: "teal bluegreen lagoon dark night deep",
		baseHue: 170,
		description: "A dark teal palette with focused teal accents.",
		isDark: true,
		label: "Teal Dark",
		value: "TealDark",
	},
	{
		accentHue: 188,
		aliases: "aqua cyan water light bright pale",
		baseHue: 196,
		description: "A light aqua palette with clear aqua controls.",
		isDark: false,
		label: "Aqua Light",
		value: "AquaLight",
	},
	{
		accentHue: 188,
		aliases: "aqua cyan water dark night deep",
		baseHue: 180,
		description: "A dark aqua palette with focused aqua accents.",
		isDark: true,
		label: "Aqua Dark",
		value: "AquaDark",
	},
	{
		accentHue: 198,
		aliases: "cyan ice bright light bright pale",
		baseHue: 206,
		description: "A light cyan palette with clear cyan controls.",
		isDark: false,
		label: "Cyan Light",
		value: "CyanLight",
	},
	{
		accentHue: 198,
		aliases: "cyan ice bright dark night deep",
		baseHue: 190,
		description: "A dark cyan palette with focused cyan accents.",
		isDark: true,
		label: "Cyan Dark",
		value: "CyanDark",
	},
	{
		accentHue: 208,
		aliases: "sky blue air light bright pale",
		baseHue: 216,
		description: "A light sky palette with clear sky controls.",
		isDark: false,
		label: "Sky Light",
		value: "SkyLight",
	},
	{
		accentHue: 208,
		aliases: "sky blue air dark night deep",
		baseHue: 200,
		description: "A dark sky palette with focused sky accents.",
		isDark: true,
		label: "Sky Dark",
		value: "SkyDark",
	},
	{
		accentHue: 218,
		aliases: "azure clear blue light bright pale",
		baseHue: 226,
		description: "A light azure palette with clear azure controls.",
		isDark: false,
		label: "Azure Light",
		value: "AzureLight",
	},
	{
		accentHue: 218,
		aliases: "azure clear blue dark night deep",
		baseHue: 210,
		description: "A dark azure palette with focused azure accents.",
		isDark: true,
		label: "Azure Dark",
		value: "AzureDark",
	},
	{
		accentHue: 232,
		aliases: "cobalt royal blue light bright pale",
		baseHue: 240,
		description: "A light cobalt palette with clear cobalt controls.",
		isDark: false,
		label: "Cobalt Light",
		value: "CobaltLight",
	},
	{
		accentHue: 232,
		aliases: "cobalt royal blue dark night deep",
		baseHue: 224,
		description: "A dark cobalt palette with focused cobalt accents.",
		isDark: true,
		label: "Cobalt Dark",
		value: "CobaltDark",
	},
	{
		accentHue: 244,
		aliases: "sapphire deep blue light bright pale",
		baseHue: 252,
		description: "A light sapphire palette with clear sapphire controls.",
		isDark: false,
		label: "Sapphire Light",
		value: "SapphireLight",
	},
	{
		accentHue: 244,
		aliases: "sapphire deep blue dark night deep",
		baseHue: 236,
		description: "A dark sapphire palette with focused sapphire accents.",
		isDark: true,
		label: "Sapphire Dark",
		value: "SapphireDark",
	},
	{
		accentHue: 256,
		aliases: "indigo blue violet light bright pale",
		baseHue: 264,
		description: "A light indigo palette with clear indigo controls.",
		isDark: false,
		label: "Indigo Light",
		value: "IndigoLight",
	},
	{
		accentHue: 256,
		aliases: "indigo blue violet dark night deep",
		baseHue: 248,
		description: "A dark indigo palette with focused indigo accents.",
		isDark: true,
		label: "Indigo Dark",
		value: "IndigoDark",
	},
	{
		accentHue: 266,
		aliases: "periwinkle soft blue purple light bright pale",
		baseHue: 274,
		description: "A light periwinkle palette with clear periwinkle controls.",
		isDark: false,
		label: "Periwinkle Light",
		value: "PeriwinkleLight",
	},
	{
		accentHue: 266,
		aliases: "periwinkle soft blue purple dark night deep",
		baseHue: 258,
		description: "A dark periwinkle palette with focused periwinkle accents.",
		isDark: true,
		label: "Periwinkle Dark",
		value: "PeriwinkleDark",
	},
	{
		accentHue: 276,
		aliases: "violet purple iris light bright pale",
		baseHue: 284,
		description: "A light violet palette with clear violet controls.",
		isDark: false,
		label: "Violet Light",
		value: "VioletLight",
	},
	{
		accentHue: 276,
		aliases: "violet purple iris dark night deep",
		baseHue: 268,
		description: "A dark violet palette with focused violet accents.",
		isDark: true,
		label: "Violet Dark",
		value: "VioletDark",
	},
	{
		accentHue: 286,
		aliases: "lavender lilac soft light bright pale",
		baseHue: 294,
		description: "A light lavender palette with clear lavender controls.",
		isDark: false,
		label: "Lavender Light",
		value: "LavenderLight",
	},
	{
		accentHue: 286,
		aliases: "lavender lilac soft dark night deep",
		baseHue: 278,
		description: "A dark lavender palette with focused lavender accents.",
		isDark: true,
		label: "Lavender Dark",
		value: "LavenderDark",
	},
	{
		accentHue: 298,
		aliases: "orchid mauve purple light bright pale",
		baseHue: 306,
		description: "A light orchid palette with clear orchid controls.",
		isDark: false,
		label: "Orchid Light",
		value: "OrchidLight",
	},
	{
		accentHue: 298,
		aliases: "orchid mauve purple dark night deep",
		baseHue: 290,
		description: "A dark orchid palette with focused orchid accents.",
		isDark: true,
		label: "Orchid Dark",
		value: "OrchidDark",
	},
	{
		accentHue: 312,
		aliases: "magenta fuchsia pink light bright pale",
		baseHue: 320,
		description: "A light magenta palette with clear magenta controls.",
		isDark: false,
		label: "Magenta Light",
		value: "MagentaLight",
	},
	{
		accentHue: 312,
		aliases: "magenta fuchsia pink dark night deep",
		baseHue: 304,
		description: "A dark magenta palette with focused magenta accents.",
		isDark: true,
		label: "Magenta Dark",
		value: "MagentaDark",
	},
	{
		accentHue: 326,
		aliases: "raspberry berry pink light bright pale",
		baseHue: 334,
		description: "A light raspberry palette with clear raspberry controls.",
		isDark: false,
		label: "Raspberry Light",
		value: "RaspberryLight",
	},
	{
		accentHue: 326,
		aliases: "raspberry berry pink dark night deep",
		baseHue: 318,
		description: "A dark raspberry palette with focused raspberry accents.",
		isDark: true,
		label: "Raspberry Dark",
		value: "RaspberryDark",
	},
	{
		accentHue: 340,
		aliases: "rose blush pink light bright pale",
		baseHue: 348,
		description: "A light rose palette with clear rose controls.",
		isDark: false,
		label: "Rose Light",
		value: "RoseLight",
	},
	{
		accentHue: 340,
		aliases: "rose blush pink dark night deep",
		baseHue: 332,
		description: "A dark rose palette with focused rose accents.",
		isDark: true,
		label: "Rose Dark",
		value: "RoseDark",
	},
	{
		accentHue: 350,
		aliases: "plum wine mulberry light bright pale",
		baseHue: 358,
		description: "A light plum palette with clear plum controls.",
		isDark: false,
		label: "Plum Light",
		value: "PlumLight",
	},
	{
		accentHue: 350,
		aliases: "plum wine mulberry dark night deep",
		baseHue: 342,
		description: "A dark plum palette with focused plum accents.",
		isDark: true,
		label: "Plum Dark",
		value: "PlumDark",
	},
	{
		accentHue: 222,
		aliases: "slate blue gray light bright pale",
		baseHue: 230,
		description: "A light slate palette with clear slate controls.",
		isDark: false,
		label: "Slate Light",
		value: "SlateLight",
	},
	{
		accentHue: 222,
		aliases: "slate blue gray dark night deep",
		baseHue: 214,
		description: "A dark slate palette with focused slate accents.",
		isDark: true,
		label: "Slate Dark",
		value: "SlateDark",
	},
	{
		accentHue: 232,
		aliases: "graphite neutral gray light bright pale",
		baseHue: 240,
		description: "A light graphite palette with clear graphite controls.",
		isDark: false,
		label: "Graphite Light",
		value: "GraphiteLight",
	},
	{
		accentHue: 232,
		aliases: "graphite neutral gray dark night deep",
		baseHue: 224,
		description: "A dark graphite palette with focused graphite accents.",
		isDark: true,
		label: "Graphite Dark",
		value: "GraphiteDark",
	},
	{
		accentHue: 78,
		aliases: "stone mineral gray light bright pale",
		baseHue: 86,
		description: "A light stone palette with clear stone controls.",
		isDark: false,
		label: "Stone Light",
		value: "StoneLight",
	},
	{
		accentHue: 78,
		aliases: "stone mineral gray dark night deep",
		baseHue: 70,
		description: "A dark stone palette with focused stone accents.",
		isDark: true,
		label: "Stone Dark",
		value: "StoneDark",
	},
	{
		accentHue: 62,
		aliases: "sand dune parchment light bright pale",
		baseHue: 70,
		description: "A light sand palette with clear sand controls.",
		isDark: false,
		label: "Sand Light",
		value: "SandLight",
	},
	{
		accentHue: 62,
		aliases: "sand dune parchment dark night deep",
		baseHue: 54,
		description: "A dark sand palette with focused sand accents.",
		isDark: true,
		label: "Sand Dark",
		value: "SandDark",
	},
	{
		accentHue: 48,
		aliases: "linen cream cloth light bright pale",
		baseHue: 56,
		description: "A light linen palette with clear linen controls.",
		isDark: false,
		label: "Linen Light",
		value: "LinenLight",
	},
	{
		accentHue: 48,
		aliases: "linen cream cloth dark night deep",
		baseHue: 40,
		description: "A dark linen palette with focused linen accents.",
		isDark: true,
		label: "Linen Dark",
		value: "LinenDark",
	},
	{
		accentHue: 36,
		aliases: "walnut wood brown light bright pale",
		baseHue: 44,
		description: "A light walnut palette with clear walnut controls.",
		isDark: false,
		label: "Walnut Light",
		value: "WalnutLight",
	},
	{
		accentHue: 36,
		aliases: "walnut wood brown dark night deep",
		baseHue: 28,
		description: "A dark walnut palette with focused walnut accents.",
		isDark: true,
		label: "Walnut Dark",
		value: "WalnutDark",
	},
	{
		accentHue: 28,
		aliases: "espresso coffee brown light bright pale",
		baseHue: 36,
		description: "A light espresso palette with clear espresso controls.",
		isDark: false,
		label: "Espresso Light",
		value: "EspressoLight",
	},
	{
		accentHue: 28,
		aliases: "espresso coffee brown dark night deep",
		baseHue: 20,
		description: "A dark espresso palette with focused espresso accents.",
		isDark: true,
		label: "Espresso Dark",
		value: "EspressoDark",
	},
	{
		accentHue: 250,
		aliases: "charcoal dark neutral light bright pale",
		baseHue: 258,
		description: "A light charcoal palette with clear charcoal controls.",
		isDark: false,
		label: "Charcoal Light",
		value: "CharcoalLight",
	},
	{
		accentHue: 250,
		aliases: "charcoal dark neutral dark night deep",
		baseHue: 242,
		description: "A dark charcoal palette with focused charcoal accents.",
		isDark: true,
		label: "Charcoal Dark",
		value: "CharcoalDark",
	},
	{
		accentHue: 136,
		aliases: "pine evergreen forest light bright pale",
		baseHue: 144,
		description: "A light pine palette with clear pine controls.",
		isDark: false,
		label: "Pine Light",
		value: "PineLight",
	},
	{
		accentHue: 136,
		aliases: "pine evergreen forest dark night deep",
		baseHue: 128,
		description: "A dark pine palette with focused pine accents.",
		isDark: true,
		label: "Pine Dark",
		value: "PineDark",
	},
	{
		accentHue: 168,
		aliases: "spruce blue green light bright pale",
		baseHue: 176,
		description: "A light spruce palette with clear spruce controls.",
		isDark: false,
		label: "Spruce Light",
		value: "SpruceLight",
	},
	{
		accentHue: 168,
		aliases: "spruce blue green dark night deep",
		baseHue: 160,
		description: "A dark spruce palette with focused spruce accents.",
		isDark: true,
		label: "Spruce Dark",
		value: "SpruceDark",
	},
	{
		accentHue: 190,
		aliases: "petrol dark teal light bright pale",
		baseHue: 198,
		description: "A light petrol palette with clear petrol controls.",
		isDark: false,
		label: "Petrol Light",
		value: "PetrolLight",
	},
	{
		accentHue: 190,
		aliases: "petrol dark teal dark night deep",
		baseHue: 182,
		description: "A dark petrol palette with focused petrol accents.",
		isDark: true,
		label: "Petrol Dark",
		value: "PetrolDark",
	},
	{
		accentHue: 204,
		aliases: "marine ocean blue light bright pale",
		baseHue: 212,
		description: "A light marine palette with clear marine controls.",
		isDark: false,
		label: "Marine Light",
		value: "MarineLight",
	},
	{
		accentHue: 204,
		aliases: "marine ocean blue dark night deep",
		baseHue: 196,
		description: "A dark marine palette with focused marine accents.",
		isDark: true,
		label: "Marine Dark",
		value: "MarineDark",
	},
	{
		accentHue: 216,
		aliases: "denim blue fabric light bright pale",
		baseHue: 224,
		description: "A light denim palette with clear denim controls.",
		isDark: false,
		label: "Denim Light",
		value: "DenimLight",
	},
	{
		accentHue: 216,
		aliases: "denim blue fabric dark night deep",
		baseHue: 208,
		description: "A dark denim palette with focused denim accents.",
		isDark: true,
		label: "Denim Dark",
		value: "DenimDark",
	},
	{
		accentHue: 306,
		aliases: "aubergine eggplant purple light bright pale",
		baseHue: 314,
		description: "A light aubergine palette with clear aubergine controls.",
		isDark: false,
		label: "Aubergine Light",
		value: "AubergineLight",
	},
	{
		accentHue: 306,
		aliases: "aubergine eggplant purple dark night deep",
		baseHue: 298,
		description: "A dark aubergine palette with focused aubergine accents.",
		isDark: true,
		label: "Aubergine Dark",
		value: "AubergineDark",
	},
	{
		accentHue: 348,
		aliases: "cherry red fruit light bright pale",
		baseHue: 356,
		description: "A light cherry palette with clear cherry controls.",
		isDark: false,
		label: "Cherry Light",
		value: "CherryLight",
	},
	{
		accentHue: 348,
		aliases: "cherry red fruit dark night deep",
		baseHue: 340,
		description: "A dark cherry palette with focused cherry accents.",
		isDark: true,
		label: "Cherry Dark",
		value: "CherryDark",
	},
	{
		accentHue: 32,
		aliases: "papaya mango orange light bright pale",
		baseHue: 40,
		description: "A light papaya palette with clear papaya controls.",
		isDark: false,
		label: "Papaya Light",
		value: "PapayaLight",
	},
	{
		accentHue: 32,
		aliases: "papaya mango orange dark night deep",
		baseHue: 24,
		description: "A dark papaya palette with focused papaya accents.",
		isDark: true,
		label: "Papaya Dark",
		value: "PapayaDark",
	},
	{
		accentHue: 104,
		aliases: "matcha tea green light bright pale",
		baseHue: 112,
		description: "A light matcha palette with clear matcha controls.",
		isDark: false,
		label: "Matcha Light",
		value: "MatchaLight",
	},
	{
		accentHue: 104,
		aliases: "matcha tea green dark night deep",
		baseHue: 96,
		description: "A dark matcha palette with focused matcha accents.",
		isDark: true,
		label: "Matcha Dark",
		value: "MatchaDark",
	},
	{
		accentHue: 200,
		aliases: "ice frost glacial light bright pale",
		baseHue: 208,
		description: "A light ice palette with clear ice controls.",
		isDark: false,
		label: "Ice Light",
		value: "IceLight",
	},
	{
		accentHue: 200,
		aliases: "ice frost glacial dark night deep",
		baseHue: 192,
		description: "A dark ice palette with focused ice accents.",
		isDark: true,
		label: "Ice Dark",
		value: "IceDark",
	},
];

export const themeOptions = [
	...coreThemeOptions,
	...designedThemeSeeds.map(createThemeOption),
];

export function getThemeOption(value: string, fallbackValue = "Morning") {
	if (value === "Light") {
		return getThemeOption("Morning");
	}
	if (value === "Dark") {
		return getThemeOption("Midnight");
	}
	if (value === "System") {
		return getThemeOption(fallbackValue);
	}
	return (
		themeOptions.find((option) => option.value === value) ??
		themeOptions.find((option) => option.value === fallbackValue) ??
		themeOptions[0]
	);
}

function createThemeOption(seed: ThemeSeed): ThemeOption {
	const variables = seed.isDark
		? createDarkVariables(seed.baseHue, seed.accentHue)
		: createLightVariables(seed.baseHue, seed.accentHue);
	return {
		accent: okLchToHex(variables.primary),
		aliases:
			`${"aliases" in seed ? seed.aliases : ""} ${seed.label} ${seed.description}`.toLowerCase(),
		background: okLchToHex(variables.background),
		card: okLchToHex(variables.card),
		description: seed.description,
		foreground: okLchToHex(variables.foreground),
		isDark: seed.isDark,
		label: seed.label,
		value: seed.value,
		variables,
	};
}

function createLightVariables(
	baseHue: number,
	accentHue: number,
): ThemeVariables {
	return {
		accent: oklch(0.87, 0.04, accentHue),
		accentForeground: oklch(0.22, 0.035, baseHue),
		background: oklch(0.975, 0.012, baseHue),
		border: oklch(0.81, 0.028, baseHue),
		card: oklch(0.94, 0.018, baseHue),
		cardForeground: oklch(0.22, 0.03, baseHue),
		foreground: oklch(0.22, 0.03, baseHue),
		input: oklch(0.81, 0.028, baseHue),
		muted: oklch(0.9, 0.02, baseHue),
		mutedForeground: oklch(0.48, 0.035, baseHue),
		popover: oklch(0.988, 0.01, baseHue),
		popoverForeground: oklch(0.22, 0.03, baseHue),
		primary: oklch(0.48, 0.11, accentHue),
		primaryForeground: oklch(0.99, 0.006, baseHue),
		ring: oklch(0.6, 0.08, accentHue),
		secondary: oklch(0.89, 0.026, baseHue),
		secondaryForeground: oklch(0.25, 0.035, baseHue),
		sidebar: oklch(0.95, 0.016, baseHue),
		sidebarAccent: oklch(0.88, 0.026, baseHue),
		sidebarAccentForeground: oklch(0.22, 0.03, baseHue),
		sidebarBorder: oklch(0.81, 0.028, baseHue),
		sidebarForeground: oklch(0.22, 0.03, baseHue),
		sidebarPrimary: oklch(0.48, 0.11, accentHue),
		sidebarPrimaryForeground: oklch(0.99, 0.006, baseHue),
		sidebarRing: oklch(0.6, 0.08, accentHue),
	};
}

function createDarkVariables(
	baseHue: number,
	accentHue: number,
): ThemeVariables {
	return {
		accent: oklch(0.32, 0.05, accentHue),
		accentForeground: oklch(0.965, 0.014, baseHue),
		background: oklch(0.15, 0.018, baseHue),
		border: "oklch(1 0 0 / 12%)",
		card: oklch(0.215, 0.026, baseHue),
		cardForeground: oklch(0.965, 0.014, baseHue),
		foreground: oklch(0.965, 0.014, baseHue),
		input: "oklch(1 0 0 / 16%)",
		muted: oklch(0.27, 0.03, baseHue),
		mutedForeground: oklch(0.73, 0.04, baseHue),
		popover: oklch(0.195, 0.024, baseHue),
		popoverForeground: oklch(0.965, 0.014, baseHue),
		primary: oklch(0.73, 0.13, accentHue),
		primaryForeground: oklch(0.15, 0.018, baseHue),
		ring: oklch(0.73, 0.13, accentHue),
		secondary: oklch(0.28, 0.034, baseHue),
		secondaryForeground: oklch(0.965, 0.014, baseHue),
		sidebar: oklch(0.205, 0.024, baseHue),
		sidebarAccent: oklch(0.28, 0.034, baseHue),
		sidebarAccentForeground: oklch(0.965, 0.014, baseHue),
		sidebarBorder: "oklch(1 0 0 / 12%)",
		sidebarForeground: oklch(0.965, 0.014, baseHue),
		sidebarPrimary: oklch(0.73, 0.13, accentHue),
		sidebarPrimaryForeground: oklch(0.15, 0.018, baseHue),
		sidebarRing: oklch(0.73, 0.13, accentHue),
	};
}

function oklch(lightness: number, chroma: number, hue: number) {
	return `oklch(${lightness} ${chroma} ${normalizeHue(hue)})`;
}

function normalizeHue(hue: number) {
	return ((hue % 360) + 360) % 360;
}

function okLchToHex(value: string) {
	const match = value.match(/oklch\(([\d.]+) ([\d.]+) ([\d.]+)\)/);
	if (!match) {
		return "#778499";
	}

	const lightness = Number(match[1]);
	const chroma = Number(match[2]);
	const hue = (Number(match[3]) * Math.PI) / 180;
	const a = chroma * Math.cos(hue);
	const b = chroma * Math.sin(hue);
	const lab = okLabToLinearSrgb(lightness, a, b).map(linearToSrgb);
	return `#${lab.map(toHexChannel).join("")}`;
}

function okLabToLinearSrgb(lightness: number, a: number, b: number) {
	const l = lightness + 0.3963377774 * a + 0.2158037573 * b;
	const m = lightness - 0.1055613458 * a - 0.0638541728 * b;
	const s = lightness - 0.0894841775 * a - 1.291485548 * b;
	const l3 = l ** 3;
	const m3 = m ** 3;
	const s3 = s ** 3;

	return [
		4.0767416621 * l3 - 3.3077115913 * m3 + 0.2309699292 * s3,
		-1.2684380046 * l3 + 2.6097574011 * m3 - 0.3413193965 * s3,
		-0.0041960863 * l3 - 0.7034186147 * m3 + 1.707614701 * s3,
	];
}

function linearToSrgb(value: number) {
	const clamped = Math.min(1, Math.max(0, value));
	return clamped <= 0.0031308
		? 12.92 * clamped
		: 1.055 * clamped ** (1 / 2.4) - 0.055;
}

function toHexChannel(value: number) {
	return Math.round(value * 255)
		.toString(16)
		.padStart(2, "0");
}
