'use client';

import { useParamsStore } from "@/hooks/useParamsStore";
import Heading from "./Heading";
import { Button } from "flowbite-react";
import { signIn } from "next-auth/react";

type Props = {
    title?: string;
    subtitle?: string;
    showReset?: boolean;
    showLogin?: boolean;
    callbackUrl?: string;
}
export default function EmptyFilter({ title, subtitle, showReset, showLogin, callbackUrl }: Props) {
    title = title || "No results found";
    subtitle = subtitle || "Try adjusting your filter or search criteria to find what you're looking for.";
    showReset,
    showLogin,
    callbackUrl


    const reset = useParamsStore(state => state.reset);


  return (
    <div className="flex flex-col gap-2 items-center justify-center h-[40vh] shadow-lg">
        <Heading title={title} subtitle={subtitle} center={true} />
        <div className="mt-4">
            {showReset && (
                <Button outline color="light" onClick={() => reset()}>
                    Remove Filters
                </Button>
            )}
            {showLogin && (
                <Button outline color="light" onClick={() => signIn('id-server', { redirectTo: callbackUrl })}>
                    Login
                </Button>
            )}
        </div>
    </div>
  )
}
