namespace LibraryM.Application.Auth.Models;

public sealed record AuthResponse(int UserId, string Token, string Username, string FullName, string Role, string QrCodeValue);
