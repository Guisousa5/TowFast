import { describe, expect, it } from 'vitest';
import { canTrackStatus, trackingIntervalMs } from './tracking-policy';

describe('tracking policy', () => {
  it('only enables tracking during the active route and service states', () => {
    expect(canTrackStatus('EnRoute')).toBe(true);
    expect(canTrackStatus('InProgress')).toBe(true);
    expect(canTrackStatus('Accepted')).toBe(false);
    expect(canTrackStatus('Completed')).toBe(false);
  });

  it('uses a safe interval for invalid or overly frequent configurations', () => {
    expect(trackingIntervalMs(0)).toBe(10_000);
    expect(trackingIntervalMs(Number.NaN)).toBe(10_000);
    expect(trackingIntervalMs(1_000)).toBe(5_000);
    expect(trackingIntervalMs(15_000)).toBe(15_000);
  });
});