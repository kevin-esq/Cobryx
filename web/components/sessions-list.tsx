"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useState } from "react";

import { useTranslation } from "@/components/i18n-provider";
import { logout, revokeAllSessions } from "@/lib/auth/client";
import { apiFetch } from "@/lib/api-client";
import type { ApiEnvelope, SessionContract } from "@/lib/auth/types";
import { translateApiError } from "@/lib/i18n/error-codes";
import { formatDateTime } from "@/lib/i18n/format";

export function SessionsList() {
  const router = useRouter();
  const { dictionary, locale } = useTranslation();
  const copy = dictionary.auth.sessions;

  const [sessions, setSessions] = useState<SessionContract[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [revokingId, setRevokingId] = useState<string | null>(null);
  const [revokingAll, setRevokingAll] = useState(false);

  const loadSessions = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const envelope = await apiFetch<ApiEnvelope<SessionContract[]>>("/api/sessions");
      setSessions(envelope.data ?? []);
    } catch (err) {
      const translated = translateApiError(err, dictionary);
      setError(translated.message || copy.loadError);
    } finally {
      setLoading(false);
    }
  }, [copy.loadError, dictionary]);

  useEffect(() => {
    void loadSessions();
  }, [loadSessions]);

  const onRevoke = async (id: string) => {
    setRevokingId(id);
    try {
      await apiFetch(`/api/sessions/${id}`, { method: "DELETE" });
      await loadSessions();
    } catch (err) {
      const translated = translateApiError(err, dictionary);
      setError(translated.message);
    } finally {
      setRevokingId(null);
    }
  };

  const onRevokeAll = async () => {
    setRevokingAll(true);
    try {
      await revokeAllSessions();
      await logout();
      router.replace("/login");
    } catch (err) {
      const translated = translateApiError(err, dictionary);
      setError(translated.message);
      setRevokingAll(false);
    }
  };

  if (loading) {
    return <p className="text-sm text-ink-500 dark:text-gray-400">...</p>;
  }

  return (
    <div className="space-y-6">
      {error ? (
        <p
          role="alert"
          className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700 dark:border-red-900/60 dark:bg-red-900/30 dark:text-red-300"
        >
          {error}
        </p>
      ) : null}

      {sessions.length === 0 ? (
        <p className="text-sm text-ink-500 dark:text-gray-400">{copy.empty}</p>
      ) : (
        <ul className="divide-y divide-divider-200 rounded-xl border border-divider-200 bg-white dark:divide-divider-700 dark:border-divider-700 dark:bg-ink-800">
          {sessions.map((session) => (
            <li
              key={session.id}
              className="flex flex-col gap-3 px-4 py-4 sm:flex-row sm:items-center sm:justify-between"
            >
              <div className="space-y-1">
                <p className="text-sm font-medium text-ink-900 dark:text-white">
                  {session.deviceName ?? copy.device}
                  {session.isCurrent ? (
                    <span className="ml-2 rounded-full bg-blue-100 px-2 py-0.5 text-xs font-semibold text-blue-800 dark:bg-blue-500/20 dark:text-blue-200">
                      {copy.current}
                    </span>
                  ) : null}
                </p>
                <p className="text-xs text-ink-500 dark:text-gray-400">
                  {copy.ip}: {session.ipAddress} · {copy.lastActive}:{" "}
                  {formatDateTime(session.lastActiveAt, locale)}
                </p>
              </div>
              {!session.isCurrent ? (
                <button
                  type="button"
                  disabled={revokingId === session.id}
                  onClick={() => void onRevoke(session.id)}
                  className="rounded-md border border-divider-200 px-3 py-1.5 text-sm font-medium text-red-700 transition hover:bg-red-50 disabled:opacity-60 dark:border-divider-600 dark:text-red-300 dark:hover:bg-red-900/20"
                >
                  {revokingId === session.id ? copy.revoking : copy.revoke}
                </button>
              ) : null}
            </li>
          ))}
        </ul>
      )}

      <div className="flex flex-wrap items-center gap-4">
        <button
          type="button"
          disabled={revokingAll}
          onClick={() => void onRevokeAll()}
          className="rounded-md bg-red-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-red-700 disabled:opacity-60"
        >
          {revokingAll ? copy.revokeAllSubmitting : copy.revokeAll}
        </button>
        <Link
          href="/dashboard"
          className="text-sm text-ink-600 underline-offset-2 hover:underline dark:text-gray-300"
        >
          {copy.backToDashboard}
        </Link>
      </div>
    </div>
  );
}
