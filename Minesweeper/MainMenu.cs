using AxWMPLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms; 

namespace Minesweeper
{
    public partial class MainMenu : Form
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_CAPTION_COLOR = 35;
        private HowToPlay guide; 
        private Settings settings;

        public MainMenu()
        {
            InitializeComponent();

            Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            label3.Text = "v" + v.Major + "." + v.Minor + "." + v.Build;
            LoadCustomFont();
        }

        public void LoadCustomFont()
        {

            try
            {
                if (Program.pfc.Families.Length > 0)
                {
                    label1.Font = new Font(Program.pfc.Families[0], 30, FontStyle.Regular);
                    label2.Font = new Font(Program.pfc.Families[0], 12, FontStyle.Regular);
                    label3.Font = new Font(Program.pfc.Families[0], 10, FontStyle.Regular);
                    label4.Font = new Font(Program.pfc.Families[0], 13, FontStyle.Regular);
                    NewGameButton.Font = new Font(Program.pfc.Families[0], 11, FontStyle.Regular);
                    HowToPlayButton.Font = new Font(Program.pfc.Families[0], 11, FontStyle.Regular);
                    SettingsButton.Font = new Font(Program.pfc.Families[0], 11, FontStyle.Regular);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Properties.Resources.ErrorMsgBox + ex.Message, "Fatal Error: Font load failed.", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void HowToPlay_Click(object sender, EventArgs e)
        {
            if (guide == null || guide.IsDisposed) 
            {
                guide = new HowToPlay();
                guide.Show();
                Program.PlayYesSound();
            }
            else
            {
                guide.Show();
                guide.Focus();
            }
        }

        private void NewGame_Click(object sender, EventArgs e)
        {
            Program.PlayYesSound();
            DifficultySelector difficultySelector = new DifficultySelector();
            if (guide != null && !guide.IsDisposed)
            {
                guide.Hide();
            }
            if (settings != null && !settings.IsDisposed)
            {
                settings.Hide();
            }
            difficultySelector.Show();
            Hide();
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            if (settings == null || settings.IsDisposed)
            {
                settings = new Settings();
                settings.Show();
                Program.PlayYesSound();
            }
            else
            {
                settings.Show();
                settings.Focus();
            }
        }

        public void MainMenu_Load(object sender, EventArgs e)
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

            Program.PlayMenuBGM();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            pictureBox2.Enabled = false;
            Timer hideTimer = new Timer();
            hideTimer.Interval = 30;
            hideTimer.Tick += (s, args) => {
                if (this.Opacity > 0.05) this.Opacity -= 0.05;
                else
                {
                    hideTimer.Stop();
                    this.Visible = false;
                    pictureBox2.Enabled = true;
                }
            };
            hideTimer.Start();

            NikoRoomba niko = new NikoRoomba(this);

            niko.FormClosed += (s, args) => {
                this.Visible = true;
                this.Opacity = 0; 

                Timer showTimer = new Timer();
                showTimer.Interval = 30;
                showTimer.Tick += (s2, e2) => {
                    if (this.Opacity < 1.0) this.Opacity += 0.05;
                    else showTimer.Stop();
                };
                showTimer.Start();
                Program.PlayMenuBGM();
            };

            niko.Show();

        }
        private void MainMenu_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Program.NikoRiding)
            {
                System.Threading.Thread.Sleep(500);
            }
            Application.Exit();
        }
    }
}
