using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Minesweeper
{
    public partial class NikoRoomba : Form
    {
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;

        private Timer storyTimer;
        private Timer moveTimer;
        private Timer fadeTimer;
        private Timer directionTimer; // Таймер для частой смены направления

        private int secondsPassed = 0;
        private Random random = new Random();

        private PictureBox nikoBox;
        private Rectangle screenBounds;
        private Point startLocation;
        private double exactX;
        private double exactY;
        private double baseSpeed = 3.5;
        private double currentSpeedX = 0;
        private double currentSpeedY = 0;
        private double targetSpeedX = 0;
        private double targetSpeedY = 0;

        private double targetOpacity = 1.0;

        public NikoRoomba(Form parentForm)
        {
            this.Opacity = 0;
            fadeTimer = new Timer();
            fadeTimer.Interval = 75;
            fadeTimer.Tick += (s, e) => {
                if (this.Opacity < targetOpacity) this.Opacity += 0.05;
                else fadeTimer.Stop();
            };
            fadeTimer.Start();

            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.DoubleBuffered = true;
            this.Text = Properties.Resources.NikoOnRoomba;

            this.AllowTransparency = true;
            this.BackColor = Color.Lime;
            this.TransparencyKey = Color.Lime;

            nikoBox = new PictureBox();
            nikoBox.Size = new Size(48, 64);
            nikoBox.SizeMode = PictureBoxSizeMode.Zoom;
            nikoBox.BackColor = Color.Transparent;
            this.Controls.Add(nikoBox);

            this.Size = nikoBox.Size;

            Screen currentScreen = Screen.FromControl(parentForm);
            screenBounds = currentScreen.WorkingArea;

            int startX = screenBounds.X + (screenBounds.Width / 2) - (this.Width / 2);
            int startY = screenBounds.Y + screenBounds.Height - this.Height;
            startLocation = new Point(startX, startY);
            this.Location = startLocation;

            exactX = startX;
            exactY = startY;

            nikoBox.Image = LoadEmbeddedImage("NikoRoombaDown.png");

            storyTimer = new Timer();
            storyTimer.Interval = 1000;
            storyTimer.Tick += StoryTimer_Tick;

            moveTimer = new Timer();
            moveTimer.Interval = 10;
            moveTimer.Tick += MoveTimer_Tick;

            // Настраиваем таймер частых поворотов (каждые 700 мс меняет направление)
            directionTimer = new Timer();
            directionTimer.Interval = 700;
            directionTimer.Tick += (s, e) => {
                if (secondsPassed > 11 && secondsPassed < 75)
                {
                    ChooseRandomDirection();
                }
            };

            this.FormClosing += NikoRoomba_FormClosing;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            int initialStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
            SetWindowLong(this.Handle, GWL_EXSTYLE, initialStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);

            StartScene();
        }

        private void StartScene()
        {
            secondsPassed = 0;
            Program.NikoRiding = true;
            Program.StopBGM();

            Program.PlayMusic("IT'S TIME TO FIGHT CRIME.mp3", false);
            if (Program.MusicEnabled)
            {
                Program.bgmPlayer.settings.volume = 70;
            }

            File.WriteAllText(Program.GetNikoDataPath(), "busy");

            storyTimer.Start();
            moveTimer.Start();
            directionTimer.Start();
        }

        private void MoveTimer_Tick(object sender, EventArgs e)
        {
            if (secondsPassed < 11)
            {
                int shakeX = random.Next(-1, 1);
                int shakeY = random.Next(-1, 1);
                this.Location = new Point(startLocation.X + shakeX, startLocation.Y + shakeY);
                exactX = this.Left;
                exactY = this.Top;
                return;
            }

            currentSpeedX += (targetSpeedX - currentSpeedX) * 0.1;
            currentSpeedY += (targetSpeedY - currentSpeedY) * 0.1;

            exactX += currentSpeedX;
            exactY += currentSpeedY;

            this.Left = (int)Math.Round(exactX);
            this.Top = (int)Math.Round(exactY);

            bool hitWall = false;

            if (this.Left <= screenBounds.Left)
            {
                this.Left = screenBounds.Left + 4;
                exactX = this.Left;
                targetSpeedX = baseSpeed;
                targetSpeedY = 0;
                hitWall = true;
            }
            else if (this.Right >= screenBounds.Right)
            {
                this.Left = screenBounds.Right - this.Width - 4;
                exactX = this.Left;
                targetSpeedX = -baseSpeed;
                targetSpeedY = 0;
                hitWall = true;
            }

            if (this.Top <= screenBounds.Top)
            {
                this.Top = screenBounds.Top + 4;
                exactY = this.Top;
                targetSpeedY = baseSpeed;
                targetSpeedX = 0;
                hitWall = true;
            }
            else if (this.Bottom >= screenBounds.Bottom)
            {
                if (secondsPassed < 75)
                {
                    this.Top = screenBounds.Bottom - this.Height - 4;
                    exactY = this.Top;
                    targetSpeedY = -baseSpeed;
                    targetSpeedX = 0;
                    hitWall = true;
                }
            }

            if (hitWall)
            {
                UpdateSprite();
            }
        }

        private void StoryTimer_Tick(object sender, EventArgs e)
        {
            secondsPassed++;

            if (secondsPassed == 11)
            {
                this.Location = startLocation;
                exactX = startLocation.X;
                exactY = startLocation.Y;

                targetSpeedX = random.Next(0, 2) == 0 ? baseSpeed : -baseSpeed;
                targetSpeedY = 0;
                UpdateSprite();
            }

            if (secondsPassed == 75)
            {
                targetSpeedX = 0;
                targetSpeedY = baseSpeed;
                UpdateSprite();
            }

            if (secondsPassed >= 90)
            {
                storyTimer.Stop();
                moveTimer.Stop();
                directionTimer.Stop();
                Program.StopBGM();

                string path = Program.GetNikoDataPath();
                if (File.Exists(path)) File.Delete(path);
                Program.NikoRiding = false;
                this.Close();
            }
        }

        private void ChooseRandomDirection()
        {
            int roll = random.Next(0, 4);
            switch (roll)
            {
                case 0: targetSpeedX = baseSpeed; targetSpeedY = 0; break;
                case 1: targetSpeedX = -baseSpeed; targetSpeedY = 0; break;
                case 2: targetSpeedX = 0; targetSpeedY = -baseSpeed; break;
                case 3: targetSpeedX = 0; targetSpeedY = baseSpeed; break;
            }
            UpdateSprite();
        }

        private void UpdateSprite()
        {
            string imageName = "NikoRoombaDown.png";

            if (targetSpeedX > 0) imageName = "NikoRoombaRight.png";
            else if (targetSpeedX < 0) imageName = "NikoRoombaLeft.png";
            else if (targetSpeedY < 0) imageName = "NikoRoombaUp.png";
            else if (targetSpeedY > 0) imageName = "NikoRoombaDown.png";

            Image newImg = LoadEmbeddedImage(imageName);
            if (newImg != null)
            {
                nikoBox.Image?.Dispose();
                nikoBox.Image = newImg;
            }
        }

        private Image LoadEmbeddedImage(string fileName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string namespaceName = typeof(NikoRoomba).Namespace;
            string resourcePath = $"{namespaceName}.Resources.Sprites.{fileName}";

            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream == null) return null;
                return Image.FromStream(stream);
            }
        }

        async void NikoRoomba_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Program.NikoRiding)
            {
                Program.PlayLoseSound();
                await Task.Delay(500);
                Application.Exit();
            }
        }
    }
}