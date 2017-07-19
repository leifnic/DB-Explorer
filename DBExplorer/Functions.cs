using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DBExplorer.Properties;

namespace DBExplorer
{
    public partial class Form1
    {
        private bool _ascii;
        private string _sql;
        private string _server;
        private string _db;
        private string _schema;
        private string _table;
        private readonly BindingSource _bindingSource = new BindingSource();
        private static SqlDataAdapter _da;
        private static DataTable _dt;

        private void Init()
        {
            dataGrid.DataSource = _bindingSource;
            //txtServer.Text = @"H46338\NAVDEMO";
            txtSQL.Text = _sql;
            toolTip.SetToolTip(txtFilter, string.Format(Resources.tipFilter));
            toolTip.SetToolTip(txtFilterAscii, string.Format(Resources.tipFilterAscii));
            toolTip.SetToolTip(chkColumns, string.Format(Resources.tipColumns));
            toolTip.SetToolTip(txtSQL, string.Format(Resources.tipSQL));
            mnuAllFields.ToolTipText = string.Format(Resources.mnuAllFields);
        }

        private SqlConnection Connect()
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = _server,
                InitialCatalog = string.IsNullOrEmpty(_db) ? "master" : _db,
                IntegratedSecurity = true
            };
            return new SqlConnection(builder.ConnectionString);
        }

        private void DoFilter()
        {
            foreach (DataGridViewTextBoxColumn c in dataGrid.Columns)
                c.HeaderCell.Style.Font = new Font(DefaultFont, FontStyle.Regular);
            if (string.IsNullOrEmpty(_server)) return;
            if (string.IsNullOrEmpty(_db)) return;
            if (string.IsNullOrEmpty(_table)) return;
            var colIdx = -1;
            if (dataGrid.CurrentCell != null)
                colIdx = dataGrid.CurrentCell.ColumnIndex;
            else
            {
                txtFilterAscii.Text = "";
                txtFilter.Text = "";
            }
            var txtString = _ascii ? txtFilterAscii.Text.ToString() : txtFilter.Text.ToString();
            var rowString = "";
            if (mnuAllFields.Checked)
            {
                foreach (var item in chkColumns.Items)
                {
                    if (chkColumns.GetItemCheckState(chkColumns.Items.IndexOf(item)) != CheckState.Checked) continue;
                    if (rowString != "")
                        rowString += @" OR ";
                    rowString += CreateFldFilter(item.ToString(), txtString);
                }
            }
            else if (colIdx != -1)
            {
                rowString = CreateFldFilter(dataGrid.Columns[colIdx].Name, txtString);
                if (txtString != "")
                    dataGrid.Columns[colIdx].HeaderCell.Style.Font = new Font(DefaultFont, FontStyle.Bold);
            }
            var sql = _sql;
            if ((txtString != "") && (rowString != ""))
                sql += " WHERE " + rowString;
            var con = Connect();
            using (con)
            {
                _dt = new DataTable();
                _da = new SqlDataAdapter(sql, con);
                _da.Fill(_dt);
                _bindingSource.DataSource = _dt;
                dataGrid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
                dataGrid.Refresh();
            }
            txtSQL.Text = sql;
            dataGrid.Refresh();
            lblRows.Text = string.Format(Resources.Rows, dataGrid.RowCount);
            if (colIdx == -1) return;
            if (dataGrid.RowCount < 1) return;
            if (dataGrid.ColumnCount <= colIdx) return;
            if (dataGrid.ColumnCount < 2) return;
            dataGrid.CurrentCell = dataGrid.Rows[0].Cells[colIdx];
        }

        private string CreateFldFilter(string col, string filter)
        {
            if (!_ascii)
                return $@"(UPPER(CONVERT(VARCHAR(300), [{col}], 20)) LIKE UPPER('%{filter}%'))";
            var cL = filter.Split(',').ToList();
            byte n;
            var charList = cL.Where(c => byte.TryParse(c, out n)).ToList();
            if (charList.Count < 1)
                return "";
            if (charList.Count < 2)
                return $@"([{col}] LIKE '%' + CHAR({charList.FirstOrDefault()}) + '%')";
            var txtString = "";
            foreach (var aChar in charList)
                txtString += txtString == "" ? $"'%[' + CHAR({aChar})" : $" + '|' + CHAR({aChar})";
            txtString += " + ']%' escape '|'";
            return $@"([{col}] LIKE {txtString})";
        }

        private void ShowMatch(DataGridViewCellFormattingEventArgs e)
        {
            if (!mnuAllFields.Checked) return;
            var stringValue = e.Value?.ToString();
            if (_ascii)
            {
                var search = txtFilterAscii.Text.ToString();
                var cL = search.Split(',').ToList();
                int n;
                var charList = cL.Where(c => int.TryParse(c, out n)).ToList();
                foreach (var ch in charList)
                {
                    if (!int.TryParse(ch, out n)) continue;
                    var cSearch = ((char)n).ToString();
                    if ((stringValue?.IndexOf(cSearch, StringComparison.Ordinal) > -1) && !string.IsNullOrEmpty(search))
                        e.CellStyle.BackColor = Color.Yellow;
                }
            }
            else
            {
                var search = txtFilter.Text.ToString().ToLower();
                stringValue = stringValue?.ToLower();
                if ((stringValue?.IndexOf(search, StringComparison.Ordinal) > -1) && !string.IsNullOrEmpty(search))
                    e.CellStyle.BackColor = Color.Yellow;
            }
        }

        private void Connect_Server()
        {
            if (txtServer.Text == _server) return;
            _db = "";
            cmbDatabase.DataSource = null;
            cmbTable.DataSource = null;
            _server = txtServer.Text;
            var con = Connect();
            using (con)
            {
                var ds = new DataSet();
                const string sql = @"SELECT [name] FROM [master].[dbo].[sysdatabases] WHERE [dbid] > 4 ORDER BY [name]";
                try
                {
                    var da = new SqlDataAdapter(sql, con);
                    da.Fill(ds, "x");
                    cmbDatabase.DisplayMember = "name";
                    cmbDatabase.ValueMember = "name";
                    cmbDatabase.DataSource = ds.Tables["x"];
                }
                catch
                {
                    cmbDatabase.Text = @"Server not found";
                }
            }
        }

        private void Connect_Database()
        {
            if (cmbDatabase.Text == _db)
                return;
            _db = cmbDatabase.Text;
            var con = Connect();
            using (con)
            {
                var ds = new DataSet();
                const string sql = @"SELECT sys.schemas.name FROM sys.schemas";
                try
                {
                    var da = new SqlDataAdapter(sql, con);
                    da.Fill(ds, "x");
                    cmbSchema.DisplayMember = "name";
                    cmbSchema.ValueMember = "name";
                    cmbSchema.DataSource = ds.Tables["x"];
                }
                catch
                {
                    cmbSchema.Text = @"Database not found";
                }
            }
        }
        
        private void Select_Schema()
        {
            if (cmbSchema.Text == _schema)
                return;
            _db = cmbDatabase.Text;
            var con = Connect();
            _schema = cmbSchema.Text;
            using (con)
            {
                var ds = new DataSet();
                var sql = @"SELECT  sys.objects.name FROM sys.objects " +
                          @"INNER JOIN sys.schemas ON sys.objects.schema_id = sys.schemas.schema_id " +
                         $@"WHERE sys.schemas.name = '{_schema}' " +
                          @"ORDER BY sys.objects.name";
                try
                {
                    var da = new SqlDataAdapter(sql, con);
                    da.Fill(ds, "x");
                    cmbTable.DisplayMember = "name";
                    cmbTable.ValueMember = "name";
                    cmbTable.DataSource = ds.Tables["x"];
                }
                catch
                {
                    cmbTable.Text = @"Database not found";
                }
            }
        }

        private void Select_Table()
        {
            if (cmbTable.Text == _table) return;
            _table = cmbTable.Text;
            txtFilter.Text = "";
            var con = Connect();
            using (con)
            {
                var dt = new DataTable();
                var sql = @"SELECT [COLUMN_NAME], [DATA_TYPE] " +
                          @"FROM [INFORMATION_SCHEMA].[COLUMNS] " +
                         $@"WHERE ([TABLE_NAME]  = '{_table}') AND " +
                                @"([DATA_TYPE] NOT IN ('image','timestamp','varbinary'))";
                try
                {
                    chkColumns.Items.Clear();
                    var da = new SqlDataAdapter(sql, con);
                    da.Fill(dt);
                    foreach (DataRow row in dt.Rows)
                        chkColumns.Items.Add(row["COLUMN_NAME"]);
                }
                catch
                {
                    //ignored
                }
            }
        }

        private void Check_Column()
        {
            var con = Connect();
            using (con)
            {
                var rowString = "";
                try
                {
                    foreach (var item in chkColumns.CheckedItems)
                    {
                        var chk = chkColumns.GetItemCheckState(chkColumns.Items.IndexOf(item)) == CheckState.Checked;
                        if (!chk) continue;
                        var s = item.ToString();
                        rowString += rowString == "" ? $@"[{s}]" : $@",[{s}]";
                    }
                    if (rowString == "")
                        rowString = "top(0) ''";
                    _sql = $@"SELECT {rowString} FROM [{_schema}].[{_table}]";
                }
                catch
                {
                    //ignored
                }
            }
            DoFilter();
        }

        private void SaveChange()
        {
            var con = Connect();
            using (con)
            {
                _bindingSource.EndEdit();
                _da.SelectCommand.Connection = con;
                var scb = new SqlCommandBuilder(_da);
                _da.UpdateCommand = scb.GetUpdateCommand();
                _da.Update(_dt);
            }
        }
    }
}
