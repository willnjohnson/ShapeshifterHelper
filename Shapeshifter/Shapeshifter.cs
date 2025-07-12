using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq; // For .Sum(), .All(), .SequenceEqual(), .Select(), .OrderBy()
using System.Text; // For StringBuilder
using System.Text.RegularExpressions; // For Regex parsing HTML
using System.Threading; // For CancellationTokenSource
using System.Threading.Tasks; // For Task.Run
using System.Windows.Forms; // For Windows Forms UI components

// Remove this line if targeting .NET Framework < 4.7.2 or .NET Core < 3.0
// If you remove it, the manual CountSetBits implementation will be used.
// If you are on .NET 5+ or .NET Core 3.0+, you could re-add it and use BitOperations.PopCount
// However, the provided manual CountSetBits is already efficient.
// using System.Numerics; // Commented out to ensure manual CountSetBits is used

namespace Shapeshifter
{
    public partial class ShapeShifter : Form
    {
        private CancellationTokenSource cancelSource = null;

        public ShapeShifter() => InitializeComponent();

        // Placeholder event handlers (can be removed if not used by designer)
        private void textBox1_TextChanged(object sender, EventArgs e) { }
        private void label1_Click(object sender, EventArgs e) { }
        private void label1_Click_1(object sender, EventArgs e) { }
        private void label1_Click_2(object sender, EventArgs e) { }
        private void Form1_Load(object sender, EventArgs e) { 
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false; 
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
        /// Initiates the A* search or cancels it if already running.
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
                        // Update UI on the main thread
                        this.Invoke((MethodInvoker)delegate
                        {
                            labelWaiting.Text = $"Calculating... [{count:N0} expanded nodes]";
                        });
                    });

                    placements = solver.SolveAStar(); // Attempt to solve
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
            textBoxInput.Enabled = !solving;
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
            textBoxStepsPanel.Controls.Clear(); // Clear previous results

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
                                                          // Optional: set a recognizable border style for debugging, then remove
                                                          // BorderStyle = BorderStyle.FixedSingle 
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
                                                          // Top margin (3px) to align with checkbox
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
                                                         // Top margin (3px) to align
                    };
                    flowPanel.Controls.Add(colLabel);

                    // Add the flowPanel (which contains the checkbox and labels)
                    textBoxStepsPanel.Controls.Add(flowPanel); // IMPORTANT: Add the FlowLayoutPanel, not the CheckBox directly
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
                    // Assumes their order based on how you add them in DisplayResults
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
                        // Set the c-th bit (0-indexed from right to left, or left to right depending on convention)
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
        /// Used as a heuristic in A* search (lower count means closer to goal).
        /// </summary>
        /// <returns>The number of filled (non-zero) tiles.</returns>
        public int CountFilledTiles() => _rows.Sum(CountSetBits); // Changed to use local CountSetBits

        /// <summary>
        /// Counts the number of set bits (1s) in a ulong using a software fallback.
        /// This method is necessary if BitOperations.PopCount is not available (e.g., older .NET Framework).
        /// </summary>
        private static int CountSetBits(ulong n)
        {
            // Efficient software-based PopCount implementation (Hamming weight)
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
            // Quick check on properties before comparing the array content
            if (Rows != other.Rows || Cols != other.Cols) return false;
            // Use SequenceEqual for efficient array content comparison
            return _rows.SequenceEqual(other._rows);
        }

        public override bool Equals(object obj) => Equals(obj as BitGrid);

        /// <summary>
        /// Generates a hash code for the BitGrid instance. Essential for efficient use
        /// in hash-based collections (Dictionary, HashSet).
        /// </summary>
        public override int GetHashCode()
        {
            unchecked // Allows integer overflows to be unchecked
            {
                int hash = 17; // Start with a prime number
                hash = hash * 23 + Rows.GetHashCode();
                hash = hash * 23 + Cols.GetHashCode();

                // Incorporate the hash of the ulong row contents.
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
                    // Check if the c-th bit is set
                    sb.Append((_rows[r] & (1UL << c)) != 0 ? '1' : '0');
                }
                if (r < Rows - 1) sb.Append(','); // Add comma between rows
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
    /// Represents a state in the A* search.
    /// Now directly stores the current Grid state.
    /// </summary>
    public class GameState : IEquatable<GameState> // Implements IEquatable directly
    {
        public IReadOnlyList<Token> RemainingTokens { get; } // Tokens yet to be placed
        public BitGrid CurrentGrid { get; } // The actual grid state at this point (now BitGrid)
        public GameState ParentState { get; } // Reference to the state from which this one was derived
        public Token PlacedToken { get; } // The token placed to reach this state
        public (int X, int Y) PlacementCoords { get; } // The coordinates where PlacedToken was placed

        /// <summary>
        /// Constructor for the initial puzzle state (no parent, no placed token).
        /// </summary>
        /// <param name="remainingTokens">All tokens available at the start.</param>
        /// <param name="initialGrid">The grid at the very beginning of the puzzle.</param>
        public GameState(IReadOnlyList<Token> remainingTokens, BitGrid initialGrid) // Changed Grid to BitGrid
        {
            RemainingTokens = remainingTokens;
            CurrentGrid = initialGrid; // The initial grid is the current grid
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
        public GameState(IReadOnlyList<Token> remainingTokens, BitGrid currentGrid, GameState parent, Token placedToken, (int X, int Y) placementCoords) // Changed Grid to BitGrid
        {
            RemainingTokens = remainingTokens;
            CurrentGrid = currentGrid; // The grid *after* placing the token
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

            // 1. Compare RemainingTokens: Check if the sequence of original indices is the same.
            if (RemainingTokens.Count != other.RemainingTokens.Count)
                return false;

            for (int i = 0; i < RemainingTokens.Count; i++)
            {
                if (RemainingTokens[i].OriginalIndex != other.RemainingTokens[i].OriginalIndex)
                {
                    return false;
                }
            }

            // 2. Compare the actual grid state.
            return CurrentGrid.Equals(other.CurrentGrid);
        }

        public override bool Equals(object obj) => Equals(obj as GameState);

        /// <summary>
        /// Generates a hash code for a GameState object.
        /// This hash code must be consistent with the Equals method.
        /// </summary>
        public override int GetHashCode()
        {
            if (CurrentGrid == null) return 0; // Handle null grid for consistency

            unchecked
            {
                int hash = 17;

                // Hash the sequence of remaining token original indices.
                foreach (var token in RemainingTokens)
                {
                    hash = hash * 23 + token.OriginalIndex.GetHashCode();
                }

                // Hash the current grid state directly.
                hash = hash * 23 + CurrentGrid.GetHashCode();

                return hash;
            }
        }
    }

    /// <summary>
    /// Implements the A* search algorithm to solve the Shapeshifter puzzle.
    /// </summary>
    public class Solver
    {
        private readonly BitGrid initialGrid; // Changed Grid to BitGrid
        private readonly IReadOnlyList<Token> tokens; // All available tokens
        private readonly CancellationToken cancelToken; // For cancellation requests
        private readonly Action<ulong> progressCallback; // Callback for UI updates
        private ulong expandedNodes = 0; // Counter for expanded nodes

        public Solver(BitGrid initialGrid, IReadOnlyList<Token> tokens, // Changed Grid to BitGrid
            CancellationToken cancelToken = default, Action<ulong> progressCallback = null)
        {
            this.initialGrid = initialGrid;
            this.tokens = tokens;
            this.cancelToken = cancelToken;
            this.progressCallback = progressCallback;
        }

        /// <summary>
        /// Solves the puzzle using the A* search algorithm.
        /// </summary>
        /// <returns>A dictionary mapping original token indices to their placement (X, Y) coordinates
        /// if a solution is found; otherwise, returns null.</returns>
        public Dictionary<int, (int X, int Y)> SolveAStar()
        {
            cancelToken.ThrowIfCancellationRequested();

            // Create the initial state of the puzzle, passing the initial grid
            var startState = new GameState(tokens, initialGrid);

            // Priority queue for open nodes (states to be explored)
            var frontier = new PriorityQueue<GameState>();

            // Dictionaries for g-score (cost from start) and f-score (g-score + heuristic)
            var gScore = new Dictionary<GameState, double> { [startState] = 0 };
            var fScore = new Dictionary<GameState, double> { [startState] = CalculateHeuristic(startState) };

            // HashSet for explored nodes (states already visited)
            var explored = new HashSet<GameState>();

            // Add the start state to the frontier
            frontier.Push(startState, fScore[startState]);

            // Main A* search loop
            while (!frontier.IsEmpty())
            {
                cancelToken.ThrowIfCancellationRequested(); // Check for cancellation

                var currentState = frontier.Pop(); // Get the state with the lowest f-score

                // Direct access to the current grid state from the GameState object itself
                BitGrid currentGrid = currentState.CurrentGrid; // Changed Grid to BitGrid

                // Check if the goal state is reached: grid is cleared and no tokens remain
                if (currentGrid.IsCleared() && currentState.RemainingTokens.Count == 0)
                {
                    // Goal reached, reconstruct and return the solution path
                    return ReconstructSolutionPath(currentState);
                }

                // If this state has already been fully explored with an equal or better path, skip it.
                if (explored.Contains(currentState)) continue;
                explored.Add(currentState); // Mark current state as explored

                UpdateProgress(); // Update progress counter for UI

                // If no tokens left and grid not cleared, this is a dead-end path
                if (currentState.RemainingTokens.Count == 0) continue;

                // Select the next token to place (typically the first one in the remaining list)
                var tokenToPlace = currentState.RemainingTokens[0];
                // Create a new list of remaining tokens for the next states
                var remainingTokensForNextState = currentState.RemainingTokens.Skip(1).ToList().AsReadOnly();

                // Iterate through all possible placement positions for the selected token
                for (int row = 0; row <= currentGrid.Rows - tokenToPlace.Height; row++)
                {
                    for (int col = 0; col <= currentGrid.Cols - tokenToPlace.Width; col++)
                    {
                        cancelToken.ThrowIfCancellationRequested();

                        // Create the next Grid state by applying the token placement to the current grid
                        BitGrid nextGrid = currentGrid.PlaceToken(tokenToPlace, col, row, 1); // Changed Grid to BitGrid

                        // Create the next GameState. Pass the newly computed nextGrid.
                        var nextState = new GameState(remainingTokensForNextState, nextGrid, currentState, tokenToPlace, (col, row));

                        double tentativeGScore = gScore[currentState] + 1; // Each move costs 1

                        // If a path to nextState has been found before, and this new path is not better, skip.
                        if (gScore.TryGetValue(nextState, out double existingGScore) && tentativeGScore >= existingGScore)
                        {
                            continue;
                        }

                        // If the nextState is already in 'explored' with a worse gScore, it needs to be 're-opened'
                        // and re-added to the frontier.
                        // This check must happen *before* updating gScore/fScore and pushing to frontier
                        // to correctly implement the A* "re-opening" behavior.
                        bool wasExplored = explored.Remove(nextState); // Try to remove from explored

                        // This path to nextState is new or better.
                        gScore[nextState] = tentativeGScore;
                        fScore[nextState] = tentativeGScore + CalculateHeuristic(nextState); // Calculate f-score
                        frontier.Push(nextState, fScore[nextState]); // Add nextState to the frontier
                    }
                }
            }

            return null; // No solution found after exhausting the frontier
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

            // Traverse back up the parent chain until the initial state (ParentState == null)
            while (current.ParentState != null)
            {
                pathStack.Push((current.PlacedToken.OriginalIndex, current.PlacementCoords.X, current.PlacementCoords.Y));
                current = current.ParentState;
            }

            // Pop from the stack to get the placements in the correct chronological order
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
            // Directly access the CurrentGrid property of the state.
            return state.CurrentGrid.CountFilledTiles();
        }

        /// <summary>
        /// Increments the count of search nodes and invokes the progress callback
        /// periodically to update the UI.
        /// </summary>
        private void UpdateProgress()
        {
            expandedNodes++;
            // Update UI only after a certain number of nodes have been searched through expansion
            if (expandedNodes % 10000 == 0) // Adjust this number for desired UI update frequency
                progressCallback?.Invoke(expandedNodes);
        }
    }

    /// <summary>
    /// A generic min-priority queue implementation using a binary heap.
    /// It stores items along with their priority and a tie-breaking count.
    /// </summary>
    /// <typeparam name="T">The type of items stored in the priority queue.</typeparam>
    public class PriorityQueue<T>
    {
        // Internal heap representation: a list of tuples (Priority, InsertionOrderCount, Item)
        private readonly List<(double Priority, int Count, T Item)> heap = new List<(double, int, T)>();
        private int count = 0; // Used to maintain insertion order for tie-breaking

        /// <summary>
        /// Adds an item to the priority queue with a given priority.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <param name="priority">The priority of the item (lower value means higher priority).</param>
        public void Push(T item, double priority)
        {
            heap.Add((priority, count++, item)); // Add to end
            HeapifyUp(heap.Count - 1); // Restore heap property by moving up
        }

        /// <summary>
        /// Removes and returns the item with the highest priority (lowest priority value).
        /// </summary>
        /// <returns>The highest priority item.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the queue is empty.</exception>
        public T Pop()
        {
            if (IsEmpty()) throw new InvalidOperationException("Priority queue is empty.");

            var item = heap[0].Item; // Get the root (highest priority item)
            heap[0] = heap[heap.Count - 1]; // Move last item to root
            heap.RemoveAt(heap.Count - 1); // Remove the last item (now duplicated at root)
            HeapifyDown(0); // Restore heap property from the root
            return item;
        }

        /// <summary>
        /// Checks if the priority queue is empty.
        /// </summary>
        public bool IsEmpty() => heap.Count == 0;

        /// <summary>
        /// Restores the heap property by moving an item up the heap.
        /// </summary>
        /// <param name="index">The index of the item to move up.</param>
        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                // If child has higher priority (smaller value) than parent, swap
                if (CompareItems(heap[index], heap[parentIndex]) >= 0) break;
                Swap(index, parentIndex);
                index = parentIndex;
            }
        }

        /// <summary>
        /// Restores the heap property by moving an item down the heap.
        /// </summary>
        /// <param name="index">The index of the item to move down.</param>
        private void HeapifyDown(int index)
        {
            while (true)
            {
                int leftChild = 2 * index + 1;
                int rightChild = 2 * index + 2;
                int smallest = index; // Assume current node is the smallest

                // Check left child
                if (leftChild < heap.Count && CompareItems(heap[leftChild], heap[smallest]) < 0)
                    smallest = leftChild;

                // Check right child
                if (rightChild < heap.Count && CompareItems(heap[rightChild], heap[smallest]) < 0)
                    smallest = rightChild;

                // If the smallest is still the current node, heap property is restored
                if (smallest == index) break;

                // Otherwise, swap with the smallest child and continue heapifying down
                Swap(index, smallest);
                index = smallest;
            }
        }

        /// <summary>
        /// Compares two items based on their priority, then their insertion order (Count) for tie-breaking.
        /// </summary>
        private int CompareItems((double Priority, int Count, T Item) a, (double Priority, int Count, T Item) b)
        {
            int priorityComp = a.Priority.CompareTo(b.Priority);
            // If priorities are equal, use the insertion order count to break ties.
            // This ensures a stable sort and helps A* to explore states consistently.
            return priorityComp != 0 ? priorityComp : a.Count.CompareTo(b.Count);
        }

        /// <summary>
        /// Swaps two items in the heap list.
        /// </summary>
        private void Swap(int i, int j) => (heap[i], heap[j]) = (heap[j], heap[i]);
    }
}