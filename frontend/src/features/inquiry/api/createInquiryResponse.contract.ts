import type { CreateInquiryResponse } from '../../../entities/inquiry/types';

type Equal<X, Y> =
  (<T>() => T extends X ? 1 : 2) extends <T>() => T extends Y ? 1 : 2 ? true : false;

/**
 * Compile-time contract: must match WebApi `CreateInquiryResponse`
 * (System.Text.Json camelCase → `inquiryId`, not `id`).
 */
const _createInquiryResponseMatchesBackendJson: true = true as Equal<
  CreateInquiryResponse,
  { inquiryId: string }
>;

void _createInquiryResponseMatchesBackendJson;
