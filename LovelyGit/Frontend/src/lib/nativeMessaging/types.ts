import {
	NativeMessageType as GeneratedNativeMessageType,
	NativeMessageTypesWithRequest as GeneratedNativeMessageTypesWithRequest,
	NativeMessageTypesWithResponse as GeneratedNativeMessageTypesWithResponse,
	type NativeMessageType as GeneratedNativeMessageTypeValue,
	type NativeMessageTypesWithRequest as GeneratedNativeMessageTypesWithRequestValue,
	type NativeMessageTypesWithRequestAndResponse,
	type NativeMessageTypesWithRequestWithoutResponse,
	type NativeMessageTypesWithResponseWithoutRequest,
	type NativeMessageTypesWithResponse as GeneratedNativeMessageTypesWithResponseValue,
	type NativeRequestBodies,
	type NativeResponseBodies,
} from "@/generated/native-message-contracts";
import type {
	NativeMessageRequest as GeneratedNativeMessageRequest,
	NativeMessageResponse as GeneratedNativeMessageResponse,
} from "@/generated/types";

export const NativeMessageType = GeneratedNativeMessageType;
export const NativeMessageTypesWithRequest =
	GeneratedNativeMessageTypesWithRequest;
export const NativeMessageTypesWithResponse =
	GeneratedNativeMessageTypesWithResponse;
export type NativeMessageType = GeneratedNativeMessageTypeValue;
export type NativeMessageTypesWithRequest =
	GeneratedNativeMessageTypesWithRequestValue;
export type NativeMessageTypesWithResponse =
	GeneratedNativeMessageTypesWithResponseValue;
export type {
	NativeMessageTypesWithRequestAndResponse,
	NativeMessageTypesWithRequestWithoutResponse,
	NativeMessageTypesWithResponseWithoutRequest,
	NativeRequestBodies,
	NativeResponseBodies,
};

export type NativeMessageRequest<
	TMessageType extends NativeMessageTypesWithRequest,
> =
	GeneratedNativeMessageRequest<NativeRequestBodies[TMessageType]>;

export type NativeMessageResponse<
	TMessageType extends NativeMessageTypesWithResponse,
> =
	GeneratedNativeMessageResponse<NativeResponseBodies[TMessageType]>;

export type NativeSubscriber<
	TMessageType extends NativeMessageTypesWithResponse,
> = (
	response: NativeMessageResponse<TMessageType>,
) => void;
