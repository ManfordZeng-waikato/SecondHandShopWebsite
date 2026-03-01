import { useCallback, useEffect, useRef, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Fade,
  IconButton,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import AutoFixHighIcon from '@mui/icons-material/AutoFixHigh';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import CloseIcon from '@mui/icons-material/Close';
import StarIcon from '@mui/icons-material/Star';
import StarBorderIcon from '@mui/icons-material/StarBorder';
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

const CHECKERED_BG = {
  backgroundImage:
    'linear-gradient(45deg, #ccc 25%, transparent 25%), linear-gradient(-45deg, #ccc 25%, transparent 25%), linear-gradient(45deg, transparent 75%, #ccc 75%), linear-gradient(-45deg, transparent 75%, #ccc 75%)',
  backgroundSize: '12px 12px',
  backgroundPosition: '0 0, 0 6px, 6px -6px, -6px 0px',
} as const;

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

  const hasCutout = !!cutoutUrl;
  const activeUrl = choice === 'cutout' && cutoutUrl ? cutoutUrl : originalUrl;

  return (
    <Box
      sx={{
        borderRadius: 3,
        overflow: 'hidden',
        width: 260,
        bgcolor: 'background.paper',
        boxShadow: isPrimary ? 4 : 1,
        border: '2px solid',
        borderColor: isPrimary ? 'primary.main' : 'transparent',
        transition: 'box-shadow 0.3s, border-color 0.3s',
        '&:hover': { boxShadow: 3 },
      }}
    >
      {/* Main preview */}
      <Box
        sx={{
          position: 'relative',
          width: '100%',
          height: 200,
          bgcolor: '#f5f5f5',
          ...(choice === 'cutout' && hasCutout ? CHECKERED_BG : {}),
        }}
      >
        {activeUrl && (
          <Fade in>
            <Box
              component="img"
              src={activeUrl}
              alt={file.name}
              sx={{
                width: '100%',
                height: '100%',
                objectFit: 'contain',
                display: 'block',
              }}
            />
          </Fade>
        )}

        {/* Top-left: primary badge */}
        {isPrimary && (
          <Chip
            icon={<StarIcon sx={{ fontSize: 14 }} />}
            label="Primary"
            size="small"
            color="primary"
            sx={{
              position: 'absolute',
              top: 8,
              left: 8,
              fontWeight: 600,
              fontSize: '0.7rem',
              height: 24,
            }}
          />
        )}

        {/* Top-right: remove button */}
        <Tooltip title="Remove this image">
          <IconButton
            size="small"
            onClick={onRemove}
            disabled={disabled}
            sx={{
              position: 'absolute',
              top: 4,
              right: 4,
              bgcolor: 'rgba(0,0,0,0.5)',
              color: '#fff',
              '&:hover': { bgcolor: 'error.main' },
              width: 28,
              height: 28,
            }}
          >
            <CloseIcon sx={{ fontSize: 16 }} />
          </IconButton>
        </Tooltip>

        {/* Bottom: current version indicator */}
        <Box
          sx={{
            position: 'absolute',
            bottom: 0,
            left: 0,
            right: 0,
            background: 'linear-gradient(transparent, rgba(0,0,0,0.6))',
            px: 1.5,
            py: 0.75,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
          }}
        >
          <Typography variant="caption" sx={{ color: '#fff', fontWeight: 500 }}>
            {choice === 'cutout' ? 'Background removed' : 'Original'}
          </Typography>
          <CheckCircleIcon sx={{ color: '#4caf50', fontSize: 16 }} />
        </Box>

        {/* Loading overlay */}
        {removeBgLoading && (
          <Box
            sx={{
              position: 'absolute',
              inset: 0,
              bgcolor: 'rgba(255,255,255,0.85)',
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              justifyContent: 'center',
              gap: 1,
            }}
          >
            <CircularProgress size={32} />
            <Typography variant="caption" color="text.secondary">
              Removing background...
            </Typography>
          </Box>
        )}
      </Box>

      {/* Controls area */}
      <Stack spacing={1} sx={{ p: 1.5 }}>
        {/* Error message */}
        {removeBgError && (
          <Alert severity="warning" sx={{ py: 0, '& .MuiAlert-message': { fontSize: '0.75rem' } }}>
            {removeBgError}
          </Alert>
        )}

        {/* Version selector (appears after background removal) */}
        {hasCutout && (
          <Box sx={{ display: 'flex', gap: 0.75 }}>
            <VersionOption
              label="Original"
              thumbnailUrl={originalUrl}
              isSelected={choice === 'original'}
              onClick={() => setChoice('original')}
              disabled={disabled}
            />
            <VersionOption
              label="No BG"
              thumbnailUrl={cutoutUrl}
              isSelected={choice === 'cutout'}
              onClick={() => setChoice('cutout')}
              disabled={disabled}
              checkered
            />
          </Box>
        )}

        {/* Action buttons */}
        <Box sx={{ display: 'flex', gap: 0.75 }}>
          {!hasCutout && (
            <Button
              size="small"
              variant="outlined"
              startIcon={<AutoFixHighIcon />}
              onClick={handleRemoveBg}
              disabled={disabled || removeBgLoading}
              fullWidth
              sx={{ textTransform: 'none', fontWeight: 500 }}
            >
              Remove background
            </Button>
          )}

          {!isPrimary && (
            <Tooltip title="Set as the main display image">
              <Button
                size="small"
                variant="text"
                startIcon={<StarBorderIcon />}
                onClick={onSetPrimary}
                disabled={disabled}
                fullWidth={hasCutout}
                sx={{ textTransform: 'none', minWidth: 'auto', color: 'text.secondary' }}
              >
                Set as primary
              </Button>
            </Tooltip>
          )}
        </Box>
      </Stack>
    </Box>
  );
}

function VersionOption({
  label,
  thumbnailUrl,
  isSelected,
  onClick,
  disabled = false,
  checkered = false,
}: {
  label: string;
  thumbnailUrl: string | null;
  isSelected: boolean;
  onClick: () => void;
  disabled?: boolean;
  checkered?: boolean;
}) {
  return (
    <Box
      onClick={disabled ? undefined : onClick}
      sx={{
        flex: 1,
        display: 'flex',
        alignItems: 'center',
        gap: 0.75,
        p: 0.75,
        borderRadius: 1.5,
        border: '2px solid',
        borderColor: isSelected ? 'primary.main' : 'divider',
        bgcolor: isSelected ? 'primary.50' : 'transparent',
        cursor: disabled ? 'default' : 'pointer',
        transition: 'all 0.2s',
        opacity: disabled ? 0.5 : 1,
        '&:hover': disabled
          ? {}
          : {
              borderColor: isSelected ? 'primary.main' : 'primary.light',
              bgcolor: isSelected ? 'primary.50' : 'action.hover',
            },
      }}
    >
      <Box
        sx={{
          width: 36,
          height: 36,
          borderRadius: 1,
          overflow: 'hidden',
          flexShrink: 0,
          bgcolor: '#f0f0f0',
          ...(checkered ? CHECKERED_BG : {}),
        }}
      >
        {thumbnailUrl && (
          <Box
            component="img"
            src={thumbnailUrl}
            alt={label}
            sx={{ width: '100%', height: '100%', objectFit: 'contain' }}
          />
        )}
      </Box>
      <Stack spacing={0} sx={{ minWidth: 0 }}>
        <Typography
          variant="caption"
          sx={{
            fontWeight: isSelected ? 600 : 400,
            color: isSelected ? 'primary.main' : 'text.secondary',
            lineHeight: 1.2,
          }}
        >
          {label}
        </Typography>
        {isSelected && (
          <Typography variant="caption" sx={{ fontSize: '0.6rem', color: 'success.main', lineHeight: 1.2 }}>
            Selected
          </Typography>
        )}
      </Stack>
    </Box>
  );
}

function getExtension(fileName: string): string {
  const dot = fileName.lastIndexOf('.');
  return dot >= 0 ? fileName.slice(dot).toLowerCase() : '.jpg';
}
