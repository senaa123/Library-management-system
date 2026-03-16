export function getStoredRole() {
  return localStorage.getItem("role") ?? "";
}

export function isStaffRole(role: string | null | undefined) {
  return role === "Admin" || role === "Librarian";
}

export function isMemberRole(role: string | null | undefined) {
  return role === "Member";
}

export function isLoggedIn() {
  return !!localStorage.getItem("token");
}
