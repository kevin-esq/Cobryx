import type { Metadata } from "next";

import { DashboardShell } from "@/components/dashboard-shell";
import { getServerDictionary } from "@/lib/i18n/server";

export async function generateMetadata(): Promise<Metadata> {
  const { dictionary } = await getServerDictionary();
  return { title: dictionary.dashboard.title };
}

export default function DashboardPage() {
  return <DashboardShell />;
}
