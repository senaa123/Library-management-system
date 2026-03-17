export interface Book {
  id: number;
  title: string;
  author: string;
  description: string;
  category: string;
  isbn: string;
  bookType: string;
  totalCopies: number;
  availableCopies: number;
  isActive: boolean;
  createdAt: string;
  availabilityStatus: string;
}

export interface AuthResponse {
  userId: number;
  token: string;
  username: string;
  fullName: string;
  role: string;
  qrCodeValue: string;
}

export interface RegisterResponse {
  message: string;
  userId: number;
  username: string;
  fullName: string;
  qrCodeValue: string;
}

export interface MemberFineStatus {
  totalOutstandingFine: number;
  maxCirculationItems: number;
  isFineLimited: boolean;
  isCirculationBlocked: boolean;
  hasTemporaryRestriction: boolean;
  restrictedUntilUtc?: string | null;
  restrictionReason: string;
  warningMessage: string;
}

export interface Loan {
  id: number;
  bookId: number;
  bookTitle: string;
  isbn: string;
  borrowerId: number;
  borrowerName: string;
  borrowerUsername: string;
  borrowerPhoneNumber: string;
  issuedById: number;
  issuedByName: string;
  issuedAt: string;
  dueDate: string;
  returnedAt?: string | null;
  renewCount: number;
  status: string;
  outstandingFine: number;
  borrowPeriodDays: number;
  daysLeft: number;
  timeLeftLabel: string;
}

export interface Reservation {
  id: number;
  bookId: number;
  bookTitle: string;
  memberId: number;
  memberName: string;
  memberUsername: string;
  memberPhoneNumber: string;
  reservedAt: string;
  notifiedAt?: string | null;
  cancelledAt?: string | null;
  fulfilledAt?: string | null;
  pickupDeadline?: string | null;
  daysLeft: number;
  timeLeftLabel: string;
  canIssue: boolean;
  status: string;
}

export interface UserProfile {
  id: number;
  username: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  nicNumber: string;
  qrCodeValue: string;
  role: string;
  isActive: boolean;
  totalOutstandingFine: number;
  maxCirculationItems: number;
  isCirculationBlocked: boolean;
  hasTemporaryRestriction: boolean;
  restrictedUntilUtc?: string | null;
  restrictionReason: string;
  restrictionWarning: string;
  createdAt: string;
  updatedAt?: string | null;
}

export interface FineItem {
  loanId?: number | null;
  reservationId?: number | null;
  bookId?: number | null;
  bookTitle: string;
  fineType: string;
  description: string;
  assessedAt: string;
  dueDate: string;
  returnedAt?: string | null;
  accruedAmount: number;
  paidAmount: number;
  outstandingAmount: number;
}

export interface FineSummary {
  totalAccrued: number;
  totalPaid: number;
  totalOutstanding: number;
  status: MemberFineStatus;
  items: FineItem[];
}

export interface FinePaymentRecord {
  id: number;
  loanId?: number | null;
  memberId: number;
  memberName: string;
  amount: number;
  paidAt: string;
  notes: string;
  receivedById?: number | null;
  receivedByName: string;
  paymentMethod: string;
  externalReference: string;
}

export interface FineCheckoutSession {
  sessionId: string;
  checkoutUrl: string;
  amount: number;
}
