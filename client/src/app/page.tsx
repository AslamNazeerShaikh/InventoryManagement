import { redirect } from "next/navigation";

export default function Home() {
  // The authenticated shell (app) redirects to /login when there is no session.
  redirect("/dashboard");
}
