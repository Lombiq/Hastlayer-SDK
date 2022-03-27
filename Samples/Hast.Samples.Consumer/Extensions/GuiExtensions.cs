using System;

namespace Terminal.Gui;

public static class GuiExtensions
{
    public static void TileHorizontally(
        this View parent,
        View left,
        View right,
        int leftWidth,
        (int Top, int Right, int Bottom, int Left) margins)
    {
        left.X = margins.Left;
        left.Y = margins.Top;
        left.Width = leftWidth;
        left.Height = Dim.Height(parent) - Dim.Sized(margins.Top + margins.Bottom);

        right.X = leftWidth;
        right.Y = margins.Top;
        right.Width = Dim.Width(parent) - left.Width - Dim.Sized(margins.Left + margins.Right);
        right.Height = left.Height;
    }

    public static T Fill<T>(this T view)
        where T : View
    {
        view.X = 0;
        view.Y = 0;
        view.Width = Dim.Percent(100);
        view.Height = Dim.Percent(100);

        return view;
    }

    public static T FillHorizontally<T>(this T view, int horizontalMargin = 1, bool verticalCenter = true)
        where T : View
    {
        view.X = horizontalMargin;
        if (verticalCenter) view.Y = Pos.Center();
        view.Width = Dim.Percent(100) - (horizontalMargin * 2);
        view.Height = 1;

        return view;
    }

    public static void OnKeyPressed(this View view, Key key, Action action) =>
        view.KeyPress += args =>
        {
            if (args.KeyEvent.Key == key)
            {
                action();
                args.Handled = true;
            }
        };

    public static void OnEnterKeyPressed(this View view, Action action) => OnKeyPressed(view, Key.Enter, action);
    public static void OnEscKeyPressed(this View view, Action action) => OnKeyPressed(view, Key.Esc, action);
}
