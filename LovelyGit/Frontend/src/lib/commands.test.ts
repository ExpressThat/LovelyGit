import { beforeEach, describe, expect, it, vi } from "vitest";
import { Setting } from "@/generated/types";
import {
	NativeMessageType,
	requestNativeMessage,
	subscribeNativeMessage,
} from "@/lib/nativeMessaging";
import {
	sendRequestWithoutResponse,
	sendRequestWithResponse,
	subscribeToServerEvent,
} from "./commands";

vi.mock("@/lib/nativeMessaging", async (importOriginal) => ({
	...(await importOriginal<typeof import("@/lib/nativeMessaging")>()),
	requestNativeMessage: vi.fn(),
	subscribeNativeMessage: vi.fn(),
}));

describe("commands", () => {
	beforeEach(() => vi.clearAllMocks());

	it("forwards arguments and a custom timeout", async () => {
		vi.mocked(requestNativeMessage).mockReturnValueOnce(
			Promise.resolve({ Theme: "Dark" }) as never,
		);

		const response = await sendRequestWithResponse(
			{ commandType: NativeMessageType.GetAllSettings },
			{ timeoutMs: 321 },
		);

		expect(response).toEqual({ Theme: "Dark" });
		expect(requestNativeMessage).toHaveBeenCalledWith(
			NativeMessageType.GetAllSettings,
			{},
			321,
		);
	});

	it("serializes a single setting value for the native contract", async () => {
		await sendRequestWithResponse({
			arguments: { setting: Setting.CommitDiffContextLines, value: 7 },
			commandType: NativeMessageType.SetSetting,
		});

		expect(requestNativeMessage).toHaveBeenCalledWith(
			NativeMessageType.SetSetting,
			{ setting: Setting.CommitDiffContextLines, valueJson: "7" },
			undefined,
		);
	});

	it("serializes multiple setting values independently", () => {
		sendRequestWithoutResponse({
			arguments: {
				settingValues: {
					[Setting.CommitDiffWrapLines]: true,
					[Setting.Theme]: "System",
				},
			},
			commandType: NativeMessageType.SetMultipleSettings,
		});

		expect(requestNativeMessage).toHaveBeenCalledWith(
			NativeMessageType.SetMultipleSettings,
			{
				settingValueJsons: {
					[Setting.CommitDiffWrapLines]: "true",
					[Setting.Theme]: '"System"',
				},
			},
		);
	});

	it("only delivers successful server events with bodies", () => {
		let nativeListener: ((response: unknown) => void) | undefined;
		vi.mocked(subscribeNativeMessage).mockImplementationOnce(
			(_event, listener) => {
				nativeListener = listener as (response: unknown) => void;
				return vi.fn();
			},
		);
		const listener = vi.fn();
		subscribeToServerEvent(NativeMessageType.CommitGraphChanged, listener);

		nativeListener?.({ body: { repositoryId: "repo" }, success: true });
		nativeListener?.({ body: null, success: true });
		nativeListener?.({ body: { repositoryId: "ignored" }, success: false });

		expect(listener).toHaveBeenCalledOnce();
		expect(listener).toHaveBeenCalledWith({ repositoryId: "repo" });
	});
});
