namespace stockfront.ViewModels;

/// <summary>
/// A stub page for sections that aren't built yet (Склад, Витрина, Заказы, Отчёты, Пользователи,
/// Профиль). Carries a title and a short, role-specific description of what the section will let the
/// current user do, so the access matrix is visible even before the real modules exist.
/// </summary>
public sealed class PlaceholderViewModel
{
    public PlaceholderViewModel(string title, string description)
    {
        Title = title;
        Description = description;
    }

    public string Title { get; }

    /// <summary>What the current role may do here (e.g. "Приёмка и списание товаров (без удаления).").</summary>
    public string Description { get; }
}