/* eslint-disable react-hooks/set-state-in-effect */
import { useEffect, useState } from 'react';
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Link,
  Stack,
  TextField,
} from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';
import type {
  CreateCustomerInput,
  CustomerConflictDetail,
} from '../../../entities/customer/types';

interface CustomerCreateDialogProps {
  open: boolean;
  isSubmitting: boolean;
  errorMessage: string | null;
  conflict: CustomerConflictDetail | null;
  onClose: () => void;
  onSubmit: (input: CreateCustomerInput) => Promise<void>;
}

const PHONE_REGEX = /^[0-9+\-\s()]+$/;
const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const NAME_MAX_LENGTH = 120;
const EMAIL_MAX_LENGTH = 254;
const PHONE_MAX_LENGTH = 40;
const NOTES_MAX_LENGTH = 2000;

export function CustomerCreateDialog({
  open,
  isSubmitting,
  errorMessage,
  conflict,
  onClose,
  onSubmit,
}: CustomerCreateDialogProps) {
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [notes, setNotes] = useState('');
  const [validationError, setValidationError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) {
      return;
    }
    setName('');
    setEmail('');
    setPhone('');
    setNotes('');
    setValidationError(null);
  }, [open]);

  const validate = (): string | null => {
    if (name.trim().length > NAME_MAX_LENGTH) {
      return `Name must be ${NAME_MAX_LENGTH} characters or fewer.`;
    }

    const trimmedEmail = email.trim();
    if (trimmedEmail.length > EMAIL_MAX_LENGTH) {
      return `Email must be ${EMAIL_MAX_LENGTH} characters or fewer.`;
    }
    if (trimmedEmail.length > 0 && !EMAIL_REGEX.test(trimmedEmail)) {
      return 'Please enter a valid email address.';
    }

    const trimmedPhone = phone.trim();
    if (trimmedPhone.length > PHONE_MAX_LENGTH) {
      return `Phone must be ${PHONE_MAX_LENGTH} characters or fewer.`;
    }
    if (trimmedPhone.length > 0 && !PHONE_REGEX.test(trimmedPhone)) {
      return 'Phone number can only contain digits, +, -, spaces, and parentheses.';
    }

    if (trimmedEmail.length === 0 && trimmedPhone.length === 0) {
      return 'At least one contact method (email or phone) is required.';
    }

    if (notes.trim().length > NOTES_MAX_LENGTH) {
      return `Notes must be ${NOTES_MAX_LENGTH} characters or fewer.`;
    }

    return null;
  };

  const handleSubmit = async () => {
    const error = validate();
    if (error) {
      setValidationError(error);
      return;
    }

    setValidationError(null);
    try {
      await onSubmit({
        name: name.trim() || undefined,
        email: email.trim() || undefined,
        phoneNumber: phone.trim() || undefined,
        notes: notes.trim() || undefined,
      });
    } catch {
      // Parent handles server errors / conflicts.
    }
  };

  const conflictFieldLabel =
    conflict?.conflictField === 'email' ? 'email' : 'phone number';

  return (
    <Dialog open={open} onClose={isSubmitting ? undefined : onClose} fullWidth maxWidth="sm">
      <DialogTitle>Add customer</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {validationError && <Alert severity="error">{validationError}</Alert>}
          {conflict && (
            <Alert severity="warning">
              A customer with this {conflictFieldLabel} already exists.{' '}
              <Link
                component={RouterLink}
                to={`/lord/customers/${conflict.existingCustomerId}`}
                onClick={onClose}
              >
                Open existing customer
              </Link>
            </Alert>
          )}
          {errorMessage && !conflict && <Alert severity="error">{errorMessage}</Alert>}
          <TextField
            label="Name"
            value={name}
            onChange={(event) => setName(event.target.value)}
            disabled={isSubmitting}
            inputProps={{ maxLength: NAME_MAX_LENGTH }}
          />
          <TextField
            label="Email"
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            disabled={isSubmitting}
            inputProps={{ maxLength: EMAIL_MAX_LENGTH }}
            helperText="Email or phone is required."
          />
          <TextField
            label="Phone"
            value={phone}
            onChange={(event) => setPhone(event.target.value)}
            disabled={isSubmitting}
            inputProps={{ maxLength: PHONE_MAX_LENGTH }}
          />
          <TextField
            label="Notes"
            value={notes}
            onChange={(event) => setNotes(event.target.value)}
            multiline
            minRows={3}
            disabled={isSubmitting}
            inputProps={{ maxLength: NOTES_MAX_LENGTH }}
            helperText={`Up to ${NOTES_MAX_LENGTH} characters`}
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={isSubmitting}>
          Cancel
        </Button>
        <Button onClick={handleSubmit} variant="contained" disabled={isSubmitting}>
          {isSubmitting ? 'Creating...' : 'Create'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
