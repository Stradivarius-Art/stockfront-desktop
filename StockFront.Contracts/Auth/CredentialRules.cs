using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace StockFront.Contracts.Auth;

/// <summary>
/// The single source of truth for credential validation. The registration screen uses these for
/// live, field-level feedback; <see cref="IAuthService"/> uses the same rules so they're enforced
/// even when the UI is bypassed (seeding, tests, a future API). Each method returns a user-facing
/// error message, or <c>null</c> when the value is valid.
/// </summary>
public static partial class CredentialRules
{
    public const int UsernameMinLength = 3;
    public const int UsernameMaxLength = 64;
    public const int PasswordMinLength = 6;

    // BCrypt only hashes the first 72 bytes; reject longer passwords rather than silently truncate.
    public const int PasswordMaxLength = 72;
    public const int DisplayNameMaxLength = 200;
    public const int EmailMaxLength = 256;

    [GeneratedRegex("^[A-Za-z0-9._-]+$")]
    private static partial Regex UsernamePattern();

    public static string? ValidateUsername(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return "Укажите логин.";

        username = username.Trim();
        if (username.Length < UsernameMinLength || username.Length > UsernameMaxLength)
            return $"Логин должен быть от {UsernameMinLength} до {UsernameMaxLength} символов.";
        if (!UsernamePattern().IsMatch(username))
            return "Логин может содержать только латинские буквы, цифры и символы . _ -";

        return null;
    }

    public static string? ValidatePassword(string? password)
    {
        if (string.IsNullOrEmpty(password))
            return "Укажите пароль.";
        if (password.Length < PasswordMinLength)
            return $"Пароль должен быть не короче {PasswordMinLength} символов.";
        if (password.Length > PasswordMaxLength)
            return $"Пароль не длиннее {PasswordMaxLength} символов.";

        return null;
    }

    public static string? ValidatePasswordConfirmation(string? password, string? confirmation) =>
        password == confirmation ? null : "Пароли не совпадают.";

    public static string? ValidateDisplayName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return "Укажите отображаемое имя.";
        if (displayName.Trim().Length > DisplayNameMaxLength)
            return $"Имя не длиннее {DisplayNameMaxLength} символов.";

        return null;
    }

    /// <summary>E-mail is optional; only a non-empty value is checked for format and length.</summary>
    public static string? ValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        email = email.Trim();
        if (email.Length > EmailMaxLength)
            return $"E-mail не длиннее {EmailMaxLength} символов.";
        if (!new EmailAddressAttribute().IsValid(email))
            return "Некорректный e-mail.";

        return null;
    }
}
