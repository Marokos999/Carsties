import { signIn } from "next-auth/react"

export { auth as middleware } from "@/auth"

export const config = {
  matcher: [
    '/session'
  ],
  pages:{
    signIn: '/signin'
  }
}