import type { SalesTrendPoint } from './types';

/**
 * Fill missing months in the sales trend with zero rows so the line chart draws a continuous
 * x-axis instead of silently skipping empty months.
 *
 * - For bounded ranges (7d/30d/90d/12m) we use the response's rangeStartUtc/rangeEndUtc as
 *   the inclusive month window, snapping each end to the first of its month.
 * - For AllTime there is no start (rangeStartUtc is null), so we use the earliest returned
 *   point as the start. If the backend returned zero points under AllTime, we bail out
 *   and render nothing — there is genuinely no data to frame.
 */
export function fillMissingMonths(
  points: SalesTrendPoint[],
  rangeStartUtc: string | null,
  rangeEndUtc: string,
): SalesTrendPoint[] {
  const endDate = startOfUtcMonth(new Date(rangeEndUtc));

  let startDate: Date;
  if (rangeStartUtc) {
    startDate = startOfUtcMonth(new Date(rangeStartUtc));
  } else if (points.length > 0) {
    startDate = startOfUtcMonth(new Date(points[0].monthStartUtc));
  } else {
    return [];
  }

  if (startDate.getTime() > endDate.getTime()) {
    return points;
  }

  const byKey = new Map<string, SalesTrendPoint>();
  for (const p of points) {
    byKey.set(monthKey(new Date(p.monthStartUtc)), p);
  }

  const result: SalesTrendPoint[] = [];
  const cursor = new Date(startDate);
  while (cursor.getTime() <= endDate.getTime()) {
    const key = monthKey(cursor);
    const existing = byKey.get(key);
    if (existing) {
      result.push(existing);
    } else {
      result.push({
        monthStartUtc: cursor.toISOString(),
        soldCount: 0,
        revenue: 0,
      });
    }
    cursor.setUTCMonth(cursor.getUTCMonth() + 1);
  }

  return result;
}

function startOfUtcMonth(date: Date): Date {
  return new Date(Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), 1));
}

function monthKey(date: Date): string {
  return `${date.getUTCFullYear()}-${date.getUTCMonth()}`;
}
