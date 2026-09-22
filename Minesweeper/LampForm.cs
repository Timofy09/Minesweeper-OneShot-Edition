using System;
using System.Drawing;
using System.Windows.Forms;

namespace Minesweeper
{
    public class LampForm : Form
    {
        private PictureBox pb;
        private Timer timer;
        private int state = 0;

        public LampForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.Black;
            TransparencyKey = Color.Black;
            AllowTransparency = true;

            pb = new PictureBox();
            pb.SizeMode = PictureBoxSizeMode.AutoSize;
            Controls.Add(pb);

            timer = new Timer();
            timer.Interval = 1000; 
            timer.Tick += Timer_Tick;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            try
            {
                var img = Properties.Resources.Lightbulb;
                if (img != null)
                {
                    pb.Image = img;
                    this.ClientSize = pb.Size;
                }
                else
                {
                    this.ClientSize = new Size(200, 200);
                }
            }
            catch
            {
                this.ClientSize = new Size(200, 200);
            }

            var screen = Screen.PrimaryScreen.Bounds;
            int x = screen.Left + (screen.Width - this.Width) / 2;
            int y = screen.Top + (screen.Height - this.Height) / 2;
            this.Location = new Point(x, y);

            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (state == 0)
                {
                    try
                    {
                        pb.Image = Properties.Resources.LightbulbBroken;
                    }
                    catch { }

                    try { Program.PlayLoseSound(); } catch { }

                    state = 1;
                    return;
                }

                if (state == 1)
                {
                    timer.Stop();
                    this.Close();
                }
            }
            catch
            {
                timer.Stop();
                this.Close();
            }
        }
    }
}