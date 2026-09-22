using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Minesweeper
{
    public class BSODForm : Form
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszSubStr);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        private Label sadLabel;
        private Label mainLabel;
        private Label percentLabel;
        private Label qrTextLabel;
        private Label stopCodeLabel;
        private PictureBox qrCodeBox;
        private Timer stateTimer;
        private Timer progressTimer;

        private double progress = 0.0;
        private double incrementPerTick;

        private int phase = 0; // 0: icons hide, 1: fade, 2: BSOD

        public BSODForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            TopMost = true;

            BackColor = Color.Magenta;
            TransparencyKey = Color.Magenta;
            AllowTransparency = true;
            KeyPreview = true;

            this.Load += BSODForm_Load;
            this.FormClosed += (s, e) => RestoreDesktop();

            sadLabel = new Label();
            mainLabel = new Label();
            percentLabel = new Label();
            qrTextLabel = new Label();
            stopCodeLabel = new Label();
            qrCodeBox = new PictureBox();

            foreach (var lbl in new[] { sadLabel, mainLabel, percentLabel, qrTextLabel, stopCodeLabel })
            {
                lbl.ForeColor = Color.White;
                lbl.BackColor = Color.Transparent;
                lbl.AutoSize = true;
                lbl.Visible = false;
                Controls.Add(lbl);
            }

            qrCodeBox.Visible = false;
            Controls.Add(qrCodeBox);

            stateTimer = new Timer();
            stateTimer.Tick += StateTimer_Tick;

            progressTimer = new Timer();
            progressTimer.Interval = 50; 
            progressTimer.Tick += ProgressTimer_Tick;
        }

        private void BSODForm_Load(object sender, EventArgs e)
        {
            try { Cursor.Hide(); } catch { }

            HideDesktopIcons();

            stateTimer.Interval = 500;
            stateTimer.Start();
        }

        private void StateTimer_Tick(object sender, EventArgs e)
        {
            stateTimer.Stop();

            if (phase == 0)
            {
                this.AllowTransparency = false;
                BackColor = Color.Black;
                phase = 1;
                stateTimer.Interval = 200;
                stateTimer.Start();
            }
            else if (phase == 1)
            {
                phase = 2;
                SetupBSODLayout();

                int totalMs = 15000;
                incrementPerTick = 100.0 / (totalMs / progressTimer.Interval);
                progress = 0.0;
                progressTimer.Start();
            }
        }

        private void SetupBSODLayout()
        {
            BackColor = Color.FromArgb(0, 120, 215);

            int leftMargin = this.ClientSize.Width / 10;
            int topMargin = this.ClientSize.Height / 8;

            // :(
            sadLabel.Font = new Font("Segoe UI", 122f, FontStyle.Regular, GraphicsUnit.Point);
            sadLabel.Text = ":(";
            sadLabel.Location = new Point(leftMargin, topMargin);
            sadLabel.Visible = true;

            // main text
            mainLabel.Font = new Font("Segoe UI", 29f, FontStyle.Regular, GraphicsUnit.Point);
            mainLabel.Text = Properties.Resources.BsodMain;
            mainLabel.Location = new Point(leftMargin, sadLabel.Bottom + 10);
            mainLabel.Visible = true;

            percentLabel.Font = new Font("Segoe UI", 29f, FontStyle.Regular, GraphicsUnit.Point);
            int currentPercent = 0;
            percentLabel.Text = string.Format(Properties.Resources.BsodPercent, currentPercent);
            percentLabel.Location = new Point(leftMargin, mainLabel.Bottom + 45);
            percentLabel.Visible = true;

            // QR
            qrCodeBox.Size = new Size(145, 145);
            qrCodeBox.Location = new Point(leftMargin, percentLabel.Bottom + 45);
            try
            {
                qrCodeBox.Image = Properties.Resources.QrCode;
                qrCodeBox.SizeMode = PictureBoxSizeMode.Zoom;
            }
            catch
            {
                qrCodeBox.BackColor = Color.White;
            }
            qrCodeBox.Visible = true;

            // link
            qrTextLabel.Font = new Font("Segoe UI", 15f, FontStyle.Regular, GraphicsUnit.Point);
            qrTextLabel.Text = Properties.Resources.BsodLink;
            qrTextLabel.Location = new Point(qrCodeBox.Right + 20, qrCodeBox.Top);
            qrTextLabel.Visible = true;

            // stopcode
            stopCodeLabel.Font = new Font("Segoe UI", 13f, FontStyle.Regular, GraphicsUnit.Point);
            stopCodeLabel.Text = Properties.Resources.BsodStopcode;
            stopCodeLabel.Location = new Point(qrCodeBox.Right + 20, qrTextLabel.Bottom + 15);
            stopCodeLabel.Visible = true;
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            progress += incrementPerTick;
            if (progress >= 100)
            {
                progress = 100;
                progressTimer.Stop();
                percentLabel.Text = string.Format(Properties.Resources.BsodPercent, 100);

                var endTimer = new Timer();
                endTimer.Interval = 800;
                sadLabel.Text = ":)";
                endTimer.Tick += (s, ev) =>
                {
                    endTimer.Stop();
                    RestoreDesktop();
                    Application.Exit();
                };
                endTimer.Start();
                return;
            }
            percentLabel.Text = string.Format(Properties.Resources.BsodPercent, (int)progress);
        }

        private void RestoreDesktop()
        {
            try { Cursor.Show(); } catch { }
            ShowDesktopIcons();
        }

        #region Win32 Desktop Icon Helpers
        private static IntPtr GetDesktopListViewHandle()
        {
            IntPtr progman = FindWindow("Progman", null);
            IntPtr defView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (defView == IntPtr.Zero)
            {
                IntPtr workerw = IntPtr.Zero;
                do
                {
                    workerw = FindWindowEx(IntPtr.Zero, workerw, "WorkerW", null);
                    if (workerw != IntPtr.Zero)
                    {
                        defView = FindWindowEx(workerw, IntPtr.Zero, "SHELLDLL_DefView", null);
                    }
                } while (defView == IntPtr.Zero && workerw != IntPtr.Zero);
            }
            return FindWindowEx(defView, IntPtr.Zero, "SysListView32", null);
        }

        private static void HideDesktopIcons()
        {
            try
            {
                IntPtr hWnd = GetDesktopListViewHandle();
                if (hWnd != IntPtr.Zero) ShowWindow(hWnd, SW_HIDE);
            }
            catch { }
        }

        private static void ShowDesktopIcons()
        {
            try
            {
                IntPtr hWnd = GetDesktopListViewHandle();
                if (hWnd != IntPtr.Zero) ShowWindow(hWnd, SW_SHOW);
            }
            catch { }
        }
        #endregion

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                RestoreDesktop();
                Application.Exit();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}