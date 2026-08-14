"use client";

import { useCallback, useEffect, useState } from "react";

/**
 * Runs an async producer, tracking loading/error/data with a manual `reload`.
 * `deps` control automatic re-runs (same contract as useEffect deps).
 */
export function useAsync<T>(
  producer: () => Promise<T>,
  deps: React.DependencyList = [],
) {
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const run = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setData(await producer());
    } catch (e) {
      setError(e instanceof Error ? e.message : "Something went wrong");
    } finally {
      setLoading(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);

  useEffect(() => {
    run();
  }, [run]);

  return { data, error, loading, reload: run, setData };
}
