import { type FormEvent, useEffect, useState } from 'react';
import { useMutation, useQuery } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Paper,
  Snackbar,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useNavigate, useParams } from 'react-router-dom';
import { fetchProducts } from '../features/catalog/api/catalogApi';
import { createInquiry } from '../features/inquiry/api/inquiryApi';

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

export function InquiryPage() {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const [formState, setFormState] = useState<InquiryFormState>(initialState);
  const [error, setError] = useState<string | null>(null);
  const [successOpen, setSuccessOpen] = useState(false);

  const productsQuery = useQuery({
    queryKey: ['products'],
    queryFn: () => fetchProducts(),
  });

  const inquiryMutation = useMutation({
    mutationFn: createInquiry,
    onSuccess: () => {
      setSuccessOpen(true);
      setFormState(initialState);
    },
  });

  useEffect(() => {
    if (!successOpen) {
      return;
    }

    const timer = window.setTimeout(() => {
      navigate('/');
    }, 1500);

    return () => {
      window.clearTimeout(timer);
    };
  }, [navigate, successOpen]);

  if (productsQuery.isLoading) {
    return <CircularProgress />;
  }

  if (productsQuery.isError) {
    return <Alert severity="error">Unable to load product information.</Alert>;
  }

  const products = productsQuery.data ?? [];
  const targetProduct = products.find((product) => product.id === id);
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

    try {
      await inquiryMutation.mutateAsync({
        productId: targetProduct.id,
        customerName: formState.customerName || undefined,
        email: normalizedEmail || undefined,
        phoneNumber: formState.phoneNumber || undefined,
        message: formState.message,
      });
    } catch {
      setError('Failed to submit inquiry. Please try again.');
    }
  };

  return (
    <Paper sx={{ p: 3, maxWidth: 700 }}>
      <Stack spacing={2} component="form" onSubmit={handleSubmit}>
        <Typography variant="h5">Inquiry for {targetProduct.title}</Typography>
        <Typography variant="body2" color="text.secondary">
          Please leave your contact details. We require at least one contact method.
        </Typography>
        {error && <Alert severity="error">{error}</Alert>}
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
        <Box>
          <Button type="submit" variant="contained" disabled={inquiryMutation.isPending}>
            {inquiryMutation.isPending ? 'Submitting...' : 'Submit inquiry'}
          </Button>
        </Box>
      </Stack>
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
    </Paper>
  );
}
