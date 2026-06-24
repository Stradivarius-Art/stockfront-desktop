using System.ComponentModel.DataAnnotations;

namespace StockFront.Contracts.Auth;

/// <summary>
/// The single source of truth for credential validation. The registration screen uses these for
/// live, field-level feedback; <see cref="IAuthService"/> uses the same rules so they're enforced
/// even when the UI is bypassed (seeding, tests, a future API). Each method returns a user-facing
/// error message, or <c>null</c> when the value is valid.
/// </summary>
public static class CredentialRules
{
    public const int PasswordMinLength = 6;

    // BCrypt only hashes the first 72 bytes; reject longer passwords rather than silently truncate.
    public const int PasswordMaxLength = 72;
    public const int DisplayNameMaxLength = 200;
    public const int EmailMaxLength = 256;

    public static string? ValidatePassword(string? password)
    {
        if (string.IsNullOrEmpty(password))
            return "Укажите пароль.";
        if (password.Length < PasswordMinLength)
            return $"Пароль должен быть не короче {PasswordMinLength} символов.";
        if (password.Length > PasswordMaxLength)
            return $"Пароль не длиннее {PasswordMaxLength} символов.";

        // Only printable ASCII (Latin letters, digits, punctuation) — no spaces/whitespace and no
        // non-ASCII (e.g. Cyrillic), so the password is portable across keyboards and login forms.
        foreach (var ch in password)
        {
            if (char.IsWhiteSpace(ch))
                return "Пароль не должен содержать пробелы.";
            if (ch < '!' || ch > '~')
                return "Пароль может содержать только латинские буквы, цифры и спецсимволы.";
        }

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

    /// <summary>E-mail is the login, so it is required and must be a valid address.</summary>
    public static string? ValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "Укажите e-mail.";

        email = email.Trim();
        if (email.Length > EmailMaxLength)
            return $"E-mail не длиннее {EmailMaxLength} символов.";
        if (!new EmailAddressAttribute().IsValid(email))
            return "Некорректный e-mail.";

        return null;
    }
}
