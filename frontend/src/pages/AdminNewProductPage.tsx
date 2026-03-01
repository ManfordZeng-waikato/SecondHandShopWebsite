import { type FormEvent, useCallback, useRef, useState } from 'react';
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
import {
  addProductImage,
  createProduct,
  createProductImageUploadUrl,
  uploadBlobToR2,
  uploadImageToR2,
} from '../features/admin/api/adminApi';
import { fetchCategories } from '../features/catalog/api/catalogApi';
import {
  ImageUploadWithPreview,
  type ImageUploadResult,
} from '../features/admin/components/ImageUploadWithPreview';

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
const maxImagesPerProduct = 5;

const SLUG_REGEX = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;

function toSlug(text: string): string {
  return text
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[^a-z0-9\s-]/g, '')
    .trim()
    .replace(/[\s_]+/g, '-')
    .replace(/-+/g, '-')
    .replace(/^-|-$/g, '');
}

function isValidSlug(slug: string): boolean {
  return slug === '' || SLUG_REGEX.test(slug);
}

export function AdminNewProductPage() {
  const navigate = useNavigate();
  const [formState, setFormState] = useState<NewProductFormState>(initialFormState);
  const [slugManuallyEdited, setSlugManuallyEdited] = useState(false);
  const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
  const [primaryIndex, setPrimaryIndex] = useState(0);
  const [uploadProgress, setUploadProgress] = useState<{ uploaded: number; total: number } | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const imageResultsRef = useRef<Map<number, ImageUploadResult>>(new Map());

  const categoriesQuery = useQuery({
    queryKey: ['categories'],
    queryFn: fetchCategories,
  });

  const createProductMutation = useMutation({
    mutationFn: createProduct,
  });

  const handleImageResult = useCallback((index: number, result: ImageUploadResult) => {
    imageResultsRef.current.set(index, result);
  }, []);

  if (categoriesQuery.isLoading) {
    return <CircularProgress />;
  }

  if (categoriesQuery.isError) {
    return <Alert severity="error">Unable to load categories.</Alert>;
  }

  const categories = categoriesQuery.data ?? [];

  const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(event.target.files ?? []).slice(0, maxImagesPerProduct);
    setSelectedFiles(files);
    setPrimaryIndex(0);
    imageResultsRef.current.clear();
    setError(null);
  };

  const handleRemoveFile = (index: number) => {
    setSelectedFiles((prev) => {
      const next = prev.filter((_, i) => i !== index);
      imageResultsRef.current.delete(index);
      const rebuilt = new Map<number, ImageUploadResult>();
      let newIdx = 0;
      for (let i = 0; i < prev.length; i++) {
        if (i === index) continue;
        const existing = imageResultsRef.current.get(i);
        if (existing) rebuilt.set(newIdx, existing);
        newIdx++;
      }
      imageResultsRef.current = rebuilt;
      return next;
    });
    setPrimaryIndex((prev) => {
      if (index < prev) return prev - 1;
      if (index === prev) return 0;
      return prev;
    });
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    setUploadProgress(null);

    const price = Number(formState.price);
    if (!formState.title.trim() || !formState.slug.trim() || !formState.description.trim()) {
      setError('Title, slug and description are required.');
      return;
    }

    if (!isValidSlug(formState.slug.trim())) {
      setError('Slug can only contain lowercase letters, numbers, and hyphens (e.g. "vintage-leather-bag").');
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

    if (selectedFiles.length > maxImagesPerProduct) {
      setError(`You can upload up to ${maxImagesPerProduct} images for a product.`);
      return;
    }

    setSubmitting(true);

    try {
      const createdProduct = await createProductMutation.mutateAsync({
        title: formState.title.trim(),
        slug: formState.slug.trim(),
        description: formState.description.trim(),
        price,
        condition: formState.condition,
        categoryId: formState.categoryId,
      });

      if (selectedFiles.length > 0) {
        setUploadProgress({ uploaded: 0, total: selectedFiles.length });

        for (let index = 0; index < selectedFiles.length; index += 1) {
          const file = selectedFiles[index];
          const imageResult = imageResultsRef.current.get(index);

          try {
            const useCutout = imageResult?.choice === 'cutout' && imageResult.blob;
            const contentType = useCutout ? 'image/png' : (file.type || 'image/jpeg');
            const fileName = useCutout
              ? `${file.name.replace(/\.[^.]+$/, '')}-nobg.png`
              : file.name;

            const presigned = await createProductImageUploadUrl(
              createdProduct.id,
              fileName,
              contentType,
            );

            if (useCutout) {
              await uploadBlobToR2(presigned.putUrl, imageResult!.blob, contentType);
            } else {
              await uploadImageToR2(presigned.putUrl, file);
            }

            await addProductImage(createdProduct.id, {
              objectKey: presigned.objectKey,
              altText: formState.title.trim(),
              sortOrder: index,
              isPrimary: index === primaryIndex,
            });
            setUploadProgress({ uploaded: index + 1, total: selectedFiles.length });
          } catch {
            throw new Error(`IMAGE_UPLOAD_PARTIAL:${createdProduct.id}:${index}`);
          }
        }
      }

      navigate('/admin/products');
    } catch (submissionError) {
      const message = submissionError instanceof Error ? submissionError.message : '';
      if (message.startsWith('IMAGE_UPLOAD_PARTIAL:')) {
        const [, productId, failedIndexText] = message.split(':');
        const uploadedCount = Number.parseInt(failedIndexText, 10);
        setError(
          `Product was created successfully (ID: ${productId}), but image upload stopped after ${uploadedCount}/${selectedFiles.length} images. You can continue managing images later.`,
        );
        return;
      }

      setError('Failed to create product. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  const isFormBusy = createProductMutation.isPending || submitting;

  return (
    <Paper sx={{ p: 3, maxWidth: 900 }}>
      <Stack spacing={2} component="form" onSubmit={handleSubmit}>
        <Typography variant="h5">Create new product</Typography>
        {error && <Alert severity="error">{error}</Alert>}
        <TextField
          label="Title"
          value={formState.title}
          onChange={(event) => {
            const title = event.target.value;
            setFormState((prev) => ({
              ...prev,
              title,
              slug: slugManuallyEdited ? prev.slug : toSlug(title),
            }));
          }}
        />
        <TextField
          label="Slug"
          value={formState.slug}
          onChange={(event) => {
            setSlugManuallyEdited(true);
            setFormState((prev) => ({ ...prev, slug: event.target.value }));
          }}
          error={!isValidSlug(formState.slug)}
          helperText={
            !isValidSlug(formState.slug)
              ? 'Slug can only contain lowercase letters, numbers, and hyphens (e.g. "vintage-leather-bag")'
              : 'Auto-generated from title. Edit to customize.'
          }
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

        <Button variant="outlined" component="label" disabled={isFormBusy}>
          Select product images
          <input
            hidden
            type="file"
            accept="image/jpeg,image/png,image/webp"
            multiple
            onChange={handleFileSelect}
          />
        </Button>
        <Typography variant="body2" color="text.secondary">
          {selectedFiles.length > 0
            ? `${selectedFiles.length} image(s) selected — you can remove background before uploading.`
            : `No images selected. You can upload up to ${maxImagesPerProduct} images.`}
        </Typography>

        {selectedFiles.length > 0 && (
          <Box>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
              For each image you can optionally remove the background.
              Choose which version to use, then click "Create product" to upload.
            </Typography>
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2 }}>
              {selectedFiles.map((file, index) => (
                <ImageUploadWithPreview
                  key={`${file.name}-${file.size}-${file.lastModified}`}
                  file={file}
                  isPrimary={index === primaryIndex}
                  onSetPrimary={() => setPrimaryIndex(index)}
                  onRemove={() => handleRemoveFile(index)}
                  onResult={(result) => handleImageResult(index, result)}
                  disabled={isFormBusy}
                />
              ))}
            </Box>
          </Box>
        )}

        {uploadProgress && uploadProgress.total > 0 && (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <CircularProgress size={18} />
            <Typography variant="body2" color="text.secondary">
              Uploading images: {uploadProgress.uploaded}/{uploadProgress.total}
            </Typography>
          </Box>
        )}

        <Box>
          <Button type="submit" variant="contained" disabled={isFormBusy}>
            {isFormBusy ? 'Creating...' : 'Create product'}
          </Button>
        </Box>
      </Stack>
    </Paper>
  );
}
