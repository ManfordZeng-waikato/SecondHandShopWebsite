import { useEffect, useState } from 'react';
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
} from '@mui/material';
import type { EditableCustomer, UpdateCustomerInput } from '../../../entities/customer/types';

interface CustomerEditDialogProps {
  open: boolean;
  customer: EditableCustomer | null;
  isSubmitting: boolean;
  errorMessage: string | null;
  onClose: () => void;
  onSubmit: (input: UpdateCustomerInput) => Promise<void>;
}

const PHONE_REGEX = /^[0-9+\-\s()]+$/;
const NAME_MAX_LENGTH = 120;
const PHONE_MAX_LENGTH = 40;
const NOTES_MAX_LENGTH = 2000;

export function CustomerEditDialog({
  open,
  customer,
  isSubmitting,
  errorMessage,
  onClose,
  onSubmit,
}: CustomerEditDialogProps) {
  const [name, setName] = useState('');
  const [phone, setPhone] = useState('');
  const [notes, setNotes] = useState('');
  const [validationError, setValidationError] = useState<string | null>(null);

  useEffect(() => {
    if (!open || !customer) {
      return;
    }

    setName(customer.name);
    setPhone(customer.phone);
    setNotes(customer.notes);
    setValidationError(null);
  }, [open, customer]);

  const validate = (): string | null => {
    if (name.trim().length > NAME_MAX_LENGTH) {
      return `Name must be ${NAME_MAX_LENGTH} characters or fewer.`;
    }

    const trimmedPhone = phone.trim();
    if (trimmedPhone.length > PHONE_MAX_LENGTH) {
      return `Phone must be ${PHONE_MAX_LENGTH} characters or fewer.`;
    }

    if (trimmedPhone.length > 0 && !PHONE_REGEX.test(trimmedPhone)) {
      return 'Phone number can only contain digits, +, -, spaces, and parentheses.';
    }

    const email = customer?.email.trim() ?? '';
    if (email.length === 0 && trimmedPhone.length === 0) {
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
        phoneNumber: phone.trim() || undefined,
        notes: notes.trim(),
      });
    } catch {
      // Parent component handles server error feedback.
    }
  };

  return (
    <Dialog open={open} onClose={isSubmitting ? undefined : onClose} fullWidth maxWidth="sm">
      <DialogTitle>Edit customer</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {validationError && <Alert severity="error">{validationError}</Alert>}
          {errorMessage && <Alert severity="error">{errorMessage}</Alert>}
          <TextField
            label="Name"
            value={name}
            onChange={(event) => setName(event.target.value)}
            disabled={isSubmitting}
            inputProps={{ maxLength: NAME_MAX_LENGTH }}
          />
          <TextField
            label="Email"
            value={customer?.email ?? ''}
            disabled
            helperText="Email is read-only to avoid identity mismatches."
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
        <Button onClick={handleSubmit} variant="contained" disabled={isSubmitting || !customer}>
          {isSubmitting ? 'Saving...' : 'Save'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
