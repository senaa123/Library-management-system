namespace LibraryM.Application.Auth.Models;

public sealed record RegisterResponse(string Message, int UserId, string Username, string FullName, string QrCodeValue);
