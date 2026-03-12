using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace Bus_coursework
{
    public class SearchManager
    {
        private readonly DataGridView _grid;   
        private readonly TextBox _searchBox;  
        private DataTable _currentTable;

        public SearchManager(DataGridView grid, TextBox searchBox)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _searchBox = searchBox ?? throw new ArgumentNullException(nameof(searchBox));

            _searchBox.TextChanged += SearchBox_TextChanged;
        }

        public void SetTable(DataTable table)
        {
            _currentTable = table;
            _grid.DataSource = table;
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            if (_currentTable == null) return;

            string text = _searchBox.Text.Trim().Replace("'", "''");

            if (string.IsNullOrEmpty(text))
            {
                _currentTable.DefaultView.RowFilter = string.Empty;
                return;
            }

            var filters = new List<string>();

            foreach (DataColumn col in _currentTable.Columns)
            {
                filters.Add(
                    $"CONVERT([{col.ColumnName}], 'System.String') LIKE '{text}%'");
            }

            _currentTable.DefaultView.RowFilter = string.Join(" OR ", filters);
        }
    }
}
