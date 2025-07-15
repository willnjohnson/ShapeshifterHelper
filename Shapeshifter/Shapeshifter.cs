using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsLabel = System.Windows.Forms.Label;
using HtmlAgilityPack;
using ShapeshifterKvho;
using System.Diagnostics;

namespace Shapeshifter
{
    public partial class ShapeShifter : Form
    {
        private CancellationTokenSource cancelSource = null;
        private byte[,] initialBoardState = null;
        private int puzzleRank = 0;
        private Label[,] boardCells;
        private List<SolutionStep> _allSolutionSteps = new List<SolutionStep>();

        public ShapeShifter() => InitializeComponent();

        private void Form1_Load(object sender, EventArgs e)
        {
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            var semiTransparent = Color.FromArgb(64, 0, 0, 0);
            labelPaste.BackColor = semiTransparent;
            labelPaste.Padding = new Padding(3);
            labelResult.BackColor = semiTransparent;
            labelResult.Padding = new Padding(3);
            labelWaiting.BackColor = semiTransparent;
            labelWaiting.Padding = new Padding(2);

            InitializeBoardUI();
        }

        /// <summary>
        /// Extracts shape ID from image URL (e.g., "blank_10x10.gif" -> "blank")
        /// </summary>
        private string ExtractShapeId(string src)
        {
            int lastSlash = src.LastIndexOf('/');
            int underscore = src.LastIndexOf('_');
            return (lastSlash != -1 && underscore != -1 && underscore > lastSlash)
                ? src.Substring(lastSlash + 1, underscore - lastSlash - 1)
                : "unknown";
        }

        /// <summary>
        /// Parses HTML to extract board state, cycle order, and generate solver input
        /// </summary>
        private string ExtractHTML(string html)
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            // Extract board dimensions from JavaScript
            int gX = 0, gY = 0;
            var script = doc.DocumentNode.SelectSingleNode("//script[contains(text(),'gX') and contains(text(),'gY')]");
            if (script != null)
            {
                var scriptText = script.InnerText;
                var matchGX = Regex.Match(scriptText, @"gX\s*=\s*(\d+);");
                var matchGY = Regex.Match(scriptText, @"gY\s*=\s*(\d+);");
                if (matchGX.Success) gX = int.Parse(matchGX.Groups[1].Value);
                if (matchGY.Success) gY = int.Parse(matchGY.Groups[1].Value);
            }

            if (gX == 0 || gY == 0)
            {
                ShowParseError("Could not parse board dimensions from script.");
                return "";
            }

            // Find goal cycle table and extract cycle order
            var goalSmall = doc.DocumentNode.SelectSingleNode("//small[contains(text(),'GOAL')]");
            var goalTd = goalSmall?.Ancestors("td").FirstOrDefault();
            var goalCycleRow = goalTd?.ParentNode;

            if (goalCycleRow == null)
            {
                ShowParseError("Cannot find GOAL cycle information.");
                return "";
            }

            // Extract cycle order from goal row images
            var cycleOrder = goalCycleRow.Elements("td")
                .SelectMany(td => td.Elements("img"))
                .Where(img => !img.GetAttributeValue("src", "").Contains("arrow.gif"))
                .Select(img => ExtractShapeId(img.GetAttributeValue("src", "")))
                .Where(shapeId => !string.IsNullOrEmpty(shapeId))
                .Distinct()
                .ToList();

            var mappings = cycleOrder.Select((shape, i) => new { shape, i })
                .ToDictionary(x => x.shape, x => x.i);

            puzzleRank = cycleOrder.Count;
            if (puzzleRank == 0)
            {
                ShowParseError("Could not determine puzzle rank.");
                return "";
            }

            // Extract goal shape
            var goalImg = goalTd.SelectSingleNode(".//img[contains(@src,'.gif') and not(contains(@src,'arrow.gif'))]");
            if (goalImg == null)
            {
                ShowParseError("Cannot find goal image.");
                return "";
            }

            string goalShapeId = ExtractShapeId(goalImg.GetAttributeValue("src", ""));
            if (!mappings.ContainsKey(goalShapeId))
            {
                ShowParseError($"Goal shape '{goalShapeId}' not in cycle order.");
                return "";
            }

            int goalIndex = mappings[goalShapeId];

            // Extract board state
            var boardTable = doc.DocumentNode.SelectSingleNode("//table[@align='center' and @cellpadding='0']");
            var boardRows = boardTable?.SelectNodes(".//tr");

            if (boardRows == null || boardRows.Count != gY)
            {
                ShowParseError($"Expected {gY} rows in board but found {boardRows?.Count ?? 0}.");
                return "";
            }

            initialBoardState = new byte[gY, gX];
            for (int r = 0; r < gY; r++)
            {
                var imgs = boardRows[r].SelectNodes(".//img");
                if (imgs?.Count != gX)
                {
                    ShowParseError($"Expected {gX} columns in row {r} but found {imgs?.Count ?? 0}.");
                    return "";
                }

                for (int c = 0; c < gX; c++)
                {
                    var shapeId = ExtractShapeId(imgs[c].GetAttributeValue("src", ""));
                    initialBoardState[r, c] = (byte)(mappings.TryGetValue(shapeId, out int value) ? value : 0);
                }
            }

            // Parse shapes for solver
            var shapesForSolver = new List<string>();

            // Helper to parse shape tables
            Func<HtmlNode, string> parseShapeTable = (table) =>
            {
                var points = new List<(int x, int y)>();
                var rows = table.SelectNodes(".//tr");
                if (rows == null) return null;

                for (int r = 0; r < rows.Count; r++)
                {
                    var cells = rows[r].SelectNodes(".//td");
                    if (cells == null) continue;

                    for (int c = 0; c < cells.Count; c++)
                    {
                        if (cells[c].SelectSingleNode(".//img[contains(@src, 'square.gif')]") != null)
                            points.Add((c, r));
                    }
                }

                if (!points.Any()) return null;

                // Normalize to bounding box
                int minX = points.Min(p => p.x), minY = points.Min(p => p.y);
                var flatIndices = points
                    .Select(p => (p.y - minY) * gX + (p.x - minX))
                    .OrderBy(n => n);

                return $"{points.Count} {string.Join(" ", flatIndices)}";
            };

            // Parse active and next shapes
            var activeHeader = doc.DocumentNode.SelectSingleNode("//big[contains(text(),'ACTIVE SHAPE')]");
            activeHeader?.ParentNode.SelectNodes("following-sibling::table[@cellpadding='15']//table[@cellpadding='0']")
                ?.ToList().ForEach(table => {
                    var shapeStr = parseShapeTable(table);
                    if (shapeStr != null) shapesForSolver.Add(shapeStr);
                });

            var nextHeader = doc.DocumentNode.SelectSingleNode("//big[contains(text(),'NEXT SHAPES')]");
            nextHeader?.ParentNode.SelectNodes("following-sibling::table[@cellpadding='15']//td//table[@cellpadding='0']")
                ?.ToList().ForEach(table => {
                    var shapeStr = parseShapeTable(table);
                    if (shapeStr != null) shapesForSolver.Add(shapeStr);
                });

            // Build solver input
            var sb = new StringBuilder();
            sb.AppendLine(gX.ToString());
            sb.AppendLine(gY.ToString());

            for (int row = 0; row < gY; row++)
            {
                sb.AppendLine(string.Join(" ", Enumerable.Range(0, gX).Select(col => initialBoardState[row, col])));
            }

            sb.AppendLine(goalIndex.ToString());
            sb.AppendLine(shapesForSolver.Count.ToString());
            shapesForSolver.ForEach(s => sb.AppendLine(s));

            return sb.ToString();
        }

        /// <summary>
        /// Shows parse error message and marks parsing as failed
        /// </summary>
        private void ShowParseError(string message)
        {
            MessageBox.Show(message, "Parsing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            initialBoardState = null;
        }

        /// <summary>
        /// Parses solver output into structured solution steps with board states and highlights
        /// </summary>
        private List<SolutionStep> ParseSolverResult(string solverOutput, int boardRows, int boardCols)
        {
            var steps = new List<SolutionStep>();
            if (string.IsNullOrWhiteSpace(solverOutput) || boardRows == 0 || boardCols == 0)
                return steps;

            var reader = new StringReader(solverOutput);
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                var placementMatch = Regex.Match(line.Trim(), @"Column:\s*(\d+),\s*Row:\s*(\d+)");
                if (!placementMatch.Success) continue;

                int col = int.Parse(placementMatch.Groups[1].Value);
                int row = int.Parse(placementMatch.Groups[2].Value);

                var currentStepBoardState = new byte[boardRows, boardCols];
                var highlightedCells = new List<(int R, int C)>();

                for (int r = 0; r < boardRows; r++)
                {
                    if ((line = reader.ReadLine()) == null) break;

                    var cellMatches = Regex.Matches(line, @"(\d+)(\+|\-)?\|?");

                    for (int c = 0; c < boardCols; c++)
                    {
                        if (c < cellMatches.Count)
                        {
                            var match = cellMatches[c];
                            if (byte.TryParse(match.Groups[1].Value, out byte val))
                            {
                                currentStepBoardState[r, c] = val;
                                if (match.Groups[2].Value == "+")
                                    highlightedCells.Add((r, c));
                            }
                        }
                    }
                }

                steps.Add(new SolutionStep
                {
                    BoardState = currentStepBoardState,
                    PlacementX = col,
                    PlacementY = row,
                    HighlightedCells = highlightedCells
                });
            }
            return steps;
        }

        /// <summary>
        /// Handles puzzle solving - starts solver or cancels if running
        /// </summary>
        private async void startStopButton_Click(object sender, EventArgs e)
        {
            if (cancelSource != null)
            {
                cancelSource.Cancel();
                btnStartStop.Enabled = false;
                return;
            }

            // Reset UI state
            InitializeBoardUI();
            _allSolutionSteps.Clear();
            textBoxStepsPanel.Controls.Clear();

            string dat = ExtractHTML(textBoxInput.Text);
            if (string.IsNullOrEmpty(dat) || initialBoardState == null || puzzleRank == 0)
            {
                SetUIState(false);
                return;
            }

            cancelSource = new CancellationTokenSource();
            SetUIState(true);

            var startTime = DateTime.Now;
            string solverResult = null;

            try
            {
                var timer = new System.Windows.Forms.Timer { Interval = 250 };
                timer.Tick += (s, args) => {
                    var elapsed = DateTime.Now - startTime;
                    labelWaiting.Text = $"Calculating... [{elapsed.TotalSeconds:F1}s]";
                };
                timer.Start();

                solverResult = await Task.Run(() => ShapeshifterKvho.Solver.Solve(dat), cancelSource.Token);
                timer.Stop();
            }
            catch (OperationCanceledException)
            {
                AddStatusLabel("Operation cancelled.", Color.DarkOrange);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(dat) && initialBoardState != null)
                {
                    MessageBox.Show($"Solver error: {ex.Message}\n\nStackTrace:\n{ex.StackTrace}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    AddStatusLabel($"Solver error: {ex.Message}", Color.Red);
                }
            }
            finally
            {
                cancelSource?.Dispose();
                cancelSource = null;
                SetUIState(false);
            }

            // Process results
            if (!string.IsNullOrEmpty(solverResult))
            {
                _allSolutionSteps = ParseSolverResult(solverResult, initialBoardState.GetLength(0), initialBoardState.GetLength(1));

                if (_allSolutionSteps.Any())
                {
                    DisplayResults(true, _allSolutionSteps);
                    var firstStep = _allSolutionSteps[0];
                    UpdateBoardUI((byte[,])firstStep.BoardState.Clone(), firstStep.HighlightedCells);
                }
                else
                {
                    SetNoSolutionUI();
                }
            }
            else
            {
                if (string.IsNullOrEmpty(labelResult.Text))
                    SetNoSolutionUI();
                UpdateBoardUI(null, null);
            }
        }

        /// <summary>
        /// Sets UI to show no solution found
        /// </summary>
        private void SetNoSolutionUI()
        {
            AddStatusLabel("No arrangement found.", Color.Red);
        }

        /// <summary>
        /// Updates UI state based on solver status
        /// </summary>
        private void SetUIState(bool solving)
        {
            labelWaiting.Visible = solving;
            labelWaiting.Text = solving ? "Calculating..." : "";
            btnStartStop.Text = solving ? "Stop" : "Start";
            btnStartStop.Enabled = true;
            textBoxInput.Enabled = !solving;
        }

        /// <summary>
        /// Initializes board UI grid based on current board state
        /// </summary>
        private void InitializeBoardUI()
        {
            tableSolution.Controls.Clear();
            tableSolution.ColumnStyles.Clear();
            tableSolution.RowStyles.Clear();

            if (initialBoardState == null) return;

            int rows = initialBoardState.GetLength(0);
            int cols = initialBoardState.GetLength(1);

            tableSolution.ColumnCount = cols;
            tableSolution.RowCount = rows;
            boardCells = new Label[rows, cols];

            float colWidth = 100f / cols;
            float rowHeight = 100f / rows;

            // Add column and row styles
            for (int i = 0; i < cols; i++)
                tableSolution.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, colWidth));
            for (int i = 0; i < rows; i++)
                tableSolution.RowStyles.Add(new RowStyle(SizeType.Percent, rowHeight));

            // Create board cells
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var cell = new Label
                    {
                        Dock = DockStyle.Fill,
                        Margin = Padding.Empty,
                        Padding = Padding.Empty,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Font = new Font("Segoe UI", 8),
                        Text = "",
                        BorderStyle = BorderStyle.FixedSingle,
                        BackColor = Color.FromArgb(40, 40, 40),
                        ForeColor = Color.White
                    };
                    boardCells[r, c] = cell;
                    tableSolution.Controls.Add(cell, c, r);
                }
            }
        }

        /// <summary>
        /// Updates board display with current state and highlights
        /// </summary>
        private void UpdateBoardUI(byte[,] boardState, List<(int R, int C)> highlightedCells)
        {
            if (boardState == null || boardState.GetLength(0) == 0 || boardState.GetLength(1) == 0)
            {
                InitializeBoardUI();
                return;
            }

            // Reinitialize if dimensions changed
            if (boardCells == null || boardCells.GetLength(0) != boardState.GetLength(0) ||
                boardCells.GetLength(1) != boardState.GetLength(1))
            {
                InitializeBoardUI();
                if (boardCells == null) return;
            }

            var highlightSet = new HashSet<(int, int)>(highlightedCells ?? new List<(int, int)>());

            for (int r = 0; r < boardState.GetLength(0); r++)
            {
                for (int c = 0; c < boardState.GetLength(1); c++)
                {
                    var cell = boardCells[r, c];
                    byte value = boardState[r, c];

                    cell.Text = value.ToString();

                    // Set colors based on value
                    var colors = new[] {
                        Color.FromArgb(0x2E, 0x8B, 0x57), // SeaGreen
                        Color.FromArgb(0xB2, 0x22, 0x22), // Firebrick
                        Color.FromArgb(0xFF, 0x8C, 0x00), // DarkOrange
                        Color.FromArgb(0x46, 0x82, 0xB4), // SteelBlue
                        Color.FromArgb(0x8B, 0x00, 0x8B)  // DarkMagenta
                    };

                    if (highlightSet.Contains((r, c)))
                    {
                        cell.BackColor = Color.White;
                        cell.ForeColor = Color.Black;
                    }
                    else
                    {
                        cell.BackColor = value < colors.Length ? colors[value] : Color.Gray;
                        cell.ForeColor = Color.White;
                    }
                }
            }
        }

        /// <summary>
        /// Displays solution results in UI with interactive step controls
        /// </summary>
        private void DisplayResults(bool solved, List<SolutionStep> solutionSteps)
        {
            textBoxStepsPanel.Controls.Clear();
            labelWaiting.Text = "";

            if (!solved || solutionSteps?.Any() != true)
            {
                SetNoSolutionUI();
                UpdateBoardUI(null, null);
                return;
            }

            for (int i = 0; i < solutionSteps.Count; i++)
            {
                var step = solutionSteps[i];
                var flowPanel = new FlowLayoutPanel
                {
                    AutoSize = true,
                    FlowDirection = FlowDirection.LeftToRight,
                    WrapContents = false,
                    Padding = Padding.Empty,
                    Margin = new Padding(5),
                    BackColor = Color.Transparent
                };

                string stepNum = (i + 1).ToString("D2");

                // Add regular checkbox for all steps except the last
                var checkbox = new CheckBox
                {
                    Text = $"Step {stepNum}:",
                    AutoSize = true,
                    Margin = new Padding(0, 3, 0, 0),
                    Font = new Font("Segoe UI", 9),
                    Tag = i,
                    Checked = false
                };
                checkbox.CheckedChanged += StepCheckbox_CheckedChanged;

                if (i == solutionSteps.Count - 1)
                {
                    // Hide the box by shifting text right and overlapping box
                    //checkbox.TextAlign = ContentAlignment.MiddleLeft;
                    //checkbox.Padding = new Padding(4, 0, 0, 0); // simulate checkbox spacing
                    checkbox.Region = new Region(new Rectangle(18, 0, checkbox.PreferredSize.Width, checkbox.Height));
                }

                flowPanel.Controls.Add(checkbox);

                // Add row and column labels
                var rowLabel = new Label
                {
                    Text = $"Row {step.PlacementY},",
                    AutoSize = true,
                    ForeColor = Color.Red,
                    Font = new Font("Segoe UI", 9),
                    Margin = new Padding(5, 3, 0, 0)
                };
                flowPanel.Controls.Add(rowLabel);

                flowPanel.Controls.Add(new Label
                {
                    Text = $"Column {step.PlacementX}",
                    AutoSize = true,
                    ForeColor = Color.Red,
                    Font = new Font("Segoe UI", 9),
                    Padding = new Padding(5, 3, 0, 0)
                });

                textBoxStepsPanel.Controls.Add(flowPanel);
            }

            HighlightStep();
        }

        /// <summary>
        /// Handles step checkbox changes with sequential logic
        /// </summary>
        private void StepCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (!(sender is CheckBox changedCb)) return;

            int changedIndex = (int)changedCb.Tag;

            if (changedCb.Checked)
            {
                // Check all previous steps
                for (int i = 0; i < changedIndex; i++)
                {
                    if (textBoxStepsPanel.Controls[i] is FlowLayoutPanel fp &&
                        fp.Controls[0] is CheckBox cb && !cb.Checked)
                        cb.Checked = true;
                }
            }
            else
            {
                // Uncheck all subsequent steps
                for (int i = changedIndex + 1; i < textBoxStepsPanel.Controls.Count; i++)
                {
                    if (textBoxStepsPanel.Controls[i] is FlowLayoutPanel fp &&
                        fp.Controls[0] is CheckBox cb && cb.Checked)
                        cb.Checked = false;
                }
            }

            UpdateBoardVisualization();
            HighlightStep();
        }

        /// <summary>
        /// Updates board visualization based on checked steps
        /// </summary>
        private void UpdateBoardVisualization()
        {
            if (!_allSolutionSteps.Any())
            {
                UpdateBoardUI(null, null);
                return;
            }

            // Find last checked step
            int lastCheckedIndex = -1;
            for (int i = textBoxStepsPanel.Controls.Count - 1; i >= 0; i--)
            {
                if (textBoxStepsPanel.Controls[i] is FlowLayoutPanel fp &&
                    fp.Controls[0] is CheckBox cb && cb.Checked)
                {
                    lastCheckedIndex = (int)cb.Tag;
                    break;
                }
            }

            var stepToDisplay = _allSolutionSteps[lastCheckedIndex != -1 ? lastCheckedIndex + 1 : 0];
            UpdateBoardUI((byte[,])stepToDisplay.BoardState.Clone(), stepToDisplay.HighlightedCells);
        }

        /// <summary>
        /// Adds status label to steps panel
        /// </summary>
        private void AddStatusLabel(string text, Color color)
        {
            textBoxStepsPanel.Controls.Add(new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = color,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Padding = new Padding(10)
            });
        }

        /// <summary>
        /// Applies visual styling to steps based on checkbox state
        /// </summary>
        private void HighlightStep()
        {
            var baseFont = new Font("Segoe UI", 9);

            foreach (Control control in textBoxStepsPanel.Controls)
            {
                if (!(control is FlowLayoutPanel flowPanel)) continue;

                var cb = flowPanel.Controls.OfType<CheckBox>().FirstOrDefault();
                var labels = flowPanel.Controls.OfType<Label>().ToArray();

                if (cb != null) // Has checkbox
                {
                    var rowLabel = labels.Length > 0 ? labels[0] : null;
                    var colLabel = labels.Length > 1 ? labels[1] : null;

                    if (cb.Checked)
                    {
                        cb.Font = new Font(baseFont, FontStyle.Strikeout);
                        cb.ForeColor = Color.DarkGray;
                        SetLabelStyle(rowLabel, baseFont, FontStyle.Strikeout, Color.DarkGray);
                        SetLabelStyle(colLabel, baseFont, FontStyle.Strikeout, Color.DarkGray);
                    }
                    else
                    {
                        cb.Font = baseFont;
                        cb.ForeColor = Color.White;
                        SetLabelStyle(rowLabel, baseFont, FontStyle.Regular, Color.Red);
                        SetLabelStyle(colLabel, baseFont, FontStyle.Regular, Color.Red);
                    }
                }
                else // Final step without checkbox
                {
                    foreach (var label in labels)
                    {
                        if (label.Text.Contains("Step"))
                            label.ForeColor = Color.White;
                        else
                            label.ForeColor = Color.Red;
                    }
                }
            }
        }

        /// <summary>
        /// Helper to set label font and color
        /// </summary>
        private void SetLabelStyle(Label label, Font baseFont, FontStyle style, Color color)
        {
            if (label == null) return;
            label.Font = new Font(baseFont, style);
            label.ForeColor = color;
        }

        private void tableSolution_Paint(object sender, PaintEventArgs e)
        {
            // Custom painting logic can be added here if needed
        }
    }

    /// <summary>
    /// Represents a solution step with board state and placement info
    /// </summary>
    public class SolutionStep
    {
        public byte[,] BoardState { get; set; }
        public int PlacementX { get; set; }
        public int PlacementY { get; set; }
        public List<(int R, int C)> HighlightedCells { get; set; } = new List<(int R, int C)>();
    }

    /// <summary>
    /// Grid representation using byte array for multiple ranks
    /// </summary>
    public class RankGrid
    {
        private byte[,] _grid;
        public int Rows { get; }
        public int Cols { get; }
        public int NumRanks { get; }

        public RankGrid(byte[,] initialGridData, int numRanks)
        {
            Rows = initialGridData.GetLength(0);
            Cols = initialGridData.GetLength(1);
            NumRanks = numRanks;
            _grid = new byte[Rows, Cols];
            Buffer.BlockCopy(initialGridData, 0, _grid, 0, initialGridData.Length * sizeof(byte));
        }

        public byte[,] GetGridArray() => _grid;
        public byte GetTile(int row, int col) => _grid[row, col];

        public bool IsCleared()
        {
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    if (_grid[r, c] != 0) return false;
            return true;
        }

        public RankGrid Clone() => new RankGrid((byte[,])_grid.Clone(), NumRanks);

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                    sb.Append(_grid[r, c]);
                if (r < Rows - 1) sb.Append(',');
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Represents a puzzle piece with position and value data
    /// </summary>
    public class Token
    {
        public IReadOnlyList<(int X, int Y, byte Value)> Points { get; }
        public int Height { get; }
        public int Width { get; }
        public int Area => Points.Count;
        public int OriginalIndex { get; }

        public Token(IEnumerable<(int X, int Y, byte Value)> points, int originalIndex, int width, int height)
        {
            Points = points.ToList().AsReadOnly();
            OriginalIndex = originalIndex;
            Width = width;
            Height = height;
        }

        public bool Equals(Token other) => other?.OriginalIndex == OriginalIndex;
        public override bool Equals(object obj) => Equals(obj as Token);
        public override int GetHashCode() => OriginalIndex.GetHashCode();

        public override string ToString()
        {
            if (!Points.Any()) return "";

            // Find bounding box dimensions
            int minX = Points.Min(p => p.X);
            int maxX = Points.Max(p => p.X);
            int minY = Points.Min(p => p.Y);
            int maxY = Points.Max(p => p.Y);

            int actualWidth = maxX - minX + 1;
            int actualHeight = maxY - minY + 1;

            // Create grid representation
            byte[,] pieceGrid = new byte[actualHeight, actualWidth];
            foreach (var point in Points)
            {
                pieceGrid[point.Y - minY, point.X - minX] = point.Value;
            }

            // Build string representation
            var sb = new StringBuilder();
            for (int row = 0; row < actualHeight; row++)
            {
                for (int col = 0; col < actualWidth; col++)
                {
                    sb.Append(pieceGrid[row, col]);
                }
                if (row < actualHeight - 1)
                    sb.Append(',');
            }
            return sb.ToString();
        }
    }
    /// <summary>
    /// Represents a puzzle shape with pre-calculated placement data for the KVHO algorithm.
    /// This combines aspects of `struct shape1` and `struct shape` from `ss.c`.
    /// </summary>
    public class ProcessedShape
    {
        public Token OriginalToken { get; }
        public int Index { get; } // Index within the *sorted and processed* list of shapes
        public int PlacementsX { get; } // Number of possible horizontal top-left placements
        public int PlacementsY { get; } // Number of possible vertical top-left placements
        public int TotalPossiblePlacements { get; } // Total (PlacementsX * PlacementsY)

        // `cache` from ss.c: A list of lists. Outer list for each possible top-left placement (y, x).
        // Inner list contains the 1D indices (row * boardCols + col) of the board cells affected by this placement.
        public IReadOnlyList<IReadOnlyList<int>> AffectedBoardCellIndicesCache { get; }

        public ProcessedShape EqualShape { get; set; } // Link to an equivalent shape for pruning

        public ProcessedShape(Token originalToken, int boardRows, int boardCols, int shapeIndex)
        {
            OriginalToken = originalToken;
            Index = shapeIndex;

            // Calculate number of possible top-left placement coordinates for this shape
            PlacementsX = boardCols - originalToken.Width + 1;
            PlacementsY = boardRows - originalToken.Height + 1;
            TotalPossiblePlacements = PlacementsX * PlacementsY;

            var affectedCellsList = new List<List<int>>();

            // Pre-calculate all affected board cell indices for all possible placements
            for (int r = 0; r < PlacementsY; r++) // Iterate through all possible top-left row placements
            {
                for (int c = 0; c < PlacementsX; c++) // Iterate through all possible top-left column placements
                {
                    var currentPlacementAffectedCells = new List<int>();
                    foreach (var point in originalToken.Points) // For each active cell within the token's shape
                    {
                        int absRow = r + point.Y; // Use named property Y instead of Item2
                        int absCol = c + point.X; // Use named property X instead of Item1
                                                  // Convert 2D coordinates to a 1D index on the board
                        currentPlacementAffectedCells.Add(absRow * boardCols + absCol);
                    }
                    affectedCellsList.Add(currentPlacementAffectedCells);
                }
            }
            AffectedBoardCellIndicesCache = affectedCellsList.Select(l => (IReadOnlyList<int>)l.AsReadOnly()).ToList().AsReadOnly();
        }

        /// <summary>
        /// Provides a unique identifier for the shape's configuration (its structure),
        /// used for grouping identical shapes for optimization (e.g., "0,0,1;1,0,1;").
        /// </summary>
        public string GetShapeConfigurationKey()
        {
            // Create a canonical string representation of the shape's points
            // Ordering by Y then X ensures a consistent key for identical shapes
            var sb = new StringBuilder();
            foreach (var point in OriginalToken.Points.OrderBy(p => p.Y).ThenBy(p => p.X)) // Use named properties Y and X
            {
                sb.Append($"{point.X},{point.Y},{point.Value};"); // Use named properties X, Y, Value
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// A custom label control with rounded corners and smooth rendering.
    /// </summary>
    public class RoundedLabel : Label
    {
        private int _cornerRadius = 10;
        private Region _currentRegion;

        /// <summary>
        /// Gets or sets the corner radius for the rounded label.
        /// </summary>
        public int CornerRadius
        {
            get => _cornerRadius;
            set
            {
                if (_cornerRadius != value && value >= 0)
                {
                    _cornerRadius = value;
                    UpdateRegion();
                    Invalidate();
                }
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateRegion();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (var path = CreateRoundedPath())
            {
                // Configure graphics for smooth rendering
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // Fill the background
                using (var brush = new SolidBrush(BackColor))
                {
                    e.Graphics.FillPath(brush, path);
                }

                // Draw the text
                DrawCenteredText(e.Graphics);
            }
        }

        private GraphicsPath CreateRoundedPath()
        {
            var path = new GraphicsPath();
            var bounds = new Rectangle(0, 0, Width, Height);
            var diameter = _cornerRadius * 2;

            if (_cornerRadius > 0)
            {
                // Create rounded rectangle
                path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
                path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
                path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
                path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
                path.CloseFigure();
            }
            else
            {
                // Regular rectangle
                path.AddRectangle(bounds);
            }

            return path;
        }

        private void DrawCenteredText(Graphics graphics)
        {
            const TextFormatFlags flags = TextFormatFlags.HorizontalCenter |
                                        TextFormatFlags.VerticalCenter |
                                        TextFormatFlags.EndEllipsis;

            TextRenderer.DrawText(graphics, Text, Font, ClientRectangle, ForeColor, flags);
        }

        private void UpdateRegion()
        {
            _currentRegion?.Dispose();
            using (var path = CreateRoundedPath())
            {
                _currentRegion = new Region(path);
                Region = _currentRegion;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _currentRegion?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Custom checkbox with just the text, no checkbox.
    /// </summary>
    public class TextOnlyCheckBox : CheckBox
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            // Skip painting the checkbox itself
            TextRenderer.DrawText(
                e.Graphics,
                this.Text,
                this.Font,
                this.ClientRectangle,
                this.ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter
            );
        }
    }

    /// <summary>
    /// A FlowLayoutPanel with enhanced double buffering to eliminate flicker during updates.
    /// Automatically applies optimized rendering styles for smooth visual performance.
    /// </summary>
    public class DoubleBufferedFlowLayoutPanel : FlowLayoutPanel
    {
        private const int WS_EX_COMPOSITED = 0x02000000;

        public DoubleBufferedFlowLayoutPanel()
        {
            InitializeDoubleBuffering();
        }

        /// <summary>
        /// Configures the control for optimal double buffering performance.
        /// </summary>
        private void InitializeDoubleBuffering()
        {
            DoubleBuffered = true;

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            UpdateStyles();
        }

        /// <summary>
        /// Applies Windows Forms Compositing for additional flicker reduction.
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                var createParams = base.CreateParams;
                createParams.ExStyle |= WS_EX_COMPOSITED;
                return createParams;
            }
        }
    }
}