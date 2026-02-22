export interface Category {
  id: string;
  name: string;
  slug: string;
  parentCategoryId?: string;
  sortOrder: number;
  isActive: boolean;
}
