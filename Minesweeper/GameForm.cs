using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing.Text;
using Minesweeper.Properties;
using System.Threading;
using System.Collections.Generic;

namespace Minesweeper
{
    public partial class GameForm : Form
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_CAPTION_COLOR = 35;

        int width = Program.MapWidth;
        int height = Program.MapHeight;
        int minesCount = Program.MinesCount;

        int[,] map;
        bool[,] revealed;
        bool[,] flagged;

        bool firstClick = true;
        bool gameOver = false;

        int cellSize = 25;

        private System.Windows.Forms.Timer gameTimer;
        private int elapsedSeconds = 0;
        private int totalClicks = 0;

        public GameForm()
        {
            InitializeComponent();

            this.DoubleBuffered = true;

            map = new int[width, height];
            revealed = new bool[width, height];
            flagged = new bool[width, height];

            this.ClientSize = new Size(width * cellSize, height * cellSize);
            this.MouseDown += GameForm_MouseDown;
            this.Paint += GameForm_Paint;

            gameTimer = new System.Windows.Forms.Timer();
            gameTimer.Interval = 1000;
            gameTimer.Tick += (s, e) => { elapsedSeconds++; };
        }

        public void GameForm_Load(object sender, EventArgs e)
        {
            Program.PlayGameBGM();

            int titleBarColor = 0;

            switch (Program.Theme)
            {
                case 0: titleBarColor = 0x002B1114; break;
                case 1: titleBarColor = 0x004F3B2C; break;
                case 2: titleBarColor = 0x00210A1D; break;
                case 3: titleBarColor = 0x00000000; break;
            }

            try
            {
                DwmSetWindowAttribute(this.Handle, DWMWA_CAPTION_COLOR, ref titleBarColor, sizeof(int));
            }
            catch { }
        }

        void GenerateMines(int safeX, int safeY)
        {
            Random rnd = new Random();
            int placed = 0;

            while (placed < minesCount)
            {
                int x = rnd.Next(width);
                int y = rnd.Next(height);

                bool isSafeZone = (x >= safeX - 1 && x <= safeX + 1) &&
                                  (y >= safeY - 1 && y <= safeY + 1);
                if (map[x, y] == -1 || isSafeZone) continue;

                map[x, y] = -1;
                placed++;
            }

            CalculateNumbers();
        }

        void CalculateNumbers()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (map[x, y] == -1) continue;

                    int count = 0;

                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;

                            if (nx >= 0 && ny >= 0 && nx < width && ny < height)
                                if (map[nx, ny] == -1)
                                    count++;
                        }

                    map[x, y] = count;
                }
            }
        }

        bool OpenCells(int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
                return false;

            if (revealed[x, y] || flagged[x, y])
                return false;

            revealed[x, y] = true;

            if (map[x, y] == 0)
            {
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        OpenCells(x + dx, y + dy);
            }

            CheckWin();
            return true;
        }

        void RevealAllMines()
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (map[x, y] == -1)
                        revealed[x, y] = true;
        }

        private int Calculate3BV()
        {
            bool[,] visited = new bool[width, height];
            int bsv = 0;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (map[x, y] == 0 && !visited[x, y] && map[x, y] != -1)
                    {
                        bsv++;
                        RevealZeroAreaFor3BV(x, y, visited);
                    }
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (map[x, y] > 0 && !visited[x, y] && map[x, y] != -1)
                    {
                        bsv++;
                    }
                }
            }

            return Math.Max(1, bsv);
        }

        private void RevealZeroAreaFor3BV(int startX, int startY, bool[,] visited)
        {
            Queue<Point> queue = new Queue<Point>();
            queue.Enqueue(new Point(startX, startY));
            visited[startX, startY] = true;

            while (queue.Count > 0)
            {
                Point p = queue.Dequeue();

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = p.X + dx;
                        int ny = p.Y + dy;

                        if (nx >= 0 && ny >= 0 && nx < width && ny < height && !visited[nx, ny] && map[nx, ny] != -1)
                        {
                            visited[nx, ny] = true;
                            if (map[nx, ny] == 0)
                            {
                                queue.Enqueue(new Point(nx, ny));
                            }
                        }
                    }
                }
            }
        }

        private string GetScoreFilePath()
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MinesweeperOneShot");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, "scores.txt");
        }

        private int LoadBestTime()
        {
            try
            {
                string path = GetScoreFilePath();
                if (File.Exists(path))
                {
                    string[] lines = File.ReadAllLines(path);
                    string key = $"{width}x{height}_{minesCount}";
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split(':');
                        if (parts.Length == 2 && parts[0] == key)
                        {
                            return int.Parse(parts[1]);
                        }
                    }
                }
            }
            catch { }
            return -1;
        }

        private bool SaveBestTime(int newTime)
        {
            try
            {
                string path = GetScoreFilePath();
                int currentBest = LoadBestTime();

                if (currentBest == -1 || newTime < currentBest)
                {
                    string key = $"{width}x{height}_{minesCount}";
                    List<string> updatedLines = new List<string>();
                    bool updated = false;

                    if (File.Exists(path))
                    {
                        foreach (string line in File.ReadAllLines(path))
                        {
                            string[] parts = line.Split(':');
                            if (parts.Length == 2 && parts[0] == key)
                            {
                                updatedLines.Add($"{key}:{newTime}");
                                updated = true;
                            }
                            else
                            {
                                updatedLines.Add(line);
                            }
                        }
                    }

                    if (!updated)
                    {
                        updatedLines.Add($"{key}:{newTime}");
                    }

                    File.WriteAllLines(path, updatedLines.ToArray());
                    return true;
                }
            }
            catch { }
            return false;
        }

        void ShowEndDialog(string Message, MessageBoxIcon IconType)
        {
            Program.StopBGM();
            if (IconType == MessageBoxIcon.Warning)
            {
                ShakeWindow(800, 18);
            }

            var result = MessageBox.Show(Message, Properties.Resources.World_sFate, MessageBoxButtons.YesNo, IconType);

            if (result == DialogResult.Yes)
            {
                Program.PlayYesSound();
                gameOver = false;
                RestartGame();
            }
            else
            {
                Program.PlayNoSound();
                Program.NikoJumpscareDone = false;
                this.Close();
            }
        }

        async void ShakeWindow(int duration = 600, int startAmplitude = 15)
        {
            Point originalLocation = this.Location;
            Random rnd = new Random();
            int elapsed = 0;
            int interval = 10;

            while (elapsed < duration)
            {
                double progress = (double)elapsed / duration;
                int currentAmplitude = (int)(startAmplitude * (1.0 - progress));

                if (currentAmplitude <= 0) break;

                int shakeX = rnd.Next(-currentAmplitude, currentAmplitude + 1);
                int shakeY = rnd.Next(-currentAmplitude, currentAmplitude + 1);

                this.Location = new Point(originalLocation.X + shakeX, originalLocation.Y + shakeY);

                await Task.Delay(interval);
                elapsed += interval;
            }
            this.Location = originalLocation;
        }

        async void NikoJumpscare()
        {
            try
            {
                if (!Program.NikoJumpscareDone)
                {
                    System.Drawing.Bitmap bitmap = Properties.Resources.niko_message;
                    string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "niko_message.png");
                    bitmap.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);
                    Process p = Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
                    Program.NikoJumpscareDone = true;

                    await Task.Run(() => {
                        p?.WaitForExit();
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Properties.Resources.ErrorMsgBox + ex.Message, "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void CheckWin()
        {
            if (gameOver) return;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (map[x, y] != -1 && !revealed[x, y])
                        return;
                }
            }

            gameTimer.Stop();
            RevealAllMines();
            Invalidate();

            string statsMessage = "";
            int finalTime = Math.Max(1, elapsedSeconds);
            bool isNewRecord = SaveBestTime(finalTime);
            int bsv = Calculate3BV();
            double bsvSpeed = (double)bsv / finalTime;

            int placedFlags = 0;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (flagged[x, y]) placedFlags++;


            if (Program.Language == "en")
            {
                    statsMessage =
                    (isNewRecord ? "🏆 NEW RECORD!\n" : "") +
                    $"Field: {width}x{height} / {minesCount} mines\n" +
                    $"Time: {finalTime / 60:D2}:{finalTime % 60:D2} ({finalTime} sec)\n" +
                    $"Flags: {placedFlags} / {minesCount}\n" +
                    $"Clicks: {totalClicks}\n" +
                    $"3BV / 3BV/s: {bsv} / {bsvSpeed:F2}\n\n";
            }
            else if (Program.Language == "ru")
            {
                statsMessage =
                (isNewRecord ? "🏆 НОВЫЙ РЕКОРД!\n" : "") +
                $"Поле: {width}x{height} / {minesCount} мин\n" +
                $"Время: {finalTime / 60:D2}:{finalTime % 60:D2} ({finalTime} сек)\n" +
                $"Флаги: {placedFlags} / {minesCount}\n" +
                $"Клики: {totalClicks}\n" +
                $"3BV / 3BV/s: {bsv} / {bsvSpeed:F2}\n\n";
            }
            
            this.BeginInvoke(new Action(() =>
            {
                Program.PlayWinSound();
                ShowEndDialog( Properties.Resources.EndDialogWin1 + " \n\n" + statsMessage + Properties.Resources.EndDialogWin2, MessageBoxIcon.Asterisk);
            }));
        }

        void RestartGame()
        {
            map = new int[width, height];
            revealed = new bool[width, height];
            flagged = new bool[width, height];

            firstClick = true;
            gameOver = false;
            elapsedSeconds = 0;
            totalClicks = 0;
            gameTimer.Stop();

            Program.PlayGameBGM();
            Invalidate();
        }

        bool TryOpenAroundNumber(int x, int y)
        {
            int flagsAround = 0;

            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx >= 0 && ny >= 0 && nx < width && ny < height)
                        if (flagged[nx, ny])
                            flagsAround++;
                }

            if (flagsAround == map[x, y])
            {
                bool anyOpened = false;

                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;

                        if (nx >= 0 && ny >= 0 && nx < width && ny < height)
                        {
                            if (!flagged[nx, ny] && !revealed[nx, ny])
                            {
                                if (map[nx, ny] == -1)
                                {
                                    GameOver();
                                    gameOver = true;
                                }

                                if (OpenCells(nx, ny))
                                    anyOpened = true;
                            }
                        }
                    }

                return anyOpened;
            }

            return false;
        }

        private void GameOver()
        {
            gameTimer.Stop();
            Program.StopBGM();
            Program.PlayLoseSound();
            Random random = new Random();
            if (random.Next(1, 5) == 1)
            {
                NikoJumpscare();
            }
            gameOver = true;
            Program.Scare1 = false;
            RevealAllMines();
            Invalidate();
            this.BeginInvoke(new Action(() => ShowEndDialog(Properties.Resources.EndDialogLose1 + Program.UserName + ". \n\n" + Properties.Resources.EndDialogLose2, MessageBoxIcon.Warning)));
        }

        private void GameForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (gameOver) return;

            int x = e.X / cellSize;
            int y = e.Y / cellSize;

            if (x < 0 || y < 0 || x >= width || y >= height)
                return;

            if (e.Button == MouseButtons.Left)
            {
                if (revealed[x, y] && map[x, y] > 0)
                {
                    totalClicks++;
                    if (TryOpenAroundNumber(x, y))
                    {
                        Program.PlayOpenSound();
                    }
                }
                else if (!revealed[x, y] && !flagged[x, y])
                {
                    totalClicks++;
                    if (firstClick)
                    {
                        GenerateMines(x, y);
                        firstClick = false;
                        gameTimer.Start();
                    }

                    if (map[x, y] == -1)
                    {
                        GameOver();
                    }
                    else
                    {
                        if (OpenCells(x, y))
                        {
                            Program.PlayOpenSound();
                        }
                    }
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (!revealed[x, y])
                {
                    totalClicks++;
                    flagged[x, y] = !flagged[x, y];
                    CheckWin();
                }
            }

            Invalidate();
        }

        private void GameForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            Font flagFont = new Font("Segoe UI Emoji", 14);
            Font font = new Font(Program.pfc.Families[0], 21, FontStyle.Bold);

            Color openColor = Color.White;
            Color closedColor = Color.LightGray;
            Color gridColor = Color.Gray;

            switch (Program.Theme)
            {
                case 0:
                    closedColor = Color.FromArgb(20, 17, 43);
                    openColor = Color.FromArgb(58, 54, 113);
                    gridColor = Color.FromArgb(99, 95, 188);
                    break;

                case 1:
                    closedColor = Color.FromArgb(44, 59, 79);
                    openColor = Color.FromArgb(14, 93, 65);
                    gridColor = Color.FromArgb(22, 139, 112);
                    break;

                case 2:
                    closedColor = Color.FromArgb(29, 10, 33);
                    openColor = Color.FromArgb(94, 31, 67);
                    gridColor = Color.FromArgb(230, 62, 111);
                    break;

                case 3:
                    closedColor = Color.FromArgb(0, 0, 0);
                    openColor = Color.FromArgb(50, 20, 90);
                    gridColor = Color.FromArgb(150, 100, 255);
                    break;
            }

            using (SolidBrush openBrush = new SolidBrush(openColor))
            using (SolidBrush closedBrush = new SolidBrush(closedColor))
            using (Pen gridPen = new Pen(gridColor, 2))
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Rectangle rect = new Rectangle(x * cellSize, y * cellSize, cellSize, cellSize);

                        if (revealed[x, y])
                        {
                            g.FillRectangle(openBrush, rect);

                            if (map[x, y] == -1)
                            {
                                Brush mineBrush = (Program.Theme == 3) ? Brushes.Gold : Brushes.Black;
                                g.FillEllipse(mineBrush, rect.X + 5, rect.Y + 5, cellSize - 10, cellSize - 10);
                            }
                            else if (map[x, y] > 0)
                            {
                                Brush color = Brushes.White;

                                if (Program.Theme == 3)
                                {
                                    color = (map[x, y] % 2 == 0) ? Brushes.Red : Brushes.Orange;
                                }
                                else
                                {
                                    if (map[x, y] == 1) color = Brushes.DeepSkyBlue;
                                    else if (map[x, y] == 2) color = Brushes.LightGreen;
                                    else if (map[x, y] == 3) color = Brushes.Tomato;
                                    else if (map[x, y] == 4) color = Brushes.CornflowerBlue;
                                    else if (map[x, y] == 5) color = Brushes.Crimson;
                                    else if (map[x, y] == 6) color = Brushes.Cyan;
                                    else if (map[x, y] == 7) color = Brushes.White;
                                    else if (map[x, y] == 8) color = Brushes.DarkGray;
                                    else if (map[x, y] == 9) color = Brushes.Violet;
                                }

                                g.DrawString(map[x, y].ToString(), font, color, rect.X + 1, rect.Y - 1);
                            }
                        }
                        else
                        {
                            g.FillRectangle(closedBrush, rect);

                            if (flagged[x, y])
                            {
                                g.DrawString("🚩", flagFont, Brushes.Red, rect.X - 2, rect.Y);
                            }
                        }

                        g.DrawRectangle(gridPen, rect);
                    }
                }
            }
        }

        private void GameForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            gameTimer?.Stop();
            Program.NikoJumpscareDone = false;
            Program.Scare1 = false;
            Program.PlayNoSound();
            foreach (Form f in Application.OpenForms)
            {
                if (f is MainMenu menu)
                {
                    menu.Show();
                    if (Program.MusicEnabled)
                    {
                        menu.MainMenu_Load(null, null);
                    }
                    break;
                }
            }
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            Random random = new Random();
            DateTime now = DateTime.Now;

            if (!Program.NightMsgBox && now.Hour >= 1 && now.Hour < 5)
            {
                Program.NightMsgBox = true;
                await Task.Delay(10000);
                Program.GoodnightNiko(Properties.Resources.NightMsg, Properties.Resources.Niko);
            }

            if (!Program.Scare1)
            {
                if (random.Next(1, 100) == 1)
                {
                    this.Text = Properties.Resources.Scare1 + Program.UserName + "?";
                    await Task.Delay(1000);
                    this.Text = Properties.Resources.DefaultGameFormName;
                    if (random.Next(1, 4) == 1)
                    Program.Scare1 = true;
                }
            }
        }
    }
}