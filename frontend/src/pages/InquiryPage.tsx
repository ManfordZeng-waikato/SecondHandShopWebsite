import axios from 'axios';
import { type FormEvent, useCallback, useEffect, useRef, useState } from 'react';
import { useMutation, useQuery } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Divider,
  Snackbar,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import SendIcon from '@mui/icons-material/Send';
import { useNavigate, useParams } from 'react-router-dom';
import { fetchProductById } from '../features/catalog/api/catalogApi';
import { createInquiry } from '../features/inquiry/api/inquiryApi';
import { TurnstileWidget, type TurnstileWidgetHandle } from '../features/inquiry/components/TurnstileWidget';
import { env } from '../shared/config/env';

interface InquiryFormState {
  customerName: string;
  email: string;
  phoneNumber: string;
  message: string;
}

const initialState: InquiryFormState = {
  customerName: '',
  email: '',
  phoneNumber: '',
  message: '',
};

const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const phonePattern = /^[0-9+\-\s()]+$/;

const MAX_NAME_LENGTH = 120;
const MAX_EMAIL_LENGTH = 256;
const MAX_PHONE_LENGTH = 40;
const MAX_MESSAGE_LENGTH = 3000;
const TURNSTILE_EXECUTE_TIMEOUT_MS = 15000;

function isTurnstileRelatedMessage(message: string): boolean {
  const normalizedMessage = message.toLowerCase();
  return normalizedMessage.includes('security verification') || normalizedMessage.includes('turnstile');
}

function getApiErrorMessage(error: unknown): string | null {
  if (!axios.isAxiosError(error)) return null;
  const payload = error.response?.data;
  if (!payload || typeof payload !== 'object') return null;
  if (!('message' in payload)) return null;
  const message = (payload as { message?: unknown }).message;
  return typeof message === 'string' ? message : null;
}

export function InquiryPage() {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const [formState, setFormState] = useState<InquiryFormState>(initialState);
  const [turnstileToken, setTurnstileToken] = useState<string | null>(null);
  const [turnstileResetKey, setTurnstileResetKey] = useState(0);
  const [isTriggeringTurnstile, setIsTriggeringTurnstile] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successOpen, setSuccessOpen] = useState(false);
  const turnstileWidgetRef = useRef<TurnstileWidgetHandle | null>(null);
  const turnstileTokenResolverRef = useRef<((token: string | null) => void) | null>(null);
  const turnstileTokenRequestRef = useRef<Promise<string | null> | null>(null);
  const turnstileTokenTimeoutRef = useRef<number | null>(null);

  const productQuery = useQuery({
    queryKey: ['product', id],
    queryFn: () => fetchProductById(id!),
    enabled: !!id,
  });

  const inquiryMutation = useMutation({
    mutationFn: createInquiry,
    onSuccess: () => {
      setSuccessOpen(true);
      setFormState(initialState);
      setTurnstileToken(null);
      setTurnstileResetKey((prev) => prev + 1);
      setIsTriggeringTurnstile(false);
    },
  });

  const resolvePendingTurnstileTokenRequest = useCallback((token: string | null) => {
    if (turnstileTokenTimeoutRef.current !== null) {
      window.clearTimeout(turnstileTokenTimeoutRef.current);
      turnstileTokenTimeoutRef.current = null;
    }
    const resolver = turnstileTokenResolverRef.current;
    turnstileTokenResolverRef.current = null;
    turnstileTokenRequestRef.current = null;
    resolver?.(token);
  }, []);

  const requestTurnstileToken = useCallback(async (): Promise<string | null> => {
    if (turnstileToken) return turnstileToken;
    if (turnstileTokenRequestRef.current) return turnstileTokenRequestRef.current;

    const pendingTokenRequest = new Promise<string | null>((resolve) => {
      turnstileTokenResolverRef.current = resolve;
      turnstileTokenTimeoutRef.current = window.setTimeout(() => {
        resolvePendingTurnstileTokenRequest(null);
      }, TURNSTILE_EXECUTE_TIMEOUT_MS);
    });

    turnstileTokenRequestRef.current = pendingTokenRequest;
    const executeTriggered = turnstileWidgetRef.current?.execute() ?? false;
    if (!executeTriggered) resolvePendingTurnstileTokenRequest(null);
    return pendingTokenRequest;
  }, [resolvePendingTurnstileTokenRequest, turnstileToken]);

  const handleTurnstileVerify = useCallback((token: string) => {
    setTurnstileToken(token);
    setIsTriggeringTurnstile(false);
    resolvePendingTurnstileTokenRequest(token);
    setError((currentError) =>
      currentError && isTurnstileRelatedMessage(currentError) ? null : currentError);
  }, [resolvePendingTurnstileTokenRequest]);

  const handleTurnstileExpire = useCallback(() => {
    setTurnstileToken(null);
    setIsTriggeringTurnstile(false);
    resolvePendingTurnstileTokenRequest(null);
    setError('Security verification expired. Please verify again.');
  }, [resolvePendingTurnstileTokenRequest]);

  const handleTurnstileError = useCallback(() => {
    setTurnstileToken(null);
    setIsTriggeringTurnstile(false);
    resolvePendingTurnstileTokenRequest(null);
    setError('Security verification failed to load. Please refresh and try again.');
  }, [resolvePendingTurnstileTokenRequest]);

  useEffect(() => () => { resolvePendingTurnstileTokenRequest(null); }, [resolvePendingTurnstileTokenRequest]);

  useEffect(() => {
    if (!successOpen) return;
    const timer = window.setTimeout(() => navigate('/'), 1500);
    return () => window.clearTimeout(timer);
  }, [navigate, successOpen]);

  if (productQuery.isLoading) {
    return <CircularProgress />;
  }

  if (productQuery.isError) {
    return <Alert severity="error">Unable to load product information.</Alert>;
  }

  const targetProduct = productQuery.data;
  const normalizedEmailInput = formState.email.trim();
  const isEmailFormatInvalid = normalizedEmailInput.length > 0 && !emailPattern.test(normalizedEmailInput);
  const normalizedPhoneInput = formState.phoneNumber.trim();
  const isPhoneFormatInvalid = normalizedPhoneInput.length > 0 && !phonePattern.test(normalizedPhoneInput);

  if (!targetProduct) {
    return <Alert severity="warning">Product not found for inquiry.</Alert>;
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    setSuccessOpen(false);

    if (!formState.message.trim()) {
      setError('Message is required.');
      return;
    }
    if (!formState.email.trim() && !formState.phoneNumber.trim()) {
      setError('Please provide at least one contact method: email or phone number.');
      return;
    }
    const normalizedEmail = formState.email.trim();
    if (normalizedEmail && !emailPattern.test(normalizedEmail)) {
      setError('Please enter a valid email address.');
      return;
    }
    const normalizedPhone = formState.phoneNumber.trim();
    if (normalizedPhone && !phonePattern.test(normalizedPhone)) {
      setError('Phone number can only contain digits, +, -, spaces, and parentheses.');
      return;
    }
    if (formState.customerName.length > MAX_NAME_LENGTH) {
      setError(`Name must be ${MAX_NAME_LENGTH} characters or fewer.`);
      return;
    }
    if (normalizedEmail.length > MAX_EMAIL_LENGTH) {
      setError(`Email must be ${MAX_EMAIL_LENGTH} characters or fewer.`);
      return;
    }
    if (normalizedPhone.length > MAX_PHONE_LENGTH) {
      setError(`Phone number must be ${MAX_PHONE_LENGTH} characters or fewer.`);
      return;
    }
    if (formState.message.length > MAX_MESSAGE_LENGTH) {
      setError(`Message must be ${MAX_MESSAGE_LENGTH} characters or fewer.`);
      return;
    }

    let activeTurnstileToken = turnstileToken;
    if (!activeTurnstileToken) {
      setIsTriggeringTurnstile(true);
      activeTurnstileToken = await requestTurnstileToken();
      setIsTriggeringTurnstile(false);
    }
    if (!activeTurnstileToken) {
      setError('Please complete the security verification before submitting.');
      return;
    }

    try {
      await inquiryMutation.mutateAsync({
        productId: targetProduct.id,
        customerName: formState.customerName || undefined,
        email: normalizedEmail || undefined,
        phoneNumber: normalizedPhone || undefined,
        message: formState.message,
        turnstileToken: activeTurnstileToken,
      });
    } catch (submissionError) {
      const apiErrorMessage = getApiErrorMessage(submissionError);
      if (apiErrorMessage) {
        setError(apiErrorMessage);
        if (isTurnstileRelatedMessage(apiErrorMessage)) {
          setTurnstileToken(null);
          setTurnstileResetKey((prev) => prev + 1);
        }
        return;
      }
      setError('Failed to submit inquiry. Please try again.');
    }
  };

  const isSubmitBusy = inquiryMutation.isPending || isTriggeringTurnstile;

  return (
    <Box sx={{ maxWidth: 680, mx: 'auto' }}>
      {/* Header */}
      <Box sx={{ mb: 3 }}>
        <Typography
          component="span"
          sx={{
            display: 'inline-block',
            mb: 1.5,
            letterSpacing: '0.14em',
            textTransform: 'uppercase',
            fontSize: '0.72rem',
            fontWeight: 600,
            color: 'text.secondary',
          }}
        >
          Interested in this item?
        </Typography>
        <Typography
          variant="h3"
          component="h1"
          sx={{ fontSize: { xs: '1.7rem', md: '2.1rem' }, lineHeight: 1.15 }}
        >
          {targetProduct.title}
        </Typography>
      </Box>

      <Divider sx={{ mb: 3 }} />

      {/* Form card */}
      <Box
        sx={{
          bgcolor: '#f0ebe4',
          border: '1px solid',
          borderColor: 'divider',
          borderRadius: 3,
          p: { xs: 2.5, sm: 4 },
        }}
      >
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2.5 }}>
          Leave your contact details below and Pat will be in touch. We require at least one contact method.
        </Typography>

        {error && (
          <Alert severity="error" sx={{ mb: 2, borderRadius: 2 }}>
            {error}
          </Alert>
        )}

        <Stack spacing={2} component="form" onSubmit={handleSubmit}>
          <TextField
            label="Your name"
            value={formState.customerName}
            onChange={(event) => setFormState((prev) => ({ ...prev, customerName: event.target.value }))}
            slotProps={{ htmlInput: { maxLength: MAX_NAME_LENGTH } }}
          />
          <TextField
            label="Email"
            type="email"
            value={formState.email}
            onChange={(event) => setFormState((prev) => ({ ...prev, email: event.target.value }))}
            error={isEmailFormatInvalid}
            helperText={isEmailFormatInvalid ? 'Please enter a valid email address.' : ' '}
            slotProps={{ htmlInput: { maxLength: MAX_EMAIL_LENGTH } }}
          />
          <TextField
            label="Phone number"
            value={formState.phoneNumber}
            onChange={(event) => setFormState((prev) => ({ ...prev, phoneNumber: event.target.value }))}
            error={isPhoneFormatInvalid}
            helperText={isPhoneFormatInvalid ? 'Phone number can only contain digits, +, -, spaces, and parentheses.' : ' '}
            slotProps={{ htmlInput: { maxLength: MAX_PHONE_LENGTH } }}
          />
          <TextField
            label="Message"
            value={formState.message}
            onChange={(event) => setFormState((prev) => ({ ...prev, message: event.target.value }))}
            required
            multiline
            minRows={4}
            slotProps={{ htmlInput: { maxLength: MAX_MESSAGE_LENGTH } }}
            helperText={`${formState.message.length}/${MAX_MESSAGE_LENGTH}`}
          />

          <TurnstileWidget
            ref={turnstileWidgetRef}
            siteKey={env.turnstileSiteKey}
            resetKey={turnstileResetKey}
            language="en"
            executionMode="execute"
            onVerify={handleTurnstileVerify}
            onExpire={handleTurnstileExpire}
            onError={handleTurnstileError}
          />

          <Box>
            <Button
              type="submit"
              variant="contained"
              size="large"
              disabled={isSubmitBusy}
              endIcon={!isSubmitBusy ? <SendIcon /> : undefined}
              sx={{ px: 4, py: 1.5, borderRadius: 2 }}
            >
              {inquiryMutation.isPending
                ? 'Submitting…'
                : isTriggeringTurnstile
                  ? 'Verifying…'
                  : 'Send Inquiry'}
            </Button>
          </Box>
        </Stack>
      </Box>

      <Snackbar
        open={successOpen}
        autoHideDuration={1500}
        anchorOrigin={{ vertical: 'top', horizontal: 'center' }}
        onClose={() => setSuccessOpen(false)}
      >
        <Alert onClose={() => setSuccessOpen(false)} severity="success" sx={{ width: '100%' }}>
          Inquiry submitted successfully.
        </Alert>
      </Snackbar>
    </Box>
  );
}
