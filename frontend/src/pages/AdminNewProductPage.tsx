import { type FormEvent, useState } from 'react';
import { useMutation, useQuery } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useNavigate } from 'react-router-dom';
import type { ProductCondition } from '../entities/product/types';
import { createProduct } from '../features/admin/api/adminApi';
import { fetchCategories } from '../features/catalog/api/catalogApi';

interface NewProductFormState {
  title: string;
  slug: string;
  description: string;
  price: string;
  condition: ProductCondition;
  categoryId: string;
}

const initialFormState: NewProductFormState = {
  title: '',
  slug: '',
  description: '',
  price: '',
  condition: 'Good',
  categoryId: '',
};

const conditionOptions: ProductCondition[] = ['LikeNew', 'Good', 'Fair', 'NeedsRepair'];

export function AdminNewProductPage() {
  const navigate = useNavigate();
  const [formState, setFormState] = useState<NewProductFormState>(initialFormState);
  const [error, setError] = useState<string | null>(null);

  const categoriesQuery = useQuery({
    queryKey: ['categories'],
    queryFn: fetchCategories,
  });

  const createProductMutation = useMutation({
    mutationFn: createProduct,
    onSuccess: () => {
      navigate('/admin/products');
    },
  });

  if (categoriesQuery.isLoading) {
    return <CircularProgress />;
  }

  if (categoriesQuery.isError) {
    return <Alert severity="error">Unable to load categories.</Alert>;
  }

  const categories = categoriesQuery.data ?? [];

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);

    const price = Number(formState.price);
    if (!formState.title.trim() || !formState.slug.trim() || !formState.description.trim()) {
      setError('Title, slug and description are required.');
      return;
    }

    if (!formState.categoryId) {
      setError('Please select a category.');
      return;
    }

    if (!Number.isFinite(price) || price <= 0) {
      setError('Price must be a positive number.');
      return;
    }

    try {
      await createProductMutation.mutateAsync({
        title: formState.title.trim(),
        slug: formState.slug.trim(),
        description: formState.description.trim(),
        price,
        condition: formState.condition,
        categoryId: formState.categoryId,
      });
    } catch {
      setError('Failed to create product. Please try again.');
    }
  };

  return (
    <Paper sx={{ p: 3, maxWidth: 700 }}>
      <Stack spacing={2} component="form" onSubmit={handleSubmit}>
        <Typography variant="h5">Create new product</Typography>
        {error && <Alert severity="error">{error}</Alert>}
        <TextField
          label="Title"
          value={formState.title}
          onChange={(event) => setFormState((prev) => ({ ...prev, title: event.target.value }))}
        />
        <TextField
          label="Slug"
          value={formState.slug}
          onChange={(event) => setFormState((prev) => ({ ...prev, slug: event.target.value }))}
        />
        <TextField
          label="Description"
          value={formState.description}
          onChange={(event) => setFormState((prev) => ({ ...prev, description: event.target.value }))}
          multiline
          minRows={3}
        />
        <TextField
          label="Price"
          type="number"
          value={formState.price}
          onChange={(event) => setFormState((prev) => ({ ...prev, price: event.target.value }))}
        />
        <FormControl>
          <InputLabel id="condition-select-label">Condition</InputLabel>
          <Select
            labelId="condition-select-label"
            value={formState.condition}
            label="Condition"
            onChange={(event) =>
              setFormState((prev) => ({ ...prev, condition: event.target.value as ProductCondition }))
            }
          >
            {conditionOptions.map((condition) => (
              <MenuItem key={condition} value={condition}>
                {condition}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <FormControl>
          <InputLabel id="category-select-label">Category</InputLabel>
          <Select
            labelId="category-select-label"
            value={formState.categoryId}
            label="Category"
            onChange={(event) => setFormState((prev) => ({ ...prev, categoryId: event.target.value }))}
          >
            {categories.map((category) => (
              <MenuItem key={category.id} value={category.id}>
                {category.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <Box>
          <Button type="submit" variant="contained" disabled={createProductMutation.isPending}>
            {createProductMutation.isPending ? 'Creating...' : 'Create product'}
          </Button>
        </Box>
      </Stack>
    </Paper>
  );
}
