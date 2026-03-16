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
  qrCodeValue: string;
  role: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}
