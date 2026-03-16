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

export interface UserProfile {
  id: number;
  username: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  role: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}
