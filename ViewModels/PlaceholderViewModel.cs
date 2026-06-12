namespace stockfront.ViewModels;

/// <summary>
/// A stub page for sections that aren't built yet (Склад, Витрина, Заказы, Отчёты). Carries only a
/// title; the view shows a "в разработке" message.
/// </summary>
public sealed class PlaceholderViewModel
{
    public PlaceholderViewModel(string title) => Title = title;

    public string Title { get; }
}
