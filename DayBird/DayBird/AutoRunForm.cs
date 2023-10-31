using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DayBird
{
    public partial class AutoRunForm : Form
    {
        private List<AssemblyInstance> loadedAutoRuns = new List<AssemblyInstance>();

        private static void InitialLoad()
        {

        }

        public AutoRunForm()
        {
            InitializeComponent();
            dataGridView1.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.dataGridView1_CellFormatting);
            loadedAutoRuns = PluginManager.LoadAutoRuns();
            dataGridView1.DataSource = loadedAutoRuns;
            dataGridView1.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridView1.Columns[3].DefaultCellStyle.NullValue = "ERR";
            this.dataGridView1.KeyPress += new System.Windows.Forms.KeyPressEventHandler(CheckEnterKeyPress);

            DataGridViewComboBoxColumn priorityBox = (DataGridViewComboBoxColumn)dataGridView1.Columns["Priority"];
            for (int i = 0; i < loadedAutoRuns.Count(); i++)
            {
                priorityBox.Items.Add((i + 1).ToString());
                if (loadedAutoRuns[i].RequiredArgs.Count() > 0)
                {
                    dataGridView1.Rows[i].Cells[3].Value = "True";
                }
                else
                {
                    dataGridView1.Rows[i].Cells[3].Value = "False";
                }
            }
        }

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {

            if (e.ColumnIndex == 0) // your combo column index

            {
                int currPriority = loadedAutoRuns[e.RowIndex].Priority;
                if(e.Value != null && (string)e.Value != "Set")
                {
                    return;
                }
                if(currPriority > -1)
                {
                    e.Value = currPriority.ToString();
                    dataGridView1.Rows[e.RowIndex].Cells[0].Value = currPriority.ToString();
                }
                else
                {
                    e.Value = "Set";
                }
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
        private void CheckEnterKeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)

            {
                PopulateArgs();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            PopulateArgs();
        }

        private void PopulateArgs()
        {
            List<DataGridViewRow> checkedRows = (from DataGridViewRow r in dataGridView1.Rows where Convert.ToBoolean(r.Cells[2].Value) == true select r).ToList();
            List<DataGridViewRow> uncheckedRows = (from DataGridViewRow r in dataGridView1.Rows where Convert.ToBoolean(r.Cells[2].Value) == false select r).ToList();

            //added to handle any autorun plugins that were enabled and then later disabled
            foreach(DataGridViewRow uncheckedRow in uncheckedRows)
            {
                AssemblyInstance disabledPlugin = loadedAutoRuns.FirstOrDefault(x => x.AssemblyName == uncheckedRow.Cells[1].Value.ToString());
                disabledPlugin.Priority = -1;
            }


            DataGridViewRow nullCheck = checkedRows.FirstOrDefault(x => x.Cells[0].Value == null);
            HashSet<string> uniquePriorityCheck = new HashSet<string>();
            bool isUnique = false;
            if (nullCheck == null)
            {
                isUnique = checkedRows.All(x => uniquePriorityCheck.Add(x.Cells[0].Value.ToString()));
            }

            if (nullCheck != null || !isUnique)
            {
                MessageBox.Show("Error: Ensure all selected plugins have a unique priority assigned");
                return;
            }

            //if we've made it here, we know we have a valid priority listing

            //set priority on all enabled plugins
            foreach (DataGridViewRow checkedRow in checkedRows)
            {
                AssemblyInstance enabledPlugin = loadedAutoRuns.FirstOrDefault(x => x.AssemblyName == checkedRow.Cells[1].Value.ToString());
                enabledPlugin.Priority = Int32.Parse(checkedRow.Cells[0].Value.ToString());
            }

            //append any args to plugins that require them
            foreach (DataGridViewRow dataRow in dataGridView1.Rows)
            {
                if (Convert.ToBoolean(dataRow.Cells[2].Value))
                {
                    AssemblyInstance requiresArgs = loadedAutoRuns.FirstOrDefault(x => x.AssemblyName == dataRow.Cells[1].Value.ToString());
                    List<string> dictEnum = requiresArgs.RequiredArgs.Keys.ToList();

                    foreach (string s in dictEnum)
                    {
                        ArgForm populateArg = new ArgForm();
                        populateArg.SetLabel1("Assembly: " + requiresArgs.AssemblyName);
                        populateArg.SetLabel2("Provide a value for: " + s);
                        populateArg.ShowDialog();
                        requiresArgs.RequiredArgs[s] = populateArg.GetText();
                        populateArg.Dispose();
                    }
                }
            }
            this.Close();
        }



        private void assemblyInstanceBindingSource_CurrentChanged(object sender, EventArgs e)
        {

        }
    }
}
