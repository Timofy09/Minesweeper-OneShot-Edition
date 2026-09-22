using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Minesweeper
{
    public partial class CutsceneForm : Form
    {
        private List<Image> frames = new List<Image>();
        private Image currentFrame;
        private Image nextFrame;
        private float alpha = 0.0f;
        private bool canClose = false;

        public CutsceneForm()
        {
            InitializeComponent();

            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ControlBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(640, 480);
            this.BackColor = Color.Black;
            this.DoubleBuffered = true;
            this.Text = "";

            this.Shown += CutsceneForm_Shown;

            frames.Add(LoadEmbeddedImage("Cg_dream1_1.png"));
            frames.Add(LoadEmbeddedImage("Cg_dream1_2.png"));
            frames.Add(LoadEmbeddedImage("Cg_dream1_3.png"));
            frames.Add(LoadEmbeddedImage("Cg_dream1_4.png"));
        }

        private Image LoadEmbeddedImage(string fileName)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string resourcePath = $"{typeof(Program).Namespace}.Resources.{fileName}";
                using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
                {
                    if (stream != null) return Image.FromStream(stream);
                }
            }
            catch { }

            try
            {
                string resName = Path.GetFileNameWithoutExtension(fileName);
                object obj = Properties.Resources.ResourceManager.GetObject(resName);
                if (obj is Image img) return img;
            }
            catch { }

            return new Bitmap(640, 480);
        }

        private async void CutsceneForm_Shown(object sender, EventArgs e)
        {
            if (frames.Count == 0) return;

            Program.PlayMusic("Distant.mp3", true);

            for (int i = 0; i < frames.Count; i++)
            {
                if (i == 0)
                {
                    currentFrame = frames[0];
                    this.Invalidate();
                    await Task.Delay(6500); 
                }
                else if (i == 3)
                {
                    await CrossfadeTo(frames[i]);
                    await Task.Delay(7000);
                }
                else
                {
                    await CrossfadeTo(frames[i]);
                    await Task.Delay(6500);
                }
            }

            canClose = true;
            this.Close();
        }

        private async Task CrossfadeTo(Image newFrame)
        {
            nextFrame = newFrame;

            for (int step = 0; step <= 20; step++)
            {
                alpha = step / 20.0f;
                this.Invalidate();
                await Task.Delay(25);
            }

            currentFrame = nextFrame;
            nextFrame = null;
            alpha = 0.0f;
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (currentFrame != null)
            {
                DrawCenteredImage(e.Graphics, currentFrame, 1.0f);
            }

            if (nextFrame != null && alpha > 0)
            {
                DrawCenteredImage(e.Graphics, nextFrame, alpha);
            }
        }

        private void DrawCenteredImage(Graphics g, Image img, float opacity)
        {
            float scale = Math.Min((float)this.ClientSize.Width / img.Width, (float)this.ClientSize.Height / img.Height);
            int width = (int)(img.Width * scale);
            int height = (int)(img.Height * scale);
            int x = (this.ClientSize.Width - width) / 2;
            int y = (this.ClientSize.Height - height) / 2;

            Rectangle destRect = new Rectangle(x, y, width, height);

            ColorMatrix matrix = new ColorMatrix();
            matrix.Matrix33 = opacity;

            using (ImageAttributes attr = new ImageAttributes())
            {
                attr.SetColorMatrix(matrix);
                g.DrawImage(img, destRect, 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, attr);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!canClose)
            {
                e.Cancel = true;
                return;
            }

            try { Program.StopBGM(); } catch { }

            base.OnFormClosing(e);
        }
    }
}