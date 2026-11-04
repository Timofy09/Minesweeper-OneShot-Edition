using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Minesweeper
{
    public partial class HowToPlay : Form
    {
        public HowToPlay()
        {
            InitializeComponent();
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
            Program.PlayNoSound();
        }

        private void HowToPlay_Load(object sender, EventArgs e)
        {
            label1.Font = new Font(Program.pfc.Families[0], 13, FontStyle.Regular);
            button1.Font = new Font(Program.pfc.Families[0], 14, FontStyle.Regular);
        }
    }
}
