using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq; // For .Sum(), .All(), .SequenceEqual(), .Select(), .OrderBy()
using System.Text; // For StringBuilder
using System.Text.RegularExpressions; // For Regex parsing HTML
using System.Threading; // For CancellationTokenSource
using System.Threading.Tasks; // For Task.Run
using System.Windows.Forms; // For Windows Forms UI components
using WinFormsLabel = System.Windows.Forms.Label;

namespace Shapeshifter
{
    public partial class ShapeShifter : Form
    {
        private CancellationTokenSource cancelSource = null;

        public ShapeShifter() => InitializeComponent();

        private void textBox1_TextChanged(object sender, EventArgs e) {
        }
        private void label1_Click(object sender, EventArgs e) { }
        private void label1_Click_1(object sender, EventArgs e) { }
        private void label1_Click_2(object sender, EventArgs e) { }
        private void Form1_Load(object sender, EventArgs e)
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            labelPaste.BackColor = Color.FromArgb(64, 0, 0, 0);
            labelPaste.Padding = new Padding(3);

            labelResult.BackColor = Color.FromArgb(64, 0, 0, 0);
            labelResult.Padding = new Padding(3);

            labelWaiting.BackColor = Color.FromArgb(64, 0, 0, 0);
            labelWaiting.Padding = new Padding(2);
        }

        /// <summary>
        /// Parses HTML grid layout and converts tile positions to binary string format.
        /// It assumes 'swo' is 0 (empty) and other tiles are 1 (filled).
        /// </summary>
        /// <param name="html">The HTML string containing the grid layout.</param>
        /// <returns>A comma-separated string representing the grid (e.g., "101,010").</returns>
        private string ParseGridLayout(string html)
        {
            var regex = new Regex(@"mouseon\((\d+),(\d+)\)""[^>]*>\s*<img[^>]+/([^/_]+)_\d+\.gif", RegexOptions.IgnoreCase);
            var tileMap = new Dictionary<(int, int), string>();
            int maxCol = -1, maxRow = -1;

            foreach (Match match in regex.Matches(html))
            {
                int x = int.Parse(match.Groups[1].Value); // Column index
                int y = int.Parse(match.Groups[2].Value); // Row index
                string tileType = match.Groups[3].Value.ToLower(); // e.g., "swo", "swp"

                if (x > maxCol) maxCol = x;
                if (y > maxRow) maxRow = y;

                // 'swo' is typically the empty state, others are filled
                tileMap[(x, y)] = tileType == "swo" ? "0" : "1";
            }

            // Determine actual grid dimensions
            int width = maxCol + 1;
            int height = maxRow + 1;

            // Initialize a 2D array for the grid and fill it
            var gridArray = new string[height, width];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    gridArray[y, x] = tileMap.TryGetValue((x, y), out var val) ? val : "0";
                }
            }

            // Convert 2D array to comma-separated binary string format
            var rows = new List<string>();
            for (int y = 0; y < height; y++)
            {
                var sb = new StringBuilder();
                for (int x = 0; x < width; x++)
                {
                    sb.Append(gridArray[y, x]);
                }
                rows.Add(sb.ToString());
            }

            return string.Join(",", rows);
        }

        /// <summary>
        /// Extracts token (puzzle piece) layouts from HTML tables and converts them
        /// into binary pattern strings.
        /// </summary>
        /// <param name="html">The HTML string containing the token tables.</param>
        /// <returns>A list of binary pattern strings for each token.</returns>
        private List<string> ExtractTokens(string html)
        {
            var tokens = new List<string>();
            var tableMatcher = new Regex(@"<table.*?>(.*?)<\/table>", RegexOptions.Singleline);
            var rowMatcher = new Regex(@"<tr>(.*?)<\/tr>", RegexOptions.Singleline);
            var fillDetector = new Regex(@"<img[^>]+square\.gif", RegexOptions.IgnoreCase); // Detects filled cells

            foreach (Match tableMatch in tableMatcher.Matches(html))
            {
                var content = tableMatch.Groups[1].Value; // Content within the <table> tags
                var rows = new List<string>();

                foreach (Match rowMatch in rowMatcher.Matches(content))
                {
                    // Split row into cells and determine if each cell is filled or empty
                    var cells = rowMatch.Groups[1].Value.Split(new[] { "</td>" }, StringSplitOptions.None);
                    var sb = new StringBuilder();
                    foreach (var cell in cells)
                    {
                        sb.Append(fillDetector.IsMatch(cell) ? "1" : "0");
                    }
                    rows.Add(sb.ToString());
                }

                // Trim trailing empty columns from the token pattern
                int maxCol = 0;
                for (int col = 0; ; col++)
                {
                    bool foundFilledInCol = rows.Any(row => col < row.Length && row[col] == '1');
                    if (!foundFilledInCol) break; // No more '1's in this column or beyond
                    maxCol = col;
                }

                // Trim empty rows and apply column trimming
                var cleanRows = rows
                    .Select(r => r.Substring(0, Math.Min(r.Length, maxCol + 1))) // Trim columns
                    .Where(r => r.Contains('1')) // Remove completely empty rows
                    .ToList();

                if (cleanRows.Count > 0)
                {
                    tokens.Add(string.Join(",", cleanRows));
                }
            }

            return tokens;
        }

        /// <summary>
        /// Handles start/stop button clicks for puzzle solving.
        /// Initiates the IDA* search or cancels it if already running.
        /// </summary>
        private async void startStopButton_Click(object sender, EventArgs e)
        {
            if (cancelSource != null)
            {
                // If a solution is in progress, cancel it.
                cancelSource.Cancel();
                btnStartStop.Enabled = false; // Disable button while cancelling
                return;
            }

            // Start a new solving operation
            cancelSource = new CancellationTokenSource();
            textBoxStepsPanel.Controls.Clear();
            SetUIState(true); // Set UI to "solving" mode

            string html = textBoxInput.Text;
            bool solved = false;
            Dictionary<int, (int X, int Y)> placements = null;
            List<Token> tokens = null;
            BitGrid initialGrid = null;

            try
            {
                // Run the computationally intensive part on a separate thread
                await Task.Run(() =>
                {
                    // Parse the puzzle input
                    string gridPattern = ParseGridLayout(html);
                    var tokenPatterns = ExtractTokens(html);

                    // Create Token objects with their original index for ordering
                    tokens = tokenPatterns.Select((p, i) => new Token(p, i)).ToList();
                    initialGrid = new BitGrid(gridPattern);

                    // Initialize and run the solver
                    var solver = new Solver(initialGrid, tokens, cancelSource.Token, count =>
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            labelWaiting.Text = $"Calculating... [{count:N0} expanded nodes]";
                        });
                    });

                    placements = solver.SolveIDAStar(); // Use IDA* to solve
                    solved = placements != null;
                }, cancelSource.Token); // Pass cancellation token to Task.Run
            }
            catch (OperationCanceledException)
            {
                // Task was cancelled
                solved = false;
                placements = null;
            }
            catch (Exception ex)
            {
                // Catch other potential exceptions during solving
                solved = false;
                placements = null;
                MessageBox.Show($"An error occurred: {ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Clean up resources and reset UI regardless of outcome
                cancelSource.Dispose();
                cancelSource = null;
                SetUIState(false); // Set UI to "not solving" mode
            }

            DisplayResults(solved, placements, tokens);
        }

        /// <summary>
        /// Sets the UI state (buttons, labels, text boxes) based on whether the solver is active.
        /// </summary>
        /// <param name="solving">True if the solver is currently running, false otherwise.</param>
        private void SetUIState(bool solving)
        {
            labelWaiting.Visible = solving;
            labelWaiting.Text = solving ? "Calculating..." : "";
            btnStartStop.Text = solving ? "Stop" : "Start";
            btnStartStop.Enabled = true; // Always enable start/stop button after operation
        }

        /// <summary>
        /// Displays the puzzle solving results in the UI, listing steps if solved,
        /// or showing status messages if cancelled/not found.
        /// </summary>
        /// <param name="solved">True if a solution was found.</param>
        /// <param name="placements">Dictionary of token placements, if solved.</param>
        /// <param name="tokens">List of original Token objects.</param>
        private void DisplayResults(bool solved, Dictionary<int, (int X, int Y)> placements, List<Token> tokens)
        {
            if (solved && placements != null)
            {
                labelWaiting.Text = "Solved!";
                // Order placements by original token index for display consistency
                var orderedPlacements = tokens
                    .Where(t => placements.ContainsKey(t.OriginalIndex))
                    .Select(t => (token: t, placement: placements[t.OriginalIndex]))
                    .OrderBy(x => x.token.OriginalIndex)
                    .ToList();

                for (int i = 0; i < orderedPlacements.Count; i++)
                {
                    var (token, pos) = orderedPlacements[i];

                    // Calculate the step number (OriginalIndex is 0-based, so add 1)
                    int stepNumber = token.OriginalIndex + 1;
                    // Format to "01", "02", etc., ensuring consistent width
                    string formattedStep = stepNumber.ToString("D2");

                    // Create a FlowLayoutPanel for each step entry to hold controls horizontally
                    var flowPanel = new FlowLayoutPanel
                    {
                        AutoSize = true, // Important: let it size itself to its contents
                        FlowDirection = FlowDirection.LeftToRight, // Arrange controls from left to right
                        WrapContents = false, // Keep all contents on a single line
                        Padding = new Padding(0), // No internal padding for the flow panel
                        Margin = new Padding(5, 5, 5, 5), // External margin for spacing between step entries
                        BackColor = Color.FromArgb(0, 0, 0, 0)
                    };

                    // 1. CheckBox for "Step 01:"
                    var stepCheckbox = new CheckBox
                    {
                        Text = $"Step {formattedStep}:", // Using the formatted step number
                        AutoSize = true, // Sizes to fit its text
                        Padding = new Padding(0), // No internal padding
                        Margin = new Padding(0, 3, 0, 0), // Top margin (3px) to visually align with labels
                        Font = new Font("Segoe UI", 9) // Explicitly set font to match labels if desired
                    };
                    // Attach the event handler to highlight when checked
                    stepCheckbox.CheckedChanged += (s, args) => HighlightStep();
                    flowPanel.Controls.Add(stepCheckbox);

                    // 2. Label for "Row Y," in Red
                    var rowLabel = new Label
                    {
                        Text = $"Row {pos.Y},",
                        AutoSize = true,
                        ForeColor = Color.Red, // Set text color to Red
                        Font = new Font("Segoe UI", 9), // Use a standard font like CheckBox
                        Padding = new Padding(0),
                        Margin = new Padding(15, 3, 0, 0) // Left margin for "tab" spacing (15px from checkbox)
                    };
                    flowPanel.Controls.Add(rowLabel);

                    // 3. Label for "Column X" in Red
                    var colLabel = new Label
                    {
                        Text = $"Column {pos.X}",
                        AutoSize = true,
                        ForeColor = Color.Red, // Set text color to Red
                        Font = new Font("Segoe UI", 9), // Use a standard font like CheckBox
                        Padding = new Padding(0),
                        Margin = new Padding(5, 3, 0, 0) // Left margin for spacing (5px from rowLabel)
                    };
                    flowPanel.Controls.Add(colLabel);

                    // Add the flowPanel (which contains the checkbox and labels)
                    textBoxStepsPanel.Controls.Add(flowPanel);
                }
            }
            else if (placements == null && cancelSource == null) // If placements is null AND not cancelled means no solution found
            {
                labelWaiting.Text = "No arrangement found.";
                AddStatusLabel("No arrangement found.", Color.Red);
            }
            else // If placements is null AND cancelSource is null (due to finally block reset) -> cancelled
            {
                labelWaiting.Text = "Operation cancelled.";
                AddStatusLabel("Operation cancelled.", Color.DarkOrange);
            }

            HighlightStep(); // Initial highlighting
        }

        /// <summary>
        /// Adds a status label to the steps panel.
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
        /// Highlights (or unhighlights) solution steps based on checkbox state.
        /// </summary>
        private void HighlightStep()
        {
            foreach (Control control in textBoxStepsPanel.Controls)
            {
                if (control is FlowLayoutPanel flowPanel)
                {
                    CheckBox cb = null;
                    Label rowLabel = null;
                    Label colLabel = null;

                    // Find the CheckBox and Labels within the FlowLayoutPanel
                    if (flowPanel.Controls.Count > 0 && flowPanel.Controls[0] is CheckBox)
                    {
                        cb = (CheckBox)flowPanel.Controls[0];
                    }
                    if (flowPanel.Controls.Count > 1 && flowPanel.Controls[1] is Label)
                    {
                        rowLabel = (Label)flowPanel.Controls[1];
                    }
                    if (flowPanel.Controls.Count > 2 && flowPanel.Controls[2] is Label)
                    {
                        colLabel = (Label)flowPanel.Controls[2];
                    }

                    if (cb != null && rowLabel != null && colLabel != null)
                    {
                        // Apply/Remove BorderStyle for the whole entry
                        flowPanel.BorderStyle = cb.Checked ? BorderStyle.None : BorderStyle.FixedSingle;

                        Font baseFont = new Font("Segoe UI", 9); // Or get it from one of your labels initially

                        if (cb.Checked)
                        {
                            // Apply strikethrough to all text in the line
                            cb.Font = new Font(baseFont, baseFont.Style | FontStyle.Strikeout);
                            rowLabel.Font = new Font(baseFont, baseFont.Style | FontStyle.Strikeout);
                            colLabel.Font = new Font(baseFont, baseFont.Style | FontStyle.Strikeout);

                            // Optional: Change forecolor to a muted color for checked items
                            cb.ForeColor = Color.DarkGray;
                            rowLabel.ForeColor = Color.DarkGray;
                            colLabel.ForeColor = Color.DarkGray;
                        }
                        else
                        {
                            // Remove strikethrough
                            cb.Font = baseFont; // Reset to original font style
                            rowLabel.Font = baseFont; // Reset to original font style
                            colLabel.Font = baseFont; // Reset to original font style

                            // Reset forecolor to original values
                            cb.ForeColor = Color.White; // Default text color
                            rowLabel.ForeColor = Color.Red; // Original red color
                            colLabel.ForeColor = Color.Red; // Original red color
                        }
                    }
                }
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {
        }

        private void labelTitle_Click(object sender, EventArgs e)
        {

        }
    }

    /// <summary>
    /// Represents the puzzle grid using bitmasks for efficient operations. Immutable.
    /// This implementation assumes number of ranks equals 2 (binary 0s and 1s).
    /// Grid width (Cols) cannot exceed 64.
    /// </summary>
    public class BitGrid : IEquatable<BitGrid>
    {
        private readonly ulong[] _rows; // Each ulong represents a row of the grid
        public int Rows { get; }
        public int Cols { get; }

        /// <summary>
        /// Initializes a BitGrid from a comma-separated binary string (e.g., "101,010").
        /// </summary>
        /// <param name="data">The string representing the grid layout.</param>
        public BitGrid(string data)
        {
            var rowData = data.Split(',');
            Rows = rowData.Length;
            Cols = rowData[0].Length;

            if (Cols > 64)
            {
                throw new ArgumentOutOfRangeException(nameof(data), "Grid width cannot exceed 64 for ulong bitmask representation.");
            }

            _rows = new ulong[Rows];

            for (int r = 0; r < Rows; r++)
            {
                ulong rowValue = 0;
                for (int c = 0; c < Cols; c++)
                {
                    if (rowData[r][c] == '1')
                    {
                        rowValue |= (1UL << c);
                    }
                }
                _rows[r] = rowValue;
            }
        }

        /// <summary>
        /// Private constructor for creating new BitGrid instances based on an existing row array.
        /// Used by PlaceToken to ensure immutability.
        /// </summary>
        private BitGrid(ulong[] existingRows, int rows, int cols)
        {
            this._rows = (ulong[])existingRows.Clone(); // Deep copy the ulong array
            Rows = rows;
            Cols = cols;
        }

        /// <summary>
        /// Creates a new BitGrid instance with a token's placement applied.
        /// Assumes delta is effectively 1 for a binary (XOR) operation.
        /// </summary>
        /// <param name="token">The token to place.</param>
        /// <param name="col">The column coordinate for the top-left of the token's bounding box.</param>
        /// <param name="row">The row coordinate for the top-left of the token's bounding box.</param>
        /// <param name="delta">Ignored in this binary implementation (always assumes XOR).</param>
        /// <returns>A new BitGrid instance representing the updated state.</returns>
        public BitGrid PlaceToken(Token token, int col, int row, int delta) // delta is ignored for XOR operation
        {
            var newRows = (ulong[])this._rows.Clone(); // Create a copy of the current rows

            foreach (var (x, y) in token.Points) // Iterate through points relative to token's origin
            {
                int absX = col + x; // Absolute column on the grid
                int absY = row + y; // Absolute row on the grid

                // Check if the absolute coordinates are within the grid boundaries
                if (absX >= 0 && absX < Cols && absY >= 0 && absY < Rows)
                {
                    // Flip the bit at (absY, absX) using XOR
                    newRows[absY] ^= (1UL << absX);
                }
            }
            return new BitGrid(newRows, this.Rows, this.Cols);
        }

        /// <summary>
        /// Checks if all tiles in the grid are cleared (have a value of 0).
        /// </summary>
        /// <returns>True if all tiles are 0, false otherwise.</returns>
        public bool IsCleared() => _rows.All(rowVal => rowVal == 0UL);

        /// <summary>
        /// Counts the number of non-zero tiles in the grid using a manual PopCount implementation.
        /// Used as a heuristic in IDA* search (lower count means closer to goal).
        /// </summary>
        /// <returns>The number of filled (non-zero) tiles.</returns>
        public int CountFilledTiles() => _rows.Sum(CountSetBits); // Changed to use local CountSetBits

        /// <summary>
        /// Counts the number of set bits (1s) in a ulong using a software fallback.
        /// This method is necessary if BitOperations.PopCount is not available (e.g., older .NET Framework).
        /// </summary>
        private static int CountSetBits(ulong n)
        {
            n = n - ((n >> 1) & 0x5555555555555555UL);
            n = (n & 0x3333333333333333UL) + ((n >> 2) & 0x3333333333333333UL);
            n = (n + (n >> 4)) & 0x0F0F0F0F0F0F0F0FUL;
            return (int)((n * 0x0101010101010101UL) >> 56);
        }

        /// <summary>
        /// Determines if this BitGrid instance is equal to another BitGrid instance.
        /// Equality is based on dimensions and the sequence of ulong row values.
        /// </summary>
        public bool Equals(BitGrid other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (Rows != other.Rows || Cols != other.Cols) return false;
            return _rows.SequenceEqual(other._rows);
        }

        public override bool Equals(object obj) => Equals(obj as BitGrid);

        /// <summary>
        /// Generates a hash code for the BitGrid instance. Essential for efficient use
        /// in hash-based collections (Dictionary, HashSet).
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Rows.GetHashCode();
                hash = hash * 23 + Cols.GetHashCode();
                foreach (var val in _rows)
                    hash = hash * 23 + val.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Provides a string representation of the grid (e.g., "101,010"),
        /// useful for debugging.
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    sb.Append((_rows[r] & (1UL << c)) != 0 ? '1' : '0');
                }
                if (r < Rows - 1) sb.Append(',');
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Represents a single puzzle piece (token). Immutable.
    /// </summary>
    public class Token : IEquatable<Token>
    {
        public List<(int X, int Y)> Points { get; } // Relative coordinates of filled cells
        public int Height { get; } // Bounding box height
        public int Width { get; } // Bounding box width
        public int Area => Points.Count; // Number of filled cells
        public int OriginalIndex { get; } // Original order index of the token from input

        /// <summary>
        /// Initializes a Token from a binary layout string (e.g., "11,01").
        /// </summary>
        /// <param name="layout">The string representing the token's shape.</param>
        /// <param name="originalIndex">The zero-based index of this token as it appeared in the input.</param>
        public Token(string layout, int originalIndex)
        {
            var rows = layout.Split(',');
            Height = rows.Length;
            Width = rows[0].Length;
            Points = new List<(int X, int Y)>();
            OriginalIndex = originalIndex;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (rows[y][x] == '1')
                    {
                        Points.Add((x, y));
                    }
                }
            }
        }

        /// <summary>
        /// Generates a hash code for the Token. For this problem, tokens are distinct
        /// if their original index is different (even if shapes are identical).
        /// </summary>
        public override int GetHashCode()
        {
            return OriginalIndex.GetHashCode();
        }

        /// <summary>
        /// Determines if this Token instance is equal to another Token instance.
        /// Equality is based solely on the OriginalIndex.
        /// </summary>
        public bool Equals(Token other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return OriginalIndex == other.OriginalIndex;
        }

        public override bool Equals(object obj) => Equals(obj as Token);
    }

    /// <summary>
    /// Represents a state in the IDA* search.
    /// Now directly stores the current Grid state.
    /// </summary>
    public class GameState : IEquatable<GameState>
    {
        public IReadOnlyList<Token> RemainingTokens { get; } // Tokens yet to be placed
        public BitGrid CurrentGrid { get; } // The actual grid state at this point
        public GameState ParentState { get; } // Reference to the state from which this one was derived
        public Token PlacedToken { get; } // The token placed to reach this state
        public (int X, int Y) PlacementCoords { get; } // The coordinates where PlacedToken was placed

        /// <summary>
        /// Constructor for the initial puzzle state (no parent, no placed token).
        /// </summary>
        /// <param name="remainingTokens">All tokens available at the start.</param>
        /// <param name="initialGrid">The grid at the very beginning of the puzzle.</param>
        public GameState(IReadOnlyList<Token> remainingTokens, BitGrid initialGrid)
        {
            RemainingTokens = remainingTokens;
            CurrentGrid = initialGrid;
            ParentState = null;
            PlacedToken = null;
            PlacementCoords = (-1, -1); // Sentinel value
        }

        /// <summary>
        /// Constructor for subsequent states derived from a parent state.
        /// </summary>
        /// <param name="remainingTokens">Tokens remaining after `placedToken` was used.</param>
        /// <param name="currentGrid">The grid state *after* `placedToken` has been placed.</param>
        /// <param name="parent">The GameState from which this state was reached.</param>
        /// <param name="placedToken">The token that was placed to create this state.</param>
        /// <param name="placementCoords">The (X, Y) coordinates of the `placedToken`.</param>
        public GameState(IReadOnlyList<Token> remainingTokens, BitGrid currentGrid, GameState parent, Token placedToken, (int X, int Y) placementCoords)
        {
            RemainingTokens = remainingTokens;
            CurrentGrid = currentGrid;
            ParentState = parent;
            PlacedToken = placedToken;
            PlacementCoords = placementCoords;
        }

        /// <summary>
        /// Determines if two GameState objects represent the same puzzle state.
        /// This involves comparing their remaining tokens and their direct grid states.
        /// </summary>
        public bool Equals(GameState other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            if (RemainingTokens.Count != other.RemainingTokens.Count)
                return false;

            for (int i = 0; i < RemainingTokens.Count; i++)
            {
                if (RemainingTokens[i].OriginalIndex != other.RemainingTokens[i].OriginalIndex)
                {
                    return false;
                }
            }

            return CurrentGrid.Equals(other.CurrentGrid);
        }

        public override bool Equals(object obj) => Equals(obj as GameState);

        /// <summary>
        /// Generates a hash code for a GameState object.
        /// This hash code must be consistent with the Equals method.
        /// </summary>
        public override int GetHashCode()
        {
            if (CurrentGrid == null) return 0;

            unchecked
            {
                int hash = 17;
                foreach (var token in RemainingTokens)
                {
                    hash = hash * 23 + token.OriginalIndex.GetHashCode();
                }
                hash = hash * 23 + CurrentGrid.GetHashCode();
                return hash;
            }
        }
    }

    /// <summary>
    /// Implements the IDA* search algorithm to solve the Shapeshifter puzzle.
    /// </summary>
    public class Solver
    {
        private readonly BitGrid initialGrid;
        private readonly IReadOnlyList<Token> tokens;
        private readonly CancellationToken cancelToken;
        private readonly Action<ulong> progressCallback;
        private ulong expandedNodes = 0;
        private const int MAX_DEPTH = 20;

        public Solver(BitGrid initialGrid, IReadOnlyList<Token> tokens,
            CancellationToken cancelToken = default, Action<ulong> progressCallback = null)
        {
            this.initialGrid = initialGrid;
            this.tokens = tokens;
            this.cancelToken = cancelToken;
            this.progressCallback = progressCallback;
        }

        /// <summary>
        /// Solves the puzzle using the IDA* search algorithm.
        /// </summary>
        /// <returns>A dictionary mapping original token indices to their placement (X, Y) coordinates
        /// if a solution is found; otherwise, returns null.</returns>
        public Dictionary<int, (int X, int Y)> SolveIDAStar()
        {
            cancelToken.ThrowIfCancellationRequested();

            var startState = new GameState(tokens, initialGrid);
            double initialThreshold = CalculateHeuristic(startState);
            Dictionary<int, (int X, int Y)> solution = null;

            // Iterative deepening loop
            double threshold = initialThreshold;
            while (solution == null && threshold < double.PositiveInfinity)
            {
                var seenStates = new Dictionary<GameState, double>();
                double newThreshold = double.PositiveInfinity;
                solution = Search(startState, 0, threshold, seenStates, ref newThreshold);
                threshold = newThreshold; // Update threshold for next iteration
                seenStates.Clear(); // Clear seen states to reset for next iteration
            }

            return solution;
        }

        /// <summary>
        /// Recursive depth-first search with threshold for IDA*.
        /// </summary>
        /// <param name="state">Current game state.</param>
        /// <param name="gScore">Cost from start to current state.</param>
        /// <param name="threshold">Current f-score threshold.</param>
        /// <param name="seenStates">Tracks visited states and their minimum g-scores.</param>
        /// <param name="newThreshold">Tracks the minimum f-score exceeding the current threshold.</param>
        /// <returns>Solution if found; otherwise, null.</returns>
        private Dictionary<int, (int X, int Y)> Search(GameState state, double gScore, double threshold,
            Dictionary<GameState, double> seenStates, ref double newThreshold)
        {
            cancelToken.ThrowIfCancellationRequested();

            double fScore = gScore + CalculateHeuristic(state);
            if (fScore > threshold)
            {
                newThreshold = Math.Min(newThreshold, fScore);
                return null;
            }

            UpdateProgress();

            // Check if goal state is reached
            if (state.CurrentGrid.IsCleared() && state.RemainingTokens.Count == 0)
            {
                return ReconstructSolutionPath(state);
            }

            // Check for cycles or better paths
            if (seenStates.TryGetValue(state, out double existingGScore) && gScore >= existingGScore)
            {
                return null;
            }
            seenStates[state] = gScore;

            // If no tokens remain, this is a dead-end
            if (state.RemainingTokens.Count == 0)
            {
                return null;
            }

            // Select the next token to place
            var tokenToPlace = state.RemainingTokens[0];
            var remainingTokens = state.RemainingTokens.Skip(1).ToList().AsReadOnly();

            // Explore all possible placements
            for (int row = 0; row <= state.CurrentGrid.Rows - tokenToPlace.Height; row++)
            {
                for (int col = 0; col <= state.CurrentGrid.Cols - tokenToPlace.Width; col++)
                {
                    cancelToken.ThrowIfCancellationRequested();

                    var nextGrid = state.CurrentGrid.PlaceToken(tokenToPlace, col, row, 1);
                    var nextState = new GameState(remainingTokens, nextGrid, state, tokenToPlace, (col, row));
                    double nextGScore = gScore + 1;

                    var result = Search(nextState, nextGScore, threshold, seenStates, ref newThreshold);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Reconstructs the sequence of token placements by backtracking from the `finalState`
        /// up to the initial state using the `ParentState` links.
        /// </summary>
        /// <param name="finalState">The GameState that represents the solved puzzle.</param>
        /// <returns>A dictionary mapping original token indices to their placement coordinates.</returns>
        private Dictionary<int, (int X, int Y)> ReconstructSolutionPath(GameState finalState)
        {
            var solution = new Dictionary<int, (int X, int Y)>();
            var pathStack = new Stack<(int OriginalTokenIndex, int X, int Y)>();
            GameState current = finalState;

            while (current.ParentState != null)
            {
                pathStack.Push((current.PlacedToken.OriginalIndex, current.PlacementCoords.X, current.PlacementCoords.Y));
                current = current.ParentState;
            }

            while (pathStack.Count > 0)
            {
                var (idx, x, y) = pathStack.Pop();
                solution[idx] = (x, y);
            }
            return solution;
        }

        /// <summary>
        /// Calculates the heuristic value for a given GameState.
        /// The heuristic estimates the cost from the current state to the goal.
        /// For this puzzle, a simple admissible heuristic is the number of filled tiles.
        /// </summary>
        /// <param name="state">The GameState for which to calculate the heuristic.</param>
        /// <returns>The heuristic value.</returns>
        private double CalculateHeuristic(GameState state)
        {
            return state.CurrentGrid.CountFilledTiles();
        }

        /// <summary>
        /// Increments the count of search nodes and invokes the progress callback
        /// periodically to update the UI.
        /// </summary>
        private void UpdateProgress()
        {
            expandedNodes++;
            if (expandedNodes % 10000 == 0)
                progressCallback?.Invoke(expandedNodes);
        }
    }

    public class RoundedLabel : System.Windows.Forms.Label
    {
        public int CornerRadius { get; set; } = 10;

        protected override void OnPaint(PaintEventArgs e)
        {
            // Don't call base.OnPaintBackground to avoid flickering
            // Instead, paint the rounded background yourself

            using (GraphicsPath path = new GraphicsPath())
            {
                Rectangle bounds = this.ClientRectangle;
                int r = CornerRadius;

                path.AddArc(bounds.X, bounds.Y, r, r, 180, 90);
                path.AddArc(bounds.Right - r, bounds.Y, r, r, 270, 90);
                path.AddArc(bounds.Right - r, bounds.Bottom - r, r, r, 0, 90);
                path.AddArc(bounds.X, bounds.Bottom - r, r, r, 90, 90);
                path.CloseFigure();

                this.Region = new Region(path);

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (SolidBrush brush = new SolidBrush(this.BackColor))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }

            // Draw the text centered
            TextRenderer.DrawText(
                e.Graphics,
                this.Text,
                this.Font,
                this.ClientRectangle,
                this.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
    public class DoubleBufferedFlowLayoutPanel : FlowLayoutPanel
    {
        public DoubleBufferedFlowLayoutPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
                return cp;
            }
        }
    }
}