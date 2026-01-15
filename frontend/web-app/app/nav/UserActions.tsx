import { Button } from "flowbite-react";
import Link from "next/dist/client/link";


export default function UserActions() {
  return (
    <Button>
        <Link href="/session">Sesion</Link>
    </Button>
  )
}
