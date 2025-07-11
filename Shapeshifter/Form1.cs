using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Shapeshifter
{
    public partial class ShapeShifter : Form
    {
        // Cancellation token source for stopping the algorithm
        private CancellationTokenSource solveCancellationTokenSource = null;

        public ShapeShifter()
        {
            InitializeComponent();
        }

        // Stub event handlers (remove if you have your own)
        private void textBox1_TextChanged(object sender, EventArgs e) { }
        private void label1_Click(object sender, EventArgs e) { }
        private void label1_Click_1(object sender, EventArgs e) { }
        private void label1_Click_2(object sender, EventArgs e) { }
        private void Form1_Load(object sender, EventArgs e) { }

        private string ParseGridLayout(string html)
        {
            var regex = new Regex(@"mouseon\((\d+),(\d+)\)""[^>]*>\s*<img[^>]+/([^/_]+)_\d+\.gif", RegexOptions.IgnoreCase);

            int maxCol = -1;
            int maxRow = -1;
            var tileMap = new Dictionary<(int, int), string>();

            foreach (Match match in regex.Matches(html))
            {
                int x = int.Parse(match.Groups[1].Value);
                int y = int.Parse(match.Groups[2].Value);
                string tile = match.Groups[3].Value.ToLower();

                if (x > maxCol) maxCol = x;
                if (y > maxRow) maxRow = y;

                tileMap[(x, y)] = tile == "swo" ? "0" : "1";
            }

            int width = maxCol + 1;
            int height = maxRow + 1;
            var grid = new string[height, width];

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    grid[y, x] = tileMap.TryGetValue((x, y), out var value) ? value : "0";

            var layoutRows = new List<string>();
            for (int y = 0; y < height; y++)
            {
                var sb = new StringBuilder();
                for (int x = 0; x < width; x++)
                    sb.Append(grid[y, x]);
                layoutRows.Add(sb.ToString());
            }

            return string.Join(",", layoutRows);
        }

        private List<string> ExtractTokenLayouts(string html)
        {
            var tokenList = new List<string>();

            var tableMatcher = new Regex(@"<table.*?>(.*?)<\/table>", RegexOptions.Singleline);
            var rowMatcher = new Regex(@"<tr>(.*?)<\/tr>", RegexOptions.Singleline);
            var fillDetector = new Regex(@"<img[^>]+square\.gif", RegexOptions.IgnoreCase);

            foreach (Match tableMatch in tableMatcher.Matches(html))
            {
                var content = tableMatch.Groups[1].Value;
                var rowSpecs = new List<string>();

                foreach (Match rowMatch in rowMatcher.Matches(content))
                {
                    var rowContent = rowMatch.Groups[1].Value;
                    var cells = rowContent.Split(new[] { "</td>" }, StringSplitOptions.None);
                    var sb = new StringBuilder();

                    foreach (var cellHtml in cells)
                    {
                        bool filled = fillDetector.IsMatch(cellHtml);
                        sb.Append(filled ? "1" : "0");
                    }

                    rowSpecs.Add(sb.ToString());
                }

                int maxActiveCol = 0;
                for (int col = 0; ; col++)
                {
                    bool found = false;
                    foreach (var row in rowSpecs)
                    {
                        if (col < row.Length && row[col] == '1')
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found) break;
                    maxActiveCol = col;
                }

                var cleanedRows = rowSpecs
                    .Select(r => r.Substring(0, Math.Min(r.Length, maxActiveCol + 1)))
                    .Where(r => r.Contains('1'))
                    .ToList();

                if (cleanedRows.Count > 0)
                {
                    tokenList.Add(string.Join(",", cleanedRows));
                }
            }

            return tokenList;
        }

        private async void startStopButton_Click(object sender, EventArgs e)
        {
            if (solveCancellationTokenSource != null)
            {
                // Stop pressed: cancel the running task
                solveCancellationTokenSource.Cancel();
                startStopButton.Enabled = false; // Prevent spam clicks
                return;
            }

            // Start pressed: begin solving
            solveCancellationTokenSource = new CancellationTokenSource();

            waitingLabel.Visible = true;
            startStopButton.Text = "Stop";
            inputTextBox.Enabled = false;

            string htmlContent = inputTextBox.Text;

            bool solved = false;
            List<(int X, int Y)> placements = null;
            List<ShapeToken> tokenObjects = null;

            try
            {
                await Task.Run(() =>
                {
                    string gridPattern = ParseGridLayout(htmlContent);
                    var tokenPatterns = ExtractTokenLayouts(htmlContent);
                    tokenObjects = tokenPatterns.Select(p => new ShapeToken(p)).ToList();

                    var puzzle = new TileGrid(gridPattern);
                    var planner = new ShapeShifterSolver(puzzle, tokenObjects, solveCancellationTokenSource.Token);
                    solved = planner.AttemptSolve();

                    if (solved)
                    {
                        var placementDict = planner.GetPlacements();
                        placements = new List<(int X, int Y)>(tokenObjects.Count);
                        for (int i = 0; i < tokenObjects.Count; i++)
                        {
                            if (placementDict.TryGetValue(i, out var pos))
                                placements.Add(pos);
                            else
                                placements.Add((-1, -1));
                        }
                    }
                }, solveCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                solved = false;
                placements = null;
            }
            finally
            {
                solveCancellationTokenSource.Dispose();
                solveCancellationTokenSource = null;

                waitingLabel.Visible = false;
                startStopButton.Text = "Start";
                startStopButton.Enabled = true;
                inputTextBox.Enabled = true;
            }

            stepsPanel.Controls.Clear();

            if (solved)
            {
                for (int i = 0; i < tokenObjects.Count; i++)
                {
                    var pos = placements[i];
                    var checkbox = new CheckBox
                    {
                        Text = $"Step {i + 1}: Row {pos.Y}, Column {pos.X}",
                        AutoSize = true
                    };

                    checkbox.CheckedChanged += (s, eArgs) => HighlightNextStep();

                    var wrapper = new Panel
                    {
                        BorderStyle = BorderStyle.None,
                        AutoSize = true,
                        Padding = new Padding(3),
                        Margin = new Padding(5)
                    };

                    wrapper.Controls.Add(checkbox);
                    stepsPanel.Controls.Add(wrapper);
                }
            }
            else if (placements == null)
            {
                var label = new Label
                {
                    Text = "Operation cancelled.",
                    AutoSize = true,
                    ForeColor = Color.DarkOrange,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Padding = new Padding(10)
                };
                stepsPanel.Controls.Add(label);
            }
            else
            {
                var label = new Label
                {
                    Text = "No arrangement found.",
                    AutoSize = true,
                    ForeColor = Color.Red,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Padding = new Padding(10)
                };
                stepsPanel.Controls.Add(label);
            }

            HighlightNextStep();
        }

        private void HighlightNextStep()
        {
            foreach (Control wrapper in stepsPanel.Controls)
            {
                if (wrapper is Panel panel && panel.Controls[0] is CheckBox cb)
                {
                    if (!cb.Checked)
                    {
                        panel.BorderStyle = BorderStyle.FixedSingle;
                        cb.Focus();
                        break;
                    }
                    else
                    {
                        panel.BorderStyle = BorderStyle.None;
                    }
                }
            }
        }
    }

    public class TileGrid
    {
        private int[] tiles;
        public int Rows { get; }
        public int Cols { get; }
        public int Wrap { get; }

        public TileGrid(string data, int wrap = 1)
        {
            var rowData = data.Split(',');
            Rows = rowData.Length;
            Cols = rowData[0].Length;
            Wrap = wrap;

            tiles = new int[Rows * Cols];
            int i = 0;
            foreach (var row in rowData)
                foreach (var c in row)
                    tiles[i++] = c == '1' ? 1 : 0;
        }

        public void Mark(ShapeToken token, int col, int row, int delta)
        {
            foreach (var (x, y) in token.Points)
            {
                int absX = col + x;
                int absY = row + y;
                int idx = absY * Cols + absX;
                tiles[idx] = (tiles[idx] + delta) % (Wrap + 1);
            }
        }

        public bool AllClear() => tiles.All(val => val == 0);
    }

    public class ShapeToken
    {
        public List<(int X, int Y)> Points { get; }
        public int Height { get; }
        public int Width { get; }

        public ShapeToken(string layout)
        {
            var rows = layout.Split(',');
            Height = rows.Length;
            Width = rows[0].Length;
            Points = new List<(int X, int Y)>();

            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    if (rows[y][x] == '1')
                        Points.Add((x, y));
        }
    }

    public class ShapeShifterSolver
    {
        private TileGrid canvas;
        private List<ShapeToken> tokens;
        private readonly Dictionary<int, (int X, int Y)> placements = new Dictionary<int, (int X, int Y)>();
        private readonly CancellationToken cancellationToken;

        public ShapeShifterSolver(TileGrid canvas, List<ShapeToken> tokens, CancellationToken cancellationToken = default)
        {
            this.canvas = canvas;
            this.tokens = tokens;
            this.cancellationToken = cancellationToken;
        }

        public bool AttemptSolve(int idx = 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (idx == tokens.Count)
                return canvas.AllClear();

            var token = tokens[idx];
            for (int col = 0; col <= canvas.Cols - token.Width; col++)
            {
                for (int row = 0; row <= canvas.Rows - token.Height; row++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    canvas.Mark(token, col, row, 1);

                    if (AttemptSolve(idx + 1))
                    {
                        placements[idx] = (col, row);
                        return true;
                    }

                    canvas.Mark(token, col, row, canvas.Wrap);
                }
            }

            return false;
        }

        public Dictionary<int, (int X, int Y)> GetPlacements() => placements;
    }
}
