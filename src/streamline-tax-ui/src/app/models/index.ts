export interface Receipt {
  id: string;
  userId: string;
  fileName: string;
  storageKey: string;
  contentType: string;
  fileSize: number;
  status: 'Pending' | 'Processing' | 'Processed' | 'Failed';
  extractedText?: string;
  amount?: number;
  receiptDate?: string;
  merchantName?: string;
  createdAt: string;
}

export interface Transaction {
  id: string;
  userId: string;
  amount: number;
  description: string;
  category: TransactionCategory;
  transactionDate: string;
  taxWithheld: number;
  receiptId?: string;
  taxPeriodId?: string;
  createdAt: string;
}

export type TransactionCategory =
  | 'Uncategorized'
  | 'Salary'
  | 'Freelance'
  | 'BusinessIncome'
  | 'Investment'
  | 'Rental'
  | 'OtherIncome';

export interface TaxSummary {
  totalIncome: number;
  totalTaxWithheld: number;
  estimatedTaxDue: number;
  balance: number;
  taxWithholdingRate: number;
  transactionCount: number;
  availableReserve: number;
  isSurplus: boolean;
}

export interface UploadReceiptResponse {
  receiptId: string;
  status: string;
  isDuplicate?: boolean;
  duplicateType?: 'ExactFile' | 'SimilarContent' | null;
  existingReceiptId?: string | null;
}

export type TaxPeriodStatus = 'Open' | 'Closed' | 'Locked';

export interface TaxPeriod {
  id: string;
  taxAccountId: string;
  name: string;
  startDate: string;
  endDate: string;
  status: TaxPeriodStatus;
  totalIncome: number;
  totalTaxWithheld: number;
  estimatedTaxDue: number;
  balance: number;
  transactionCount: number;
  closedAt?: string | null;
  lockedAt?: string | null;
}

export interface TaxPeriodTransaction {
  id: string;
  amount: number;
  description: string;
  category: string;
  transactionDate: string;
  taxWithheld: number;
}

export interface TaxPeriodComparison {
  current?: TaxPeriod | null;
  previous?: TaxPeriod | null;
  incomeChangePercent?: number | null;
  taxWithheldChangePercent?: number | null;
  estimatedTaxDueChangePercent?: number | null;
  transactionCountChangePercent?: number | null;
}

export interface TaxPeriodListResponse {
  periods: TaxPeriod[];
  comparison: TaxPeriodComparison;
}

export interface TaxPeriodDetailResponse {
  period: TaxPeriod;
  transactions: TaxPeriodTransaction[];
}

export type NotificationType =
  | 'ReceiptProcessed'
  | 'ReceiptProcessingFailed'
  | 'DuplicateReceipt'
  | 'TaxDeadlineApproaching'
  | 'TaxDeadlineOverdue'
  | 'TaxDueChanged'
  | 'WeeklyCategorization'
  | 'TaxPeriodLocked';

export interface AppNotification {
  id: string;
  userId: string;
  type: NotificationType;
  title: string;
  message: string;
  isRead: boolean;
  createdAt: string;
  readAt?: string | null;
  relatedEntityId?: string | null;
  relatedEntityType?: string | null;
}