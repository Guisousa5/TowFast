const MIN_TRACKING_INTERVAL_MS = 5_000;
const DEFAULT_TRACKING_INTERVAL_MS = 10_000;
const TRACKABLE_STATUSES = new Set(['EnRoute', 'InProgress']);

export function trackingIntervalMs(configuredInterval: number): number {
  if (!Number.isFinite(configuredInterval) || configuredInterval <= 0) {
    return DEFAULT_TRACKING_INTERVAL_MS;
  }

  return Math.max(MIN_TRACKING_INTERVAL_MS, configuredInterval);
}

export function canTrackStatus(status: string | null | undefined): boolean {
  return TRACKABLE_STATUSES.has(status ?? '');
}