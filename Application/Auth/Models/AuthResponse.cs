namespace LibraryM.Application.Auth.Models;

public sealed record AuthResponse(string Token, string Username, string Role);
