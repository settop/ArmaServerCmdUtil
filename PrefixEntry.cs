using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ArmaServerCmdUtil
{
    public partial class PrefixEntry : Form
    {        
        public PrefixEntry(string initalPrefix)
        {
            InitializeComponent();

            prefixInput.Text = initalPrefix;
        }

        public string GetPrefix()
        {
            return prefixInput.Text;
        }

        private void okBtn_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
