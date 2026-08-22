"use client";

import { useCallback, useEffect, useRef, useState } from "react";

/**
 * Runs an async producer, tracking loading/error/data with a manual `reload`.
 * `deps` control automatic re-runs (same contract as useEffect deps).
 *
 * A call-id + mounted guard prevents stale/superseded responses (rapid refetch,
 * StrictMode double-invoke, or unmount) from committing state.
 */
export function useAsync<T>(
  producer: () => Promise<T>,
  deps: React.DependencyList = [],
) {
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const callIdRef = useRef(0);
  const mountedRef = useRef(true);

  useEffect(() => {
    mountedRef.current = true;
    return () => {
      mountedRef.current = false;
    };
  }, []);

  const run = useCallback(async () => {
    const callId = ++callIdRef.current;
    const isCurrent = () => mountedRef.current && callId === callIdRef.current;
    setLoading(true);
    setError(null);
    try {
      const result = await producer();
      if (isCurrent()) setData(result);
    } catch (e) {
      if (isCurrent()) {
        setError(e instanceof Error ? e.message : "Something went wrong");
      }
    } finally {
      if (isCurrent()) setLoading(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);

  useEffect(() => {
    run();
  }, [run]);

  return { data, error, loading, reload: run, setData };
}
