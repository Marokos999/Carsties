"use client";

import { useSession } from "next-auth/react";
import SignalRProvider from "./SignalRProvider";
import { ReactNode } from "react";

export default function SignalRProviderWrapper({ children }: { children: ReactNode }) {
  const { data } = useSession();
  return <SignalRProvider user={data?.user ?? null}>{children}</SignalRProvider>;
}
