import { useCallback, useEffect, useRef, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Stack,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from '@mui/material';
import AutoFixHighIcon from '@mui/icons-material/AutoFixHigh';
import PhotoIcon from '@mui/icons-material/Photo';
import ContentCutIcon from '@mui/icons-material/ContentCut';
import { removeBackgroundPreview } from '../api/adminApi';

export type ImageChoice = 'original' | 'cutout';

export interface ImageUploadResult {
  blob: Blob;
  contentType: string;
  fileExtension: string;
  choice: ImageChoice;
}

interface ImageUploadWithPreviewProps {
  file: File;
  onResult: (result: ImageUploadResult) => void;
  onRemove: () => void;
  isPrimary: boolean;
  onSetPrimary: () => void;
  disabled?: boolean;
}

export function ImageUploadWithPreview({
  file,
  onResult,
  onRemove,
  isPrimary,
  onSetPrimary,
  disabled = false,
}: ImageUploadWithPreviewProps) {
  const [originalUrl, setOriginalUrl] = useState<string | null>(null);
  const [cutoutBlob, setCutoutBlob] = useState<Blob | null>(null);
  const [cutoutUrl, setCutoutUrl] = useState<string | null>(null);
  const [choice, setChoice] = useState<ImageChoice>('original');
  const [removeBgLoading, setRemoveBgLoading] = useState(false);
  const [removeBgError, setRemoveBgError] = useState<string | null>(null);
  const cutoutUrlRef = useRef<string | null>(null);

  useEffect(() => {
    const url = URL.createObjectURL(file);
    setOriginalUrl(url);
    setCutoutBlob(null);
    setCutoutUrl((prev) => {
      if (prev) URL.revokeObjectURL(prev);
      cutoutUrlRef.current = null;
      return null;
    });
    setChoice('original');
    setRemoveBgError(null);

    return () => URL.revokeObjectURL(url);
  }, [file]);

  useEffect(() => {
    return () => {
      if (cutoutUrlRef.current) URL.revokeObjectURL(cutoutUrlRef.current);
    };
  }, []);

  const notifyParent = useCallback(
    (currentChoice: ImageChoice, currentCutoutBlob: Blob | null) => {
      if (currentChoice === 'cutout' && currentCutoutBlob) {
        onResult({
          blob: currentCutoutBlob,
          contentType: 'image/png',
          fileExtension: '.png',
          choice: 'cutout',
        });
      } else {
        onResult({
          blob: file,
          contentType: file.type || 'image/jpeg',
          fileExtension: getExtension(file.name),
          choice: 'original',
        });
      }
    },
    [file, onResult],
  );

  useEffect(() => {
    notifyParent(choice, cutoutBlob);
  }, [choice, cutoutBlob, notifyParent]);

  const handleRemoveBg = async () => {
    if (removeBgLoading) return;
    setRemoveBgLoading(true);
    setRemoveBgError(null);

    try {
      const blob = await removeBackgroundPreview(file);
      const url = URL.createObjectURL(blob);

      if (cutoutUrlRef.current) URL.revokeObjectURL(cutoutUrlRef.current);
      cutoutUrlRef.current = url;
      setCutoutBlob(blob);
      setCutoutUrl(url);
      setChoice('cutout');
    } catch (err) {
      const message =
        err instanceof Error ? err.message : 'Background removal failed.';
      setRemoveBgError(message);
    } finally {
      setRemoveBgLoading(false);
    }
  };

  const handleChoiceChange = (_: unknown, value: ImageChoice | null) => {
    if (value) setChoice(value);
  };

  return (
    <Box
      sx={{
        border: '2px solid',
        borderColor: isPrimary ? 'primary.main' : 'divider',
        borderRadius: 2,
        p: 1.5,
        position: 'relative',
        width: 280,
        transition: 'border-color 0.2s',
      }}
    >
      {isPrimary && (
        <Typography
          variant="caption"
          sx={{
            position: 'absolute',
            top: -10,
            left: 12,
            bgcolor: 'primary.main',
            color: 'primary.contrastText',
            px: 1,
            borderRadius: 1,
            fontSize: '0.65rem',
          }}
        >
          Primary
        </Typography>
      )}

      <Stack spacing={1.5} alignItems="center">
        {/* Preview area */}
        <Box sx={{ display: 'flex', gap: 1, width: '100%', justifyContent: 'center' }}>
          <PreviewThumbnail
            label="Original"
            url={originalUrl}
            isSelected={choice === 'original'}
            onClick={() => setChoice('original')}
          />
          {cutoutUrl ? (
            <PreviewThumbnail
              label="No background"
              url={cutoutUrl}
              isSelected={choice === 'cutout'}
              onClick={() => setChoice('cutout')}
              checkered
            />
          ) : (
            <Box
              sx={{
                width: 120,
                height: 120,
                borderRadius: 1,
                border: '1px dashed',
                borderColor: 'divider',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                bgcolor: 'action.hover',
              }}
            >
              <Typography variant="caption" color="text.secondary" textAlign="center" px={1}>
                {removeBgLoading ? <CircularProgress size={24} /> : 'Click below to remove background'}
              </Typography>
            </Box>
          )}
        </Box>

        {removeBgError && (
          <Alert severity="warning" sx={{ width: '100%', py: 0 }}>
            <Typography variant="caption">{removeBgError}</Typography>
          </Alert>
        )}

        {/* Choice toggle */}
        {cutoutUrl && (
          <ToggleButtonGroup
            value={choice}
            exclusive
            onChange={handleChoiceChange}
            size="small"
            fullWidth
            disabled={disabled}
          >
            <ToggleButton value="original">
              <PhotoIcon fontSize="small" sx={{ mr: 0.5 }} />
              Original
            </ToggleButton>
            <ToggleButton value="cutout">
              <ContentCutIcon fontSize="small" sx={{ mr: 0.5 }} />
              No BG
            </ToggleButton>
          </ToggleButtonGroup>
        )}

        {/* Action buttons */}
        <Box sx={{ display: 'flex', gap: 1, width: '100%' }}>
          <Button
            size="small"
            variant="outlined"
            startIcon={removeBgLoading ? <CircularProgress size={14} /> : <AutoFixHighIcon />}
            onClick={handleRemoveBg}
            disabled={disabled || removeBgLoading || !!cutoutUrl}
            fullWidth
          >
            {removeBgLoading ? 'Processing...' : cutoutUrl ? 'Done' : 'Remove BG'}
          </Button>
          {!isPrimary && (
            <Button size="small" variant="text" onClick={onSetPrimary} disabled={disabled}>
              Set primary
            </Button>
          )}
        </Box>

        <Button
          size="small"
          color="error"
          onClick={onRemove}
          disabled={disabled}
          sx={{ alignSelf: 'flex-end' }}
        >
          Remove
        </Button>
      </Stack>
    </Box>
  );
}

function PreviewThumbnail({
  label,
  url,
  isSelected,
  onClick,
  checkered = false,
}: {
  label: string;
  url: string | null;
  isSelected: boolean;
  onClick: () => void;
  checkered?: boolean;
}) {
  return (
    <Box
      onClick={onClick}
      sx={{
        width: 120,
        height: 120,
        borderRadius: 1,
        overflow: 'hidden',
        border: '2px solid',
        borderColor: isSelected ? 'primary.main' : 'transparent',
        cursor: 'pointer',
        position: 'relative',
        transition: 'border-color 0.2s',
        ...(checkered && {
          backgroundImage:
            'linear-gradient(45deg, #e0e0e0 25%, transparent 25%), linear-gradient(-45deg, #e0e0e0 25%, transparent 25%), linear-gradient(45deg, transparent 75%, #e0e0e0 75%), linear-gradient(-45deg, transparent 75%, #e0e0e0 75%)',
          backgroundSize: '16px 16px',
          backgroundPosition: '0 0, 0 8px, 8px -8px, -8px 0px',
        }),
      }}
    >
      {url && (
        <Box
          component="img"
          src={url}
          alt={label}
          sx={{ width: '100%', height: '100%', objectFit: 'contain' }}
        />
      )}
      <Typography
        variant="caption"
        sx={{
          position: 'absolute',
          bottom: 0,
          left: 0,
          right: 0,
          bgcolor: isSelected ? 'primary.main' : 'rgba(0,0,0,0.5)',
          color: '#fff',
          textAlign: 'center',
          fontSize: '0.6rem',
          py: 0.2,
        }}
      >
        {label}
      </Typography>
    </Box>
  );
}

function getExtension(fileName: string): string {
  const dot = fileName.lastIndexOf('.');
  return dot >= 0 ? fileName.slice(dot).toLowerCase() : '.jpg';
}
