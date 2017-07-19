using System;
using System.Windows.Forms;

namespace DBExplorer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Init();
        }

        private void txtServer_Validated(object sender, EventArgs e)
        {
            Connect_Server();
        }

        private void cmbDatabase_SelectedIndexChanged(object sender, EventArgs e)
        {
            Connect_Database();
        }

        private void cmbSchema_SelectedIndexChanged(object sender, EventArgs e)
        {
            Select_Schema();
        }

        private void cmbTable_SelectedIndexChanged(object sender, EventArgs e)
        {
            Select_Table();
        }

        private void txtFilter_TextChanged(object sender, EventArgs e)
        {
            txtFilterAscii.Text = "";
            txtFilterAscii.Enabled = (txtFilter.Text == "");
            _ascii = false;
            DoFilter();
        }

        private void txtFilterAscii_TextChanged(object sender, EventArgs e)
        {
            txtFilter.Text = "";
            txtFilter.Enabled = (txtFilterAscii.Text == "");
            _ascii = !txtFilter.Enabled;
            DoFilter();
        }

        private void chkColumns_MouseUp(object sender, MouseEventArgs e)
        {
            Check_Column();
        }

        private void dataGrid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            SaveChange();
        }

        private void mnuExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void mnuAllFields_Click(object sender, EventArgs e)
        {
            DoFilter();
        }

        private void dataGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            ShowMatch(e);
        }
    }
}
