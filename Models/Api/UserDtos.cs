using System.ComponentModel.DataAnnotations;

namespace MVC.Models.Api;

public class ProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? PhoneNumber { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public class UpdateProfileRequestDto
{
    [StringLength(100)]
    public string? Name { get; set; }

    [StringLength(100)]
    public string? Surname { get; set; }

    [StringLength(32)]
    public string? PhoneNumber { get; set; }

    public string? RowVersion { get; set; }
}

public class UserListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? PhoneNumber { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public class UserDetailsDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? PhoneNumber { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public class UpdateUserRequestDto
{
    [StringLength(100)]
    public string? Name { get; set; }

    [StringLength(100)]
    public string? Surname { get; set; }

    [StringLength(32)]
    public string? PhoneNumber { get; set; }

    public string? RowVersion { get; set; }
}

public class DeleteUserRequestDto
{
    public string? RowVersion { get; set; }
}
