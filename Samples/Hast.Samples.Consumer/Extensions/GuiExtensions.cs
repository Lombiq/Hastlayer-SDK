namespace Terminal.Gui
{
    public static class GuiExtensions
    {
        public static FrameView WithShortcut(this FrameView frameView, Key shortcutKeys)
        {
            frameView.Shortcut = shortcutKeys;
            frameView.Title = $"{frameView.Title} ({frameView.ShortcutTag})";
            frameView.ShortcutAction = frameView.SetFocus;

            return frameView;
        }

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
            view.Width = Dim.Fill();
            view.Height = Dim.Fill();

            return view;
        }
    }
}
