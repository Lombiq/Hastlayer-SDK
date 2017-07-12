using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Hast.Samples.Kpz
{
    /// <summary>
    /// This form allows us to inspect the KPZ algorithm step by step.
    /// It relies heavily on the <see cref="KpzStateLogger"/> class.
    /// </summary>
    public partial class InspectForm : Form
    {
        KpzStateLogger _stateLogger;

        /// <summary>
        /// When the form is loaded, <see cref="listIterations"/> is initialized with the interations available in
        /// <see cref="_stateLogger"/>.
        /// </summary>
        /// <param name="StateLogger">is the data source to be displayed on the form.</param>
        public InspectForm(KpzStateLogger StateLogger)
        {
            InitializeComponent();
            this._stateLogger = StateLogger;
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
            dgv.ColumnCount = arr.GetLength(0);
            for (int y = 0; y < arr.GetLength(1); y++)
            {
                var dgvRow = new DataGridViewRow();
                for (int x = 0; x < arr.GetLength(0); x++)
                {
                    dgvRow.Cells.Add(new DataGridViewTextBoxCell() { Value = arr[x, y] });
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
            dgv.ColumnCount = arr.GetLength(0);
            for (int y = 0; y < arr.GetLength(1); y++)
            {
                var dgvRow = new DataGridViewRow();
                for (int x = 0; x < arr.GetLength(0); x++)
                {
                    dgvRow.Cells.Add(new DataGridViewTextBoxCell()
                    {
                        Value = String.Format("{0}{1}", (arr[x, y].dx) ? "1" : "0", (arr[x, y].dy) ? "1" : "0")
                    });
                }
                dgv.Rows.Add(dgvRow);
            }
        }

        /// <summary>
        /// This function adds highlight (by applying background color) to a list of table cells in the
        /// <see cref="DataGridView" />.
        /// </summary>
        /// <param name="HighlightedCoords">is the list of table cell indexes to be highlighted.</param>
        /// <param name="Color">is the background color to be set on the given table cells.</param>
        private void DgvAddHighlight(List<KpzCoords> HighlightedCoords, Color HighlightColor)
        {
            foreach (var coord in HighlightedCoords)
            {
                dgv.Rows[coord.y].Cells[coord.x].Style.BackColor = HighlightColor;
            }
        }

        /// <summary>
        /// This function adds highlight (by applying background color) to a list of table cells in the
        /// <see cref="DataGridView" />.
        /// </summary>
        private void listIterations_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                listActions.Items.Clear();
                int i = 0;
                _stateLogger.Iterations[listIterations.SelectedIndex].Actions.ForEach(
                (a) => listActions.Items.Add(String.Format("{0} {1}", i++, a.Description))
                );
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }
        }

        /// <summary>
        /// If an item in <see cref="listActions" /> is clicked, the <see cref="DataGridView" /> is updated.
        /// </summary>
        private void listActions_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                KpzAction action = _stateLogger
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
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }
        }
    }
}
