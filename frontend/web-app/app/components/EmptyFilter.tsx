import { useParamsStore } from "@/hooks/useParamsStore";
import Heading from "./Heading";
import { Button } from "flowbite-react";

type Props = {
    title?: string;
    subtitle?: string;
    showReset?: boolean;
}
export default function EmptyFilter({ title, subtitle, showReset }: Props) {
    title = title || "No results found";
    subtitle = subtitle || "Try adjusting your filter or search criteria to find what you're looking for.";
    showReset

    const reset = useParamsStore(state => state.reset);


  return (
    <div className="flex flex-col gap-2 items-center justify-center h-[40vh] shadow-lg">
        <Heading title={title} subtitle={subtitle} center={true} />
        <div className="mt-4">
            {showReset && (
                <Button color="light" onClick={() => reset()}>
                    Remove Filters
                </Button>
            )}
        </div>
    </div>
  )
}
