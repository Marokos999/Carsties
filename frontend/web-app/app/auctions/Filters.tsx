import { Button, ButtonGroup } from "flowbite-react";

type Props = {
    pageSize: number;
    setPageSize: (pageSize: number) => void;
}

const pageSizeButtons = [4, 8, 12];


export default function Filters({pageSize, setPageSize}: Props) {
  return (
    <div className="flex justify-between items-center mb-4">
        <div>
            <span className="uppercase text-sm text-shadow-gray-500 mr-2">
                Page Size
                <ButtonGroup outline className="ml-2">
                    {pageSizeButtons.map((value, index) => (
                        <Button 
                            key={index} 
                            color={value === pageSize ? 'red' : 'light'} 
                            onClick={() => setPageSize(value)}
                            className="focus:ring-0"
                        >
                            {value}
                        </Button>
                    ))}
                </ButtonGroup>
            </span>
        </div>
    </div>
  )
}
