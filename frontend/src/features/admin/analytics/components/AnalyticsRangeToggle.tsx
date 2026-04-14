import { ToggleButton, ToggleButtonGroup } from '@mui/material';
import { ANALYTICS_RANGE_OPTIONS, type AnalyticsRangeKey } from '../types';

interface Props {
  value: AnalyticsRangeKey;
  onChange: (next: AnalyticsRangeKey) => void;
  disabled?: boolean;
}

export function AnalyticsRangeToggle({ value, onChange, disabled }: Props) {
  return (
    <ToggleButtonGroup
      exclusive
      size="small"
      color="primary"
      value={value}
      disabled={disabled}
      onChange={(_, next) => {
        if (next !== null) {
          onChange(next as AnalyticsRangeKey);
        }
      }}
      aria-label="Analytics date range"
    >
      {ANALYTICS_RANGE_OPTIONS.map((opt) => (
        <ToggleButton key={opt.key} value={opt.key}>
          {opt.label}
        </ToggleButton>
      ))}
    </ToggleButtonGroup>
  );
}
