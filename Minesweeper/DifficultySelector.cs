using AxWMPLib;
using Minesweeper.Properties;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Minesweeper
{
    public partial class DifficultySelector : Form
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_CAPTION_COLOR = 35;
        private bool suppressScrollSound = false;
        public static bool maxDifficulty = false;

        public DifficultySelector()
        {
            InitializeComponent();
            this.Shown += DifficultySelector_Shown;
        }

        private void DifficultySelector_Shown(object sender, EventArgs e)
        {
            label1.Font = new Font(Program.pfc.Families[0], 22, FontStyle.Regular);
            label2.Font = new Font(Program.pfc.Families[0], 20, FontStyle.Regular);
            label3.Font = new Font(Program.pfc.Families[0], 12, FontStyle.Regular);
            label4.Font = new Font(Program.pfc.Families[0], 19, FontStyle.Regular);
            label5.Font = new Font(Program.pfc.Families[0], 19, FontStyle.Regular);
            LaunchButton.Font = new Font(Program.pfc.Families[0], 15, FontStyle.Regular);
            comboBox1.Font = new Font(Program.pfc.Families[0], 13, FontStyle.Regular);
            checkBox1.Font = new Font(Program.pfc.Families[0], 15, FontStyle.Regular);

            suppressScrollSound = true;

            comboBox1.Items.Clear();
            comboBox1.Items.Add(Properties.Resources.Barrens);
            comboBox1.Items.Add(Properties.Resources.Glen);
            comboBox1.Items.Add(Properties.Resources.Refuge);
            comboBox1.Items.Add(Properties.Resources.The_Tower);
            Program.Theme = 0;
            checkBox1.Text = Properties.Resources.Auto_Theme;
            comboBox1.SelectedIndex = 0;

            if (Program.MapWidth == 9 && Program.MapHeight == 9) difficultyTrackBar.Value = 0;
            else if (Program.MapWidth == 16 && Program.MapHeight == 16) difficultyTrackBar.Value = 1;
            else if (Program.MapWidth == 30 && Program.MapHeight == 16) difficultyTrackBar.Value = 2;
            else if (Program.MapWidth == 35 && Program.MapHeight == 25) difficultyTrackBar.Value = 3;
            else difficultyTrackBar.Value = 0;

            suppressScrollSound = false;
            UpdateDifficultyUI();
        }

        private void difficultyTrackBar_Scroll(object sender, EventArgs e)
        {
            UpdateDifficultyUI();
        }

        private void UpdateDifficultyUI()
        {
            if (!suppressScrollSound)
            {
                Program.PlayTickSound();
            }

            switch (difficultyTrackBar.Value)
            {
                case 0:
                    SetDifficulty(9, 9, 10, Properties.Resources.Difficulty0Name, Properties.Resources.Difficulty0Description, Color.MediumVioletRed, Color.White, 0, false);
                    break;
                case 1:
                    SetDifficulty(16, 16, 40, Properties.Resources.Difficulty1Name, Properties.Resources.Difficulty1Description, Color.MediumVioletRed, Color.White, 1, false);
                    break;
                case 2:
                    SetDifficulty(30, 16, 99, Properties.Resources.Difficulty2Name, Properties.Resources.Difficulty2Description, Color.MediumVioletRed, Color.White, 2, false);
                    break;
                case 3:
                    SetDifficulty(35, 25, 250, Properties.Resources.Difficulty3Name, Properties.Resources.Difficulty3Description, Color.Red, Color.Red, 3, true);
                    break;
            }
        }

        private void SetDifficulty(int w, int h, int m, string title, string desc, Color trackBarAndLabelColor, Color textColor, int theme, bool isMax)
        {
            Program.MapWidth = w;
            Program.MapHeight = h;
            Program.MinesCount = m;

            difficultyTrackBar.BackColor = trackBarAndLabelColor;
            maxDifficulty = isMax;

            label2.ForeColor = textColor;
            label2.Text = title;

            label3.ForeColor = textColor;
            label3.Text = desc;

            label4.ForeColor = textColor;
            label4.Text = Properties.Resources.Field + ": " + w + "x" + h;

            label5.ForeColor = textColor;
            label5.Text = Properties.Resources.Mines + ": " + m;

            checkBox1.ForeColor = textColor;

            if (checkBox1.Checked)
            {
                comboBox1.SelectedIndex = theme;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!checkBox1.Checked)
            {
                Program.Theme = comboBox1.SelectedIndex;
            }
        }

        private void LaunchButton_Click(object sender, EventArgs e)
        {
            Program.Theme = comboBox1.SelectedIndex;
            Program.StopBGM();
            Program.PlayStartSound();
            new GameForm().Show();
            this.Hide();
        }

        private void DifficultySelector_FormClosed(object sender, FormClosedEventArgs e)
        {
            new MainMenu().Show();
            Program.PlayNoSound();
        }

        private void DifficultySelector_Load(object sender, EventArgs e)
        {
            try
            {
                int titleBarColor = 0x001A0218;
                DwmSetWindowAttribute(this.Handle, DWMWA_CAPTION_COLOR, ref titleBarColor, sizeof(int));
                this.Refresh();
            }
            catch
            {
                // This won't work on systems except Win11
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Program.PlayOpenSound();
            comboBox1.Enabled = !checkBox1.Checked;
        }
    }
}