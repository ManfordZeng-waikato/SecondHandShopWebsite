export interface Category {
  id: string;
  name: string;
  slug: string;
  parentId?: string;
  sortOrder: number;
  isActive: boolean;
}

export interface CategoryTreeNode {
  id: string;
  name: string;
  slug: string;
  children: CategoryTreeNode[];
}
