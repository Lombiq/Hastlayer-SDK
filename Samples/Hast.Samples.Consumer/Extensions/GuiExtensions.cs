namespace Terminal.Gui
{
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
            view.Width = Dim.Fill();
            view.Height = Dim.Fill();

            return view;
        }

        public static T FillHorizontally<T>(this T view, int horizontalMargin = 1, bool verticalCenter = true)
            where T : View
        {
            view.X = horizontalMargin;
            if (verticalCenter) view.Y = Pos.Center();
            view.Width = Dim.Fill(horizontalMargin);
            view.Height = 1;

            return view;
        }
    }
}
