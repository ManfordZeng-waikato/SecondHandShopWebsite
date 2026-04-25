import { fillMissingMonths } from '../fillTrend';
import {
  formatCurrency,
  formatCurrencyPrecise,
  formatInt,
  formatMonthLabel,
  formatPercent,
} from '../format';

describe('analytics helpers', () => {
  it('fills missing bounded range months with zero values', () => {
    const result = fillMissingMonths(
      [{ monthStartUtc: '2026-02-01T00:00:00.000Z', soldCount: 2, revenue: 150 }],
      '2026-01-15T00:00:00.000Z',
      '2026-03-20T00:00:00.000Z',
    );

    expect(result).toEqual([
      { monthStartUtc: '2026-01-01T00:00:00.000Z', soldCount: 0, revenue: 0 },
      { monthStartUtc: '2026-02-01T00:00:00.000Z', soldCount: 2, revenue: 150 },
      { monthStartUtc: '2026-03-01T00:00:00.000Z', soldCount: 0, revenue: 0 },
    ]);
  });

  it('uses the first point as all-time start and returns empty all-time data unchanged', () => {
    expect(fillMissingMonths([], null, '2026-03-20T00:00:00.000Z')).toEqual([]);

    expect(
      fillMissingMonths(
        [{ monthStartUtc: '2026-02-01T00:00:00.000Z', soldCount: 1, revenue: 80 }],
        null,
        '2026-03-20T00:00:00.000Z',
      ),
    ).toEqual([
      { monthStartUtc: '2026-02-01T00:00:00.000Z', soldCount: 1, revenue: 80 },
      { monthStartUtc: '2026-03-01T00:00:00.000Z', soldCount: 0, revenue: 0 },
    ]);
  });

  it('returns original points when the start range is after the end range', () => {
    const points = [{ monthStartUtc: '2026-04-01T00:00:00.000Z', soldCount: 1, revenue: 80 }];

    expect(fillMissingMonths(points, '2026-05-01T00:00:00.000Z', '2026-04-01T00:00:00.000Z')).toBe(points);
  });

  it('formats analytics labels for display', () => {
    expect(formatCurrency(1234)).toBe('$1,234');
    expect(formatCurrencyPrecise(12.5)).toBe('$12.50');
    expect(formatInt(1234)).toBe('1,234');
    expect(formatPercent(0.123)).toBe('12.3%');
    expect(formatMonthLabel('2026-04-01T00:00:00.000Z')).toBe('Apr 26');
  });
});
