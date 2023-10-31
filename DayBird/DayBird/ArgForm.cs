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
    public partial class ArgForm : Form
    {
        public ArgForm()
        {
            InitializeComponent();
            this.textBox1.KeyPress += new System.Windows.Forms.KeyPressEventHandler(CheckEnterKeyPress);
        }
        private void CheckEnterKeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                this.Close();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        public void SetLabel1(string text)
        {
            label1.Text = text;
        }

        public void SetLabel2(string text)
        {
            label2.Text = text;
        }

        public string GetText()
        {
            return textBox1.Text;
        }
    }
}
