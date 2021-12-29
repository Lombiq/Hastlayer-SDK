using Hast.Samples.Kpz.Algorithms;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;

namespace Hast.Samples.Kpz
{
    /// <summary>
    /// This form allows us to inspect the KPZ algorithm step by step.
    /// It relies heavily on the <see cref="KpzStateLogger"/> class.
    /// For large grids, it does not display the whole grid (due to speed limitations of <see cref="DataGridView"/>)
    /// only a part of it, which can be configured with <see cref="MaxGridDisplayWidth"/> and
    /// <see cref="MaxGridDisplayHeight"/>.
    /// </summary>
    public partial class InspectForm : Form
    {
        private const int MaxGridDisplayWidth = 128;
        private const int MaxGridDisplayHeight = 128;

        private readonly KpzStateLogger _stateLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="InspectForm"/> class. When the form is loaded,
        /// <see cref="listIterations"/> is initialized with the iterations available in <see cref="_stateLogger"/>.
        /// </summary>
        /// <param name="stateLogger">is the data source to be displayed on the form.</param>
        public InspectForm(KpzStateLogger stateLogger)
        {
            InitializeComponent();
            _stateLogger = stateLogger;

            for (int i = 0; i < _stateLogger.Iterations.Count; i++)
            {
                listIterations.Items.Add(i);
            }
        }

        /// <summary>
        /// This function displays a 2D int array in the <see cref="DataGridView"/> on the form.
        /// </summary>
        /// <param name="arr">is the input array.</param>
        private void DgvShowIntArray(int[,] arr)
        {
            dgv.Rows.Clear();
            int gridDisplayWidth = Math.Min(MaxGridDisplayWidth, arr.GetLength(0));
            int gridDisplayHeight = Math.Min(MaxGridDisplayHeight, arr.GetLength(1));
            dgv.ColumnCount = gridDisplayWidth;

            for (int y = 0; y < gridDisplayHeight; y++)
            {
                var dgvRow = new DataGridViewRow();

                for (int x = 0; x < gridDisplayWidth; x++)
                {
                    dgvRow.Cells.Add(new DataGridViewTextBoxCell { Value = arr[x, y] });
                }

                dgv.Rows.Add(dgvRow);
            }
        }

        /// <summary>
        /// This function displays a 2D array of <see cref="KpzNode"/> items on the <see cref="DataGridView"/>
        /// on the form. dx and dy values are shown as binary digits, e.g. 10 in a cell means that dx=1 and dy=0
        /// for that <see cref="KpzNode"/>.
        /// </summary>
        /// <param name="arr">is the input array.</param>
        private void DgvShowKpzNodeArray(KpzNode[,] arr)
        {
            dgv.Rows.Clear();
            int gridDisplayWidth = Math.Min(MaxGridDisplayWidth, arr.GetLength(0));
            int gridDisplayHeight = Math.Min(MaxGridDisplayHeight, arr.GetLength(1));
            dgv.ColumnCount = gridDisplayWidth;

            for (int y = 0; y < gridDisplayHeight; y++)
            {
                var dgvRow = new DataGridViewRow();

                for (int x = 0; x < gridDisplayWidth; x++)
                {
                    dgvRow.Cells.Add(new DataGridViewTextBoxCell
                    {
                        Value = $"{(arr[x, y].Dx ? "1" : "0")}{(arr[x, y].Dy ? "1" : "0")}",
                    });
                }

                dgv.Rows.Add(dgvRow);
            }
        }

        /// <summary>
        /// This function adds highlight (by applying background color) to a list of table cells in the
        /// <see cref="DataGridView" />.
        /// </summary>
        /// <param name="highlightedCoords">is the list of table cell indexes to be highlighted.</param>
        /// <param name="highlightColor">is the background color to be set on the given table cells.</param>
        private void DgvAddHighlight(IEnumerable<KpzCoords> highlightedCoords, Color highlightColor)
        {
            foreach (var coord in highlightedCoords)
            {
                if (coord.X >= dgv.ColumnCount || coord.Y >= dgv.RowCount) continue;
                dgv.Rows[coord.Y].Cells[coord.Y].Style.BackColor = highlightColor;
            }
        }

        /// <summary>
        /// This function adds highlight (by applying background color) to a list of table cells in the
        /// <see cref="DataGridView" />.
        /// </summary>
        [SuppressMessage(
            "Design",
            "CA1031:Do not catch general exception types",
            Justification = "If there's an error then nothing to do, just display it to the user.")]
        private void ListIterations_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                listActions.Items.Clear();
                int i = 0;
                _stateLogger.Iterations[listIterations.SelectedIndex].Actions.ForEach(
                    (a) => listActions.Items.Add($"{i++} {a.Description}"));
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }

        /// <summary>
        /// If an item in <see cref="listActions" /> is clicked, the <see cref="DataGridView" /> is updated.
        /// </summary>
        [SuppressMessage(
            "Design",
            "CA1031:Do not catch general exception types",
            Justification = "If there's an error then nothing to do, just display it to the user.")]
        private void ListActions_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                var action = _stateLogger
                    .Iterations[listIterations.SelectedIndex]
                    .Actions[listActions.SelectedIndex];

                if (action.Grid.GetLength(0) > 0)
                {
                    DgvShowKpzNodeArray(action.Grid);
                }
                else if (action.HeightMap.GetLength(0) > 0)
                {
                    DgvShowIntArray(action.HeightMap);
                }
                else
                {
                    dgv.Rows.Clear();
                }

                if (action.HighlightedCoords.Count > 0)
                {
                    DgvAddHighlight(action.HighlightedCoords, action.HightlightColor);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }
    }
}
