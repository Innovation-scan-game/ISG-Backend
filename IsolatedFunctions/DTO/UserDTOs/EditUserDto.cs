﻿namespace IsolatedFunctions.DTO.UserDTOs;

public class EditUserDto
{
    public string Id { get; set; } = "";
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; }
}
