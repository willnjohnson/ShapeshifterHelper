/*
 * shapeshifter_dll.c — DLL-compatible version of ss.c for C# use
 *
 * Based on the original solver by Kvho (GPL v2) -- see link to the preserved version:
 *     https://github.com/jimrustle/neopets-shapeshifter/blob/1c31a419971de3f25cac9a5dfc4fa32ca4aa7605/kvho_ss/ss.c
 *
 * Modified by @willnjohnson
 *
 * Modification(s):
 * - Exposed solver via __declspec(dllexport) for use in C# projects
 * - Input/output converted from stdin/stdout to char* buffers
 * - Replaced exit() with safe error returns
 * - Removed signal handling, main(), and interactive logic
 * - Static globals preserved: NOT thread-safe
 *
 * Known issue(s):
 * - Cannot cancel DLL mid-process when pressing Stop button from C# application. Aggressively added cancel_requested checks (now commented), but to no avail.
 *      (You'll just have to close and reopen the C# application, if you want to quit out of a long operation)
 * ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
⠀*⠀⠀⠀⠀⠀⠀⠀⠀⠀⣀⡈⢯⡉⠓⠦⣄⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀ ____  _     ____  ____  _____ ____  _     _  _____ _____  _____ ____
⠀*⠀⠀⠀⠀⠀⠀⠀⠀⠀⠻⣉⠹⠷⠀⠀⠀⠙⢷⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀/ ___\/ \ /|/  _ \/  __\/  __// ___\/ \ /|/ \/    //__ __\/  __//  __\
⠀*⠀⠀⠀⠀⠀⠀⠀⠀⠀⣠⠞⠀⠀⠀⠀⠀⠀⠀⢿⡇⠀⠀⠀⠀⠀⠀⠀⠀|    \| |_||| / \||  \/||  \  |    \| |_||| ||  __\  / \  |  \  |  \/|
⠀*⠀⠀⠀⠀⠀⠀⠀⠀⠀⡇⠀⠀⠀⠀⠀⠀⠀⠀⢈⡇⠀⠀⠀⠀⠀⠀⠀⠀\___ || | ||| |-|||  __/|  /_ \___ || | ||| || |     | |  |  /_ |    /
⠀*⠀⠀⠀⠀⠀⠀⠀⠀⠀⡇⠀⠹⠝⠀⠀⠀⠀⠀⣼⠃⠀⠀⠀⠀⠀⠀⠀⠀\____/\_/ \|\_/ \|\_/   \____\\____/\_/ \|\_/\_/     \_/  \____\\_/\_\
⠀*⠀⠀⠀⠀⠀⠀⠀⣠⠞⠀⣀⣠⣤⣤⠄⠀⠀⢠⡏⠀⠀⠀⠀⠀⠀⠀⠀⠀
⠀*⠀⠀⠀⠀⠀⠀⠚⠢⠼⠿⠟⢛⣾⠃⠀⠀⠀⢸⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀ _     _____ _     ____  _____ ____
⠀*⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⡴⣻⠃⠀⠀⠀⠀⢸⡉⠀⠀⠀⠀⠀⠀⠀⠀⠀/ \ /|/  __// \   /  __\/  __//  __\
⠀*⠀⠀⠀⠀⠀⠀⠀⠀⣰⢻⡷⠁⠀⠀⠀⠀⠀⢸⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀| |_|||  \  | |   |  \/||  \  |  \/|
⠀*⠀⠀⠀⠀⠀⠀⠀⢰⢽⡟⠁⠀⠀⠀⠀⠀⠀⠀⣇⠀⠀⠀⠀⠀⠀⠀⠀⠀| | |||  /_ | |_/\|  __/|  /_ |    /
⠀*⠀⠀⠀⠀⠀⠀⠀⢾⣿⠀⠀⠀⠀⠀⠀⠀⠀⠀⣸⡆⠀⠀⠀⠀⠀⠀⠀⠀\_/ \|\____\\____/\_/   \____\\_/\_\
⠀*⠀⠀⠀⠀⠀⠀⠀⢸⣿⡄⠀⠀⠀⠀⠀⠀⠀⠀⠀⣿⡀⠀⠀⠀⠀⠀⠀⠀
⠀*⠀⠀⠀⠀⠀⠀⠀⠘⢧⣳⡀⠀⠀⠀⠀⠀⠀⠀⠀⠘⣷⠀⠀⠀⠀⠀⠀⠀      Version 2.0.2
⠀*⠀⠀⠀⠀⠀⠀⠀⠀⠈⣷⣱⡀⠀⠀⠀⠀⣸⠀⠀⠀⠈⢻⣦⠀⠀⠀⠀⠀
⠀*⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⣷⡙⣆⠀⠀⣾⠃⠀⠀⠀⠀⠈⢽⡆⠀⠀⠀⠀      CSharp Application by @willnjohnson
⠀*⠀⠀⠀⠀⠀⠀⠀⠀⠀⠸⡇⢷⡏⠃⢠⠇⠀⠀⣀⠄⠀⠀⠀⣿⡖⠀⠀⠀
⠀*⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⡇⢨⠇⠀⡼⢀⠔⠊⠀⠀⠀⠀⠀⠘⣯⣄⢀⠀      Algorithm by Kvho (C code modified into DLL-compatible C code)
⠀*⠀⠀⠀⠀⠀⠀⠀⠀⠀⢰⡇⣼⡀⣰⣷⠁⠀⠀⠀⠀⠀⠀⠀⠀⣇⢻⣧⡄
⠀*⠀⠀⠀⠀⠀⠀⣀⣮⣿⣿⣿⣯⡭⢉⠟⠛⠳⢤⣄⣀⣀⣀⣀⡴⢠⠨⢻⣿      File: shapeshifter_dll.c
⠀*⠀   ⢀⣾⣿⣿⣿⣿⢏⠓⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⢨⣿
⠀*   ⣰⣿⣿⣿⣿⣿⣿⡱⠌⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⢭⣾⠏      Description: Re-written C code to be compiled in DLL (use Visual Studio C++ to compile, not MINGW or other compiler toolchains).
 *  ⣰⡿⠟⠋⠛⢿⣿⣿⣊⠡⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⣠⣼⡿⠋⠀
 * ⠋⠁⠀⠀⠀⠀⠈⠑⠿⢶⣄⣀⣀⣀⣀⣀⣄⣤⡶⠿⠟⠋⠁⠀⠀⠀
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <limits.h>
#include <ctype.h> // For isdigit
#include <stdarg.h> // Required for va_start, va_end, vsnprintf

 // Define for Windows DLL export
#ifdef _WIN32
#include <intrin.h> // For _ReadWriteBarrier() if needed for volatile
#define EXPORT __declspec(dllexport)
#else
#define EXPORT
#endif

// --- Structures from original ss.c ---
struct shape1 {
    unsigned int  nr;
    unsigned int  npoints; /* will be incremented by 1
                            to store separating 0 pointer */
    unsigned int* points;
    unsigned int  x;
    unsigned int  y;
    unsigned int  total;
};
struct shape {
    struct shape1 a;
    int** cache;
    struct shape* equal; // Pointer to another shape if they are logically equivalent
    int           incs;  // Number of increments remaining for this shape type
    int           seq;   // Sequence/placement index for this shape
};

// --- Static Global Variables from original ss.c ---
static unsigned int nshapes;
static unsigned int* max_incs_left; // Declared but unused in original find_sequence logic
static unsigned int x_dim, y_dim;
static unsigned int ntokens;
static int            max_print_cols;
static struct shape* shapes;    // Array of 'struct shape' for processing
static struct shape1* shapes1;  // Array of 'struct shape1' from initial parsing
static unsigned int max_token;  // Max token value + 1 (e.g., if tokens are 0,1,2, max_token is 3)
static unsigned int last_token; // Highest token value encountered during matrix read
static int* matrix;             // The current state of the game board
static int* start_matrix;       // The initial state of the game board
static int** result_matrices;    // Array of matrices to store intermediate results for printing
static int** shape_graphs;       // Array of matrices to show shape placements for printing
static unsigned int goal_token; // The target token value (usually 0)

// --- Custom Input/Output Buffers and Pointers ---
static const char* input_buffer_ptr = NULL; // Pointer to the C# input string
static int input_offset = 0;              // Current read offset in the input string

static char* output_buffer = NULL;  // Dynamically growing buffer for output string
static size_t output_capacity = 0;  // Current capacity of output_buffer
static size_t output_length = 0;    // Current length of output_buffer

// Cancel flag - must be volatile for proper multithreaded access
static volatile int cancel_requested = 0;

// --- Helper Functions for DLL (replacing scanf/printf) ---

// Resets internal state for a new solve operation
static void reset_solver_state(void) {
    // 1. Free structures that contain pointers INTO other large blocks first.
    // This ensures we don't try to access freed memory when iterating or accessing sub-pointers.

    // Free internal 'points' arrays within shapes1 structs FIRST.
    // Loop uses nshapes from the previous successful run's setup.
    if (shapes1) {
        // Safe iteration: check 'nshapes' is not ridiculously large if it's corrupted.
        // As nshapes is bounded by input read and checked, this should be fine.
        for (unsigned int i = 0; i < nshapes; i++) {
            if (shapes1[i].points) {
                free(shapes1[i].points);
                shapes1[i].points = NULL; // Crucial: nullify individual pointers
            }
        }
        free(shapes1); // Free the array of shape1 structs itself
        shapes1 = NULL; // Crucial: nullify the global pointer
    }

    // Free internal 'cache' arrays within shapes structs.
    // Loop uses nshapes from the previous successful run's setup.
    if (shapes) {
        for (unsigned int i = 0; i < nshapes; i++) {
            // shape->cache points into a larger block starting at (original_malloc_ptr + 1)
            // So we free (shape->cache - 1) to get back the original malloc'd pointer.
            if (shapes[i].cache) {
                free(shapes[i].cache - 1);
                shapes[i].cache = NULL; // Crucial: nullify individual pointers
            }
        }
        free(shapes); // Free the array of shape structs itself
        shapes = NULL; // Crucial: nullify the global pointer
    }


    // 2. Free matrices. These are arrays of pointers, then the blocks they point to.
    // 'max_print_cols' must reflect the previous allocation size for these loops to be safe.
    // If it was corrupted or not set during a previous failed 'initialise', there's risk.
    // However, it's initialized to 1 and updated by read_matrix_layout, so usually fine.

    // Free the inner arrays pointed to by result_matrices[i], then the outer result_matrices array itself.
    if (result_matrices) {
        for (int i = 0; i <= max_print_cols; i++) {
            if (result_matrices[i]) {
                free(result_matrices[i]);
                result_matrices[i] = NULL; // Nullify individual pointers
            }
        }
        free(result_matrices); // Free the array of pointers itself
        result_matrices = NULL; // Nullify the global pointer
    }

    // Free the inner arrays pointed to by shape_graphs[i], then the outer shape_graphs array itself.
    // This was a key bug source previously: not freeing the individual `shape_graphs[i]` blocks.
    if (shape_graphs) {
        for (int i = 0; i <= max_print_cols; i++) {
            if (shape_graphs[i]) { // Each shape_graphs[i] points to an allocated int array
                free(shape_graphs[i]);
                shape_graphs[i] = NULL; // Nullify individual pointers
            }
        }
        free(shape_graphs); // Free the array of pointers itself
        shape_graphs = NULL; // Nullify the global pointer
    }

    // 3. Free single-block allocations (matrix, start_matrix, max_incs_left).
    if (matrix) {
        free(matrix);
        matrix = NULL;
    }
    if (start_matrix) {
        free(start_matrix);
        start_matrix = NULL;
    }
    if (max_incs_left) {
        free(max_incs_left);
        max_incs_left = NULL;
    }

    // Reset output buffer (this is managed as a single block)
    if (output_buffer) {
        free(output_buffer);
        output_buffer = NULL;
        output_capacity = 0;
        output_length = 0;
    }

    // Reset all other global static variables to their initial "empty" state
    nshapes = 0;
    x_dim = 0;
    y_dim = 0;
    ntokens = 0;
    max_print_cols = 1; // Reset to default initial value
    max_token = 0;
    last_token = 0;
    goal_token = 0;

    // Input buffer pointers are not dynamically allocated by the DLL.
    input_buffer_ptr = NULL;
    input_offset = 0;

    // Reset cancel flag for the next run
    cancel_requested = 0;
}

// Reads an unsigned integer from the global input_buffer_ptr
static unsigned int read_int_from_input(void) {
    unsigned int val = 0;
    // Skip non-digit characters (like newline or space)
    while (input_buffer_ptr[input_offset] != '\0' && !isdigit((unsigned char)input_buffer_ptr[input_offset])) {
        input_offset++;
    }
    // Read digits
    if (input_buffer_ptr[input_offset] == '\0') {
        // Handle unexpected end of input or no digits found where expected
        return UINT_MAX; // Indicate error by returning max unsigned int value
    }
    while (input_buffer_ptr[input_offset] != '\0' && isdigit((unsigned char)input_buffer_ptr[input_offset])) {
        // Prevent overflow during conversion for very large numbers
        if (val > UINT_MAX / 10 || (val == UINT_MAX / 10 && (input_buffer_ptr[input_offset] - '0') > UINT_MAX % 10)) {
            return UINT_MAX; // Indicate overflow
        }
        val = val * 10 + (input_buffer_ptr[input_offset] - '0');
        input_offset++;
    }
    return val;
}

// Appends formatted string to the global output_buffer, growing it as needed
static void append_to_output(const char* format, ...) {
    va_list args;
    va_start(args, format);

    // Determine required size using vsnprintf with a temporary buffer
    char temp_buffer[1024]; // A reasonable stack buffer size for most single lines
    int needed = vsnprintf(temp_buffer, sizeof(temp_buffer), format, args);
    va_end(args);

    if (needed < 0) { // Encoding error or other issue with vsnprintf
        // Clear output buffer to signal an error state for the whole operation
        if (output_buffer) free(output_buffer);
        output_buffer = NULL;
        output_capacity = 0;
        output_length = 0;
        return;
    }

    // If needed size exceeds temp_buffer, determine actual needed size correctly
    // vsnprintf(NULL, 0, ...) returns the number of characters needed EXCLUDING the null terminator.
    if ((size_t)needed >= sizeof(temp_buffer)) {
        va_list args_retry;
        va_start(args_retry, format);
        needed = vsnprintf(NULL, 0, format, args_retry); // Get actual required size (excluding null terminator)
        va_end(args_retry);
        if (needed < 0) {
            if (output_buffer) free(output_buffer); output_buffer = NULL; output_capacity = 0; output_length = 0;
            return;
        }
    }


    // Resize buffer if necessary to accommodate new content + null terminator
    if (output_length + needed + 1 > output_capacity) {
        size_t new_capacity = output_capacity == 0 ? 1024 : output_capacity * 2; // Start with 1KB, then double
        // Ensure new_capacity is at least large enough for current content + new needed content + null terminator
        while (new_capacity < output_length + needed + 1) {
            new_capacity *= 2;
        }

        char* new_buffer = (char*)realloc(output_buffer, new_capacity);
        if (new_buffer == NULL) {
            // Memory allocation error during realloc
            if (output_buffer) free(output_buffer);
            output_buffer = NULL;
            output_capacity = 0;
            output_length = 0;
            return;
        }
        output_buffer = new_buffer;
        output_capacity = new_capacity;
    }

    // Append formatted string to buffer
    // Re-call vsnprintf if temp_buffer was too small initially.
    // If temp_buffer was sufficient, copy from temp_buffer.
    if ((size_t)needed >= sizeof(temp_buffer)) { // If it required more than temp_buffer
        va_list args_final;
        va_start(args_final, format);
        vsnprintf(output_buffer + output_length, needed + 1, format, args_final);
        va_end(args_final);
    }
    else { // temp_buffer was sufficient
        memcpy(output_buffer + output_length, temp_buffer, needed);
    }
    output_length += needed;
    output_buffer[output_length] = '\0'; // Null-terminate the entire buffer
}

// --- Original Helper Functions (modified to use custom I/O) ---

static void initialise(void);
static void find_sequence(void);
static void print_result(int print_matrix);
static void read_matrix_layout(void);
static void read_matrix(void);
static void read_goal_token(void);
static void set_toggle(void);
static void read_shapes(void);
static void prepare_shapes(void);
static void print_result_matrix(int from, int to);
static void copy_shape_to_result(int* shape_graph, struct shape* shape, int** cache);
static void* xmalloc(size_t size);
// Simplified allocation macro. If xmalloc returns NULL, it sets output_buffer=NULL.
// Caller must check the return value and handle output_buffer == NULL.
#define alloc_arr(arr,nr) arr=xmalloc(sizeof(arr[0])*(nr))
static void read_shape(struct shape1* shape);

// --- Implementation of Original Helper Functions (modified) ---

static void initialise(void) {
    // Each call needs to check output_buffer to propagate errors/cancellation
    read_matrix_layout();
    if (!output_buffer) return;
    read_matrix();
    if (!output_buffer) return;
    read_goal_token();
    if (!output_buffer) return;
    set_toggle();
    if (!output_buffer) return;
    read_shapes();
    if (!output_buffer) return;
    prepare_shapes();
    if (!output_buffer) return;
}

static void read_matrix_layout(void) {
    x_dim = read_int_from_input();
    if (x_dim == UINT_MAX || x_dim == 0) { // Error reading or overflow, or invalid dimension
        if (output_buffer) free(output_buffer); output_buffer = NULL; output_capacity = 0; output_length = 0; return;
    }
    y_dim = read_int_from_input();
    if (y_dim == UINT_MAX || y_dim == 0) { // Error reading or overflow, or invalid dimension
        if (output_buffer) free(output_buffer); output_buffer = NULL; output_capacity = 0; output_length = 0; return;
    }

    ntokens = x_dim * y_dim;
    max_print_cols = 1; // Default from original problem/solver behavior
}

static void read_matrix(void) {
    unsigned int i, token;

    // Handle potential malloc(0) if dimensions are 0 (from bad input)
    if (ntokens == 0) {
        if (output_buffer) free(output_buffer); output_buffer = NULL; output_capacity = 0; output_length = 0; return;
    }

    alloc_arr(matrix, ntokens);
    if (!matrix) return; // xmalloc sets output_buffer to NULL on failure
    alloc_arr(start_matrix, ntokens);
    if (!start_matrix) { free(matrix); matrix = NULL; return; } // Cleanup matrix on failure

    // Allocate result_matrices (array of int pointers) and shape_graphs (array of int pointers)
    // max_print_cols is usually 1, so this is small.
    alloc_arr(result_matrices, max_print_cols + 1);
    if (!result_matrices) { free(matrix); matrix = NULL; free(start_matrix); start_matrix = NULL; return; }
    alloc_arr(shape_graphs, max_print_cols + 1);
    if (!shape_graphs) {
        free(matrix); matrix = NULL; free(start_matrix); start_matrix = NULL;
        free(result_matrices); result_matrices = NULL; return;
    }

    // Initialize individual pointers within result_matrices and shape_graphs arrays to NULL.
    // This is crucial for cleanup in case a later allocation fails.
    for (i = 0; i <= max_print_cols; i++) {
        result_matrices[i] = NULL;
        shape_graphs[i] = NULL;
    }

    // Allocate actual integer arrays for each entry in result_matrices and shape_graphs
    for (i = 0; i <= max_print_cols; i++) {
        alloc_arr(result_matrices[i], ntokens);
        if (!result_matrices[i]) {
            // Cleanup all previous allocations on failure for result_matrices
            for (unsigned int j = 0; j < i; ++j) { if (result_matrices[j]) free(result_matrices[j]); }
            free(result_matrices); result_matrices = NULL;
            // Also free already allocated shape_graphs arrays (for j < i) and the main array
            for (unsigned int j = 0; j < i; ++j) { if (shape_graphs[j]) free(shape_graphs[j]); }
            free(shape_graphs); shape_graphs = NULL;
            free(matrix); matrix = NULL; free(start_matrix); start_matrix = NULL;
            return; // xmalloc already set output_buffer to NULL
        }
        alloc_arr(shape_graphs[i], ntokens);
        if (!shape_graphs[i]) {
            // Cleanup all previous allocations on failure for shape_graphs
            for (unsigned int j = 0; j <= i; ++j) { if (result_matrices[j]) free(result_matrices[j]); }
            free(result_matrices); result_matrices = NULL;
            for (unsigned int j = 0; j < i; ++j) { if (shape_graphs[j]) free(shape_graphs[j]); } // Only up to i-1
            free(shape_graphs); shape_graphs = NULL;
            free(matrix); matrix = NULL; free(start_matrix); start_matrix = NULL;
            return; // xmalloc already set output_buffer to NULL
        }
    }

    last_token = 0;
    for (i = 0; i < ntokens; ++i) {
        token = read_int_from_input();
        if (token == UINT_MAX) { // Error reading token or overflow
            if (output_buffer) free(output_buffer); output_buffer = NULL; output_capacity = 0; output_length = 0; return;
        }

        if (token > last_token) {
            last_token = token;
        }
        matrix[i] = token;
    }

    // Validate tokens vs board size
    if (last_token >= ntokens) { // Tokens should be 0 to ntokens-1
        if (output_buffer) free(output_buffer); output_buffer = NULL; output_capacity = 0; output_length = 0; return;
    }
    max_token = last_token + 1; // max_token is the count of unique token values (0-indexed)
}

// Memory allocation wrapper. Sets global output_buffer to NULL on failure.
static void* xmalloc(size_t size) {
    void* p = malloc(size);
    if (p == NULL) {
        // Error: Out of memory. Clear global output buffer to signal failure.
        if (output_buffer) free(output_buffer);
        output_buffer = NULL;
        output_capacity = 0;
        output_length = 0;
    }
    return p;
}

static void read_goal_token(void) {
    goal_token = read_int_from_input();
    if (goal_token == UINT_MAX) { // Error reading or overflow
        if (output_buffer) free(output_buffer); output_buffer = NULL; output_capacity = 0; output_length = 0; return;
    }
    if (goal_token >= max_token) { // Goal token out of range for defined tokens
        if (output_buffer) free(output_buffer); output_buffer = NULL; output_capacity = 0; output_length = 0; return;
    }
}

// Corrected: Uses dynamic allocation for new_order to avoid fixed-size array issues.
static void set_toggle(void) {
    // If max_token is 0, this indicates a problem from previous setup, prevent malloc(0)
    if (max_token == 0) {
        if (output_buffer) free(output_buffer); output_buffer = NULL; output_capacity = 0; output_length = 0; return;
    }

    // Dynamically allocate new_order based on the actual max_token
    unsigned int* new_order = (unsigned int*)xmalloc(sizeof(unsigned int) * max_token);
    if (!new_order) { // xmalloc handles setting output_buffer to NULL on failure
        return;
    }

    for (int i = 0; i < (int)max_token; i++) {
        if (i <= (int)goal_token) new_order[i] = goal_token - i;
        else new_order[i] = goal_token + max_token - i;
    }
    // Ensure matrix is valid and has ntokens elements before using.
    // This is implicitly guaranteed if read_matrix succeeded.
    if (!matrix || !start_matrix) {
        free(new_order); new_order = NULL; // Clean up local allocation
        if (output_buffer) free(output_buffer); output_buffer = NULL; return;
    }
    for (int i = 0;i < (int)ntokens;i++) {
        // matrix[i] is the original token value (0 to last_token).
        // Since max_token = last_token + 1, new_order[matrix[i]] is a safe access.
        matrix[i] = new_order[matrix[i]];
    }
    memcpy(start_matrix, matrix, sizeof(matrix[0]) * ntokens);

    free(new_order); // Free the dynamically allocated array
    new_order = NULL;
}

static void read_shapes(void) {
    unsigned int i;

    nshapes = read_int_from_input();
    if (nshapes == UINT_MAX) { // Error reading or overflow
        if (output_buffer) free(output_buffer); output_buffer = NULL; output_capacity = 0; output_length = 0; return;
    }
    if (nshapes == 0) { // Invalid number of shapes
        if (output_buffer) free(output_buffer); output_buffer = NULL; output_capacity = 0; output_length = 0; return;
    }

    alloc_arr(shapes1, nshapes);
    if (!shapes1) return; // xmalloc handles setting output_buffer to NULL on failure

    // Initialize all shapes1[i].points to NULL.
    // This is CRITICAL for robust cleanup if read_shape fails mid-loop,
    // ensuring reset_solver_state or subsequent manual cleanup only frees valid pointers.
    for (i = 0; i < nshapes; ++i) {
        shapes1[i].points = NULL;
    }

    for (i = 0; i < nshapes; ++i) {
        read_shape(&shapes1[i]);
        // If read_shape fails (sets output_buffer to NULL, indicating error/cancellation)
        // we must clean up any points arrays already successfully allocated for shapes < i.
        if (!output_buffer) {
            // Manually clean up shapes1[j].points for already processed shapes (j < i)
            for (unsigned int j_cleanup = 0; j_cleanup < i; ++j_cleanup) {
                if (shapes1[j_cleanup].points) {
                    free(shapes1[j_cleanup].points);
                    shapes1[j_cleanup].points = NULL; // Nullify after freeing
                }
            }
            free(shapes1); // Free the main shapes1 array
            shapes1 = NULL;
            // output_buffer already cleared by xmalloc/read_shape
            return; // Exit
        }
        shapes1[i].nr = i; // Assign original shape number
    }
}

static void read_shape(struct shape1* shape) {
    unsigned int i, shape_point_val, shape_x_coord, shape_y_coord;

    shape->npoints = read_int_from_input();
    if (shape->npoints == UINT_MAX) { // Error reading or overflow
        // No points array was allocated yet, so no need to free shape->points.
        if (output_buffer) free(output_buffer); output_buffer = NULL; output_capacity = 0; output_length = 0; return;
    }
    // npoints must be non-zero and less than total board tokens to be a valid shape.
    if (shape->npoints == 0 || shape->npoints >= ntokens) {
        if (output_buffer) free(output_buffer); output_buffer = NULL; output_capacity = 0; output_length = 0; return;
    }

    // Allocate npoints + 1 for the sentinel (0 pointer) used later in prepare_shapes
    alloc_arr(shape->points, shape->npoints + 1);
    if (!shape->points) return; // xmalloc handles setting output_buffer to NULL on failure

    shape->x = 0; // Max relative X coordinate within the shape (used for width)
    shape->y = 0; // Max relative Y coordinate within the shape (used for height)
    for (i = 0; i < shape->npoints; ++i) {
        shape_point_val = read_int_from_input();
        if (shape_point_val == UINT_MAX) { // Error reading or overflow
            if (shape->points) free(shape->points); shape->points = NULL; // Clean up partially allocated points
            if (output_buffer) free(output_buffer); output_buffer = NULL; output_capacity = 0; output_length = 0; return;
        }
        if (shape_point_val >= ntokens) { // Shape point coordinate out of board bounds (0 to ntokens-1)
            if (shape->points) free(shape->points); shape->points = NULL;
            if (output_buffer) free(output_buffer); output_buffer = NULL; output_capacity = 0; output_length = 0; return;
        }
        shape->points[i] = shape_point_val;

        shape_x_coord = shape_point_val % x_dim;
        shape_y_coord = shape_point_val / x_dim;
        if (shape_x_coord > shape->x) {
            shape->x = shape_x_coord;
        }
        if (shape_y_coord > shape->y) {
            shape->y = shape_y_coord;
        }
    }

    // Calculate maximum top-left placement coordinates for this shape (board_dim - shape_max_relative_coord)
    shape->x = x_dim - shape->x;
    shape->y = y_dim - shape->y;
    shape->total = shape->x * shape->y; // Total possible distinct placements on the board
}

static void prepare_shapes(void) {
    struct shape* shape = NULL;
    int i, j, k;
    int max_points_in_shapes1_block; // Renamed for clarity: max 'npoints' in the shapes1 block
    unsigned int idx_to_swap_from_shapes1; // Index in shapes1 to swap from
    unsigned int toggles_accounted_for; // Renamed to avoid collision with global 'toggles' if any
    int** current_shape_cache_ptr; // Renamed to avoid collision with macro-defined 'cache'

    if (nshapes == 0) { // No shapes, nothing to prepare
        return;
    }

    alloc_arr(shapes, nshapes);
    if (!shapes) return; // xmalloc handles setting output_buffer to NULL on failure

    // Initialize all `shapes` elements' internal pointers and members to safe default states.
    // This is CRITICAL for robust cleanup if an allocation fails later,
    // ensuring `reset_solver_state` or manual cleanup loops only free valid pointers.
    for (unsigned int init_idx = 0; init_idx < nshapes; init_idx++) {
        shapes[init_idx].cache = NULL;
        shapes[init_idx].equal = NULL;
        shapes[init_idx].seq = 0;
        shapes[init_idx].incs = 0;
        // Don't initialize shapes[init_idx].a; it will be copied from shapes1.
    }

    toggles_accounted_for = 0; // Accumulator for initial incs calculation
    for (i = (int)nshapes - 1; i >= 0; i--) {
        // Add cancellation check at the start of each major shape preparation.
        /*
        if (cancel_requested) {
            // If cancellation occurs here, we must free any `cache` blocks already successfully allocated
            // for shapes at indices `k_cleanup` which are higher than current `i` (as they were processed earlier).
            for (unsigned int k_cleanup = i + 1; k_cleanup < nshapes; k_cleanup++) {
                if (shapes[k_cleanup].cache) {
                    free(shapes[k_cleanup].cache - 1); // Free its cache block (subtract 1 for base ptr)
                    shapes[k_cleanup].cache = NULL;
                }
            }
            free(shapes); // Free the main `shapes` array itself
            shapes = NULL;
            // output_buffer already cleared by `CancelSolve` or will be by main `SolveShapeshifter` error path.
            return; // Indicate cancellation and return
        }
        */

        shape = shapes + i; // Current shape being prepared (destination in `shapes` array)
        max_points_in_shapes1_block = 0;
        // Find the shape in the remaining `shapes1` array with the maximum number of points
        for (j = i; j >= 0; j--) {
            if (shapes1[j].npoints > max_points_in_shapes1_block) {
                idx_to_swap_from_shapes1 = (unsigned int)j;
                max_points_in_shapes1_block = shapes1[j].npoints;
            }
        }
        // Move the largest shape (from shapes1) to its sorted position in the `shapes` array
        shape->a = shapes1[idx_to_swap_from_shapes1]; // Copy the shape1 data to `shape->a`
        shapes1[idx_to_swap_from_shapes1] = shapes1[i]; // Perform the swap within shapes1 for next iteration
        toggles_accounted_for += shape->a.npoints; // Accumulate total points

        shape->equal = NULL; // Reset for this shape, will be set below if a match is found
        // Search for a logically equivalent shape among those already processed (higher indices in `shapes` array)
        for (j = i - 1; j >= 0; j--) {
            if (shapes[j].a.npoints != shape->a.npoints) continue; // Must have same number of points
            for (k = (int)shape->a.npoints - 1; k >= 0; k--) {
                // Access `points` array for comparison.
                // Ensures array access is within bounds of allocated points array.
                // shape->a.points comes from shapes1, which has `npoints + 1` allocated.
                // k goes from shape->a.npoints - 1 down to 0, which is correct for actual points.
                // shapes[j].a.points must also be valid.
                // THIS WAS THE CRITICAL LINE FOR AV.
                // The fix relies on `read_shapes` correctly initializing and cleaning `shapes1[i].points`.
                if (k < 0 || k >= (int)shape->a.npoints || k >= (int)shapes[j].a.npoints) { // Additional defensive bounds check
                    // Logic error or corruption, attempt graceful exit.
                    if (output_buffer) free(output_buffer); output_buffer = NULL; return;
                }
                if (shapes[j].a.points[k] != shape->a.points[k]) break; // Compare point by point
            }
            if (k < 0) { // If inner loop completed (all points matched)
                shapes[j].equal = shape; // Mark as equal to this `shape` (which is `shapes[i]`)
                break; // Found an equal shape, no need to check further
            }
        }
        shape->a.npoints++; // Increment npoints to account for the sentinel (0 pointer) when filling cache

        // Calculate total elements needed for this shape's cache block
        unsigned int cache_total_elements = shape->a.total * shape->a.npoints + 1;
        if (cache_total_elements == 0) { // Avoid malloc(0) and handle invalid total/npoints
            // Treat as an error, clean up previously allocated caches and shapes array.
            for (unsigned int k_cleanup = i; k_cleanup < nshapes; k_cleanup++) { // Includes current shape 'i' if its cache was invalid.
                if (shapes[k_cleanup].cache) {
                    free(shapes[k_cleanup].cache - 1);
                    shapes[k_cleanup].cache = NULL;
                }
            }
            free(shapes); shapes = NULL;
            if (output_buffer) free(output_buffer); output_buffer = NULL;
            return;
        }

        alloc_arr(current_shape_cache_ptr, cache_total_elements); // Allocate cache for current shape
        if (!current_shape_cache_ptr) {
            // CRITICAL ERROR HANDLING: If malloc fails, clean up ALL previously allocated `cache` blocks
            // for shapes at indices `k_cleanup` which are higher than current `i`, AND the main 'shapes' array.
            for (unsigned int k_cleanup = i + 1; k_cleanup < nshapes; k_cleanup++) {
                if (shapes[k_cleanup].cache) {
                    free(shapes[k_cleanup].cache - 1);
                    shapes[k_cleanup].cache = NULL;
                }
            }
            free(shapes); // Free the main `shapes` array itself
            shapes = NULL;
            // xmalloc already handled setting output_buffer to NULL
            return; // Indicate memory allocation failure and return
        }
        *current_shape_cache_ptr = 0; // First sentinel for the whole cache block
        current_shape_cache_ptr++; // Move past the initial sentinel
        shape->cache = current_shape_cache_ptr; // Store pointer to the start of actual placement data

        // Populate the cache with pointers to matrix locations for each possible placement.
        for (k = 0; k < (int)shape->a.total; ++k) { // Iterate through all possible top-left placements (total combinations)
            for (j = (int)shape->a.npoints - 2; j >= 0; --j) { // Iterate through original points of the shape
                // Calculate the absolute index in the 'matrix'.
                // Add robust bounds checks for safety.
                unsigned int matrix_index_candidate = (k / shape->a.x) * x_dim + (k % shape->a.x) + shape->a.points[j];
                if (!matrix || matrix_index_candidate >= ntokens || matrix_index_candidate < 0) {
                    // This indicates a severe logic error or corruption if matrix or calculated index is invalid.
                    // Clean up all allocated memory and exit gracefully.
                    for (unsigned int k_cleanup = i; k_cleanup < nshapes; k_cleanup++) { // Includes current shape 'i'
                        if (shapes[k_cleanup].cache) {
                            free(shapes[k_cleanup].cache - 1);
                            shapes[k_cleanup].cache = NULL;
                        }
                    }
                    free(shapes); shapes = NULL;
                    if (output_buffer) free(output_buffer); output_buffer = NULL;
                    return;
                }
                *current_shape_cache_ptr = &matrix[matrix_index_candidate];
                current_shape_cache_ptr++;
            }
            *current_shape_cache_ptr = 0; // Sentinel for the end of a single placement's points
            current_shape_cache_ptr++;
        }
    }
    // Calculate initial 'toggles' remaining (how many times we need to decrement to reach all 0s)
    for (i = 0; i < (int)ntokens; i++) toggles_accounted_for -= matrix[i]; // Subtract current matrix values
    alloc_arr(max_incs_left, nshapes); // Allocate max_incs_left array
    if (!max_incs_left) { // If this allocation fails
        // Clean up shapes and their caches already allocated if max_incs_left fails here.
        for (unsigned int k_cleanup = 0; k_cleanup < nshapes; k_cleanup++) {
            if (shapes[k_cleanup].cache) {
                free(shapes[k_cleanup].cache - 1);
                shapes[k_cleanup].cache = NULL;
            }
        }
        free(shapes); shapes = NULL;
        return; // xmalloc already handled setting output_buffer to NULL
    }
    // Set initial 'incs' for the last shape (most significant shape)
    shapes[nshapes - 1].incs = toggles_accounted_for / max_token;
}

static void find_sequence(void) {
    int i; // Current depth in the backtracking search (index of the shape being placed)
    struct shape* shape = NULL; // Pointer to the current shape being processed
    register int** cache; // Pointer to the current position within the shape's cache (for placements)
    unsigned int seq; // Current placement sequence (0 to shape->a.total - 1)
    int incs; // Number of "increments" (toggles) available/used

    // Initialize to the "deepest" level (the last shape in the sorted 'shapes' array)
    i = (int)nshapes - 1;
    if (i < 0) { // Should not happen if `initialise` succeeded, but defensive check
        if (output_buffer) free(output_buffer); output_buffer = NULL; output_capacity = 0; output_length = 0;
        return;
    }
    shape = shapes + i; // Point to the last shape
    incs = shape->incs; // Get initial 'incs' for this shape
    seq = shape->equal ? shape->equal->seq : 0; // Start sequence: if equal, use stored; else 0
    // Set cache pointer to the start of the first sequence's data in the cache
    if (!shape->cache || (unsigned int)seq * shape->a.npoints >= (shape->a.total * shape->a.npoints + 1)) {
        if (output_buffer) free(output_buffer); output_buffer = NULL; goto cancelled_no_free;
    }
    cache = shape->cache + (seq * shape->a.npoints);

    for (;;) { // Main loop: Drives the backtracking search
        /*
        if (cancel_requested) goto cancelled; // Check 1: Global cancellation flag
        */

        // --- Inner Loop: Try to place the current shape (`shape` at index `i`) ---
        for (;;) {
            /*
            if (cancel_requested) goto cancelled; // Check 2: Inner loop cancellation
            */

            // Defensive check for shape's npoints (critical for loops using it)
            if (shape->a.npoints == 0) { // Indicates a corrupted/malformed shape
                if (output_buffer) free(output_buffer); output_buffer = NULL; goto cancelled_no_free;
            }

            // Simulate the placement and check if valid (dry run)
            int** test_cache = cache; // Temporary cache pointer for dry run
            int test_incs = incs;     // Temporary incs count for dry run
            int ok = 1;               // Flag for successful dry run
            for (int j = 0; j < (int)shape->a.npoints - 1; j++) { // Iterate through points of the shape
                /*
                if (cancel_requested) goto cancelled; // Check 3: Granular check during dry run
                */
                if (*test_cache == 0) { // Sentinel found before all points checked
                    break;
                }
                // Ensure *test_cache points within the valid range of 'matrix'
                if (*test_cache < matrix || *test_cache >= matrix + ntokens) {
                    if (output_buffer) free(output_buffer); output_buffer = NULL; goto cancelled_no_free;
                }
                // If a token is 0 and we need to decrement it, we must have an `incs` available.
                // If incs goes negative, this placement is not `ok`.
                if ((**test_cache == 0) && (--test_incs < 0)) {
                    ok = 0; // Cannot make this placement
                    break;
                }
                test_cache++;
            }

            if (!ok) break; // If dry run failed, break to outer loop to try next sequence or backtrack

            // If dry run was successful, apply the shape's effect to the `matrix`
            // Reset cache pointer to re-apply from the beginning of the current placement
            // This is needed because `test_cache` moved during the dry run.
            if (!shape->cache || (unsigned int)seq * shape->a.npoints >= (shape->a.total * shape->a.npoints + 1)) {
                if (output_buffer) free(output_buffer); output_buffer = NULL; goto cancelled_no_free;
            }
            cache = shape->cache + (seq * shape->a.npoints); // Reset to current sequence start
            for (int j = 0; j < (int)shape->a.npoints - 1; j++) {
                /*
                if (cancel_requested) goto cancelled; // Check 4: Granular check during actual modification
                */
                if (*cache == 0) break; // Sentinel found
                // Ensure *cache points within the valid range of 'matrix'
                if (*cache < matrix || *cache >= matrix + ntokens) {
                    if (output_buffer) free(output_buffer); output_buffer = NULL; goto cancelled_no_free;
                }
                **cache = (**cache - 1 + max_token) % max_token; // Decrement token value (wrap around if needed)
                cache++;
            }

            incs = test_incs; // Update incs count after successful application
            shape->seq = seq; // Store the chosen placement sequence for this shape

            if (i == 0) return; // Base case: If we successfully placed the first shape (index 0), a solution is found!

            // Move to the next "recursive" level (process the previous shape in the sorted array)
            --i;
            shape = shapes + i; // Move to the previous shape
            seq = shape->equal ? shape->equal->seq : 0; // If current shape has an equivalent, start its search from that sequence; otherwise, start from 0
            // Set cache pointer for the next shape's initial sequence
            if (!shape->cache || (unsigned int)seq * shape->a.npoints >= (shape->a.total * shape->a.npoints + 1)) {
                if (output_buffer) free(output_buffer); output_buffer = NULL; goto cancelled_no_free;
            }
            cache = shape->cache + (seq * shape->a.npoints);
            shape->incs = incs; // Pass current incs count to the next recursive level
        }

        // --- Inner Loop: Backtrack or Try Next Sequence ---
        for (;;) {
            /*
            if (cancel_requested) goto cancelled; // Check 5: Inner loop cancellation
            */

            ++seq; // Try the next placement sequence for the current `shape`
            if (seq < shape->a.total) { // If there are more sequences to try for this shape
                // Update cache pointer for the new sequence
                if (!shape->cache || (unsigned int)seq * shape->a.npoints >= (shape->a.total * shape->a.npoints + 1)) {
                    if (output_buffer) free(output_buffer); output_buffer = NULL; goto cancelled_no_free;
                }
                cache = shape->cache + (seq * shape->a.npoints);
                incs = shape->incs; // Restore incs for this shape (saved when we moved forward)
                break; // Found a new sequence, go back to the "Add shapes loop" (outer `for (;;)` will restart)
            }

            // No more sequences for the current shape, need to backtrack further
            ++i; // Move back "up" one recursive level (to the shape we processed previously)
            if (i >= (int)nshapes) { // If we've exhausted all shapes and all their possibilities
                // All shapes exhausted, no solution found after trying all permutations
                if (output_buffer) free(output_buffer); output_buffer = NULL; output_capacity = 0; output_length = 0;
                return; // Exit find_sequence: no solution
            }

            shape = shapes + i; // Move back to the previous shape
            seq = shape->seq; // Get the sequence that was *last successfully used* for this shape

            // Calculate cache position to undo the changes made by this shape at its stored 'seq'
            // We need to point to the last element that was modified by this shape's placement.
            // shape->a.npoints includes the sentinel, so (npoints-1) is the number of actual points.
            // `seq * shape->a.npoints` gets to the start of the placement.
            // Adding `(shape->a.npoints - 2)` points to the last actual point in that sequence block.
            if (!shape->cache || (unsigned int)seq * shape->a.npoints + (shape->a.npoints - 2) >= (shape->a.total * shape->a.npoints + 1)) {
                if (output_buffer) free(output_buffer); output_buffer = NULL; goto cancelled_no_free;
            }
            cache = shape->cache + (seq * shape->a.npoints) + (shape->a.npoints - 2);

            incs = shape->incs; // Restore incs state from when this shape was last placed

            // Undo the shape's effect by incrementing tokens back
            for (int j = 0; j < (int)shape->a.npoints - 1; j++) {
                /*
                if (cancel_requested) goto cancelled; // Check 6: Granular check during undo loop
                */
                if (*cache == 0) { // Sentinel found prematurely (indicates logic error if cache pointer is off)
                    if (output_buffer) free(output_buffer); output_buffer = NULL; goto cancelled_no_free;
                }
                // Ensure *cache points within the valid range of 'matrix'
                if (*cache < matrix || *cache >= matrix + ntokens) {
                    if (output_buffer) free(output_buffer); output_buffer = NULL; goto cancelled_no_free;
                }
                **cache = (**cache + 1) % max_token; // Increment token value (undo decrease, wrap around)
                cache--; // Move to previous point (working backwards)
            }
            // After undoing, `cache` now points before the first point of the undone placement.
            // The logic will then automatically pick up by trying the `++seq` on the next iteration of the outer loop.
        }
    }

    /*
    cancelled:
        // This label is hit when `cancel_requested` is true.
        // It signals cancellation to the C# side by clearing the `output_buffer`.
        if (output_buffer) {
            free(output_buffer);
            output_buffer = NULL;
            output_capacity = 0;
            output_length = 0;
        }
    */
    cancelled_no_free: // For cases where `output_buffer` was already `NULL` due to an earlier error, just exit gracefully
        return;
   
}
static void print_result_matrix(int from, int to) {
    int i, j, k;
    int* result_matrix;
    int* shape_graph;
    int v; /* inverted */

    if (!result_matrices || !shape_graphs || !matrix) { // Basic sanity check for global arrays
        if (output_buffer) free(output_buffer); output_buffer = NULL; return;
    }

    for (i = 0;i < (int)ntokens;i += x_dim) { // Iterate row by row (using x_dim as width)
        v = 0; // Reset 'inverted' flag for each new row of output
        for (j = from;j < to;j++) { // Iterate through the columns of matrices to print
            // Ensure result_matrices[j] and shape_graphs[j] are valid before access
            if (j < 0 || j > max_print_cols || !result_matrices[j] || !shape_graphs[j]) {
                if (output_buffer) free(output_buffer); output_buffer = NULL; return;
            }
            result_matrix = result_matrices[j];
            shape_graph = shape_graphs[j];
            for (k = 0;k < (int)x_dim;k++) { // Iterate through cells in current row/column
                // Ensure matrix index (i+k) is valid
                if ((i + k) < 0 || (i + k) >= ntokens) {
                    if (output_buffer) free(output_buffer); output_buffer = NULL; return;
                }
                if (shape_graph[i + k] != v) { // Apply inversion based on shape graph
                    v ^= 1;
                }
                append_to_output("%d%c", result_matrix[i + k], v ? '+' : '|');
                if (!output_buffer) return; // Check append_to_output for memory issues/errors
            }
            if (v) { // If 'inverted' flag is still active at end of row, reset it
                v = 0;
            }
            append_to_output("%s", "  "); // Add spacing between matrix columns in output
            if (!output_buffer) return;
        }
        append_to_output("\n"); // Newline after each row of matrices
        if (!output_buffer) return;
    }
}

static void copy_shape_to_result(int* shape_graph, struct shape* shape, int** cache_override) {
    int** current_cache;
    if (cache_override == 0) { // If no override, use the shape's stored sequence and cache
        // Ensure shape's cache and sequence-derived pointer are valid
        if (!shape->cache || (unsigned int)shape->seq * shape->a.npoints >= (shape->a.total * shape->a.npoints + 1)) {
            if (output_buffer) free(output_buffer); output_buffer = NULL; return;
        }
        current_cache = shape->cache + (shape->seq * shape->a.npoints);
    }
    else { // Use the provided cache override pointer
        current_cache = cache_override;
    }

    // Ensure shape_graph is valid before memset
    if (!shape_graph) {
        if (output_buffer) free(output_buffer); output_buffer = NULL; return;
    }

    memset(shape_graph, 0, sizeof(int) * ntokens); // Clear the graph matrix to all zeros

    // Iterate through points to mark shape on graph (set to 1)
    for (int i = 0; i < (int)shape->a.npoints - 1; i++) { // Loop until the sentinel (npoints-1 actual points)
        if (*current_cache == 0) break; // Sentinel encountered prematurely
        // Ensure '*current_cache - matrix' results in a valid index into the matrix.
        int matrix_idx = (int)(*current_cache - matrix);
        if (matrix_idx < 0 || matrix_idx >= (int)ntokens) {
            // Corrupted pointer in cache, indicates a serious error during prepare_shapes
            if (output_buffer) free(output_buffer); output_buffer = NULL; return;
        }
        shape_graph[matrix_idx] = 1; // Mark the cell as part of the shape
        current_cache++;
    }
}

void print_result(int print_matrix_flag) {
    int i, j;
    int m; // Index for current shape (original `nr` property)
    int n; // Placement sequence for current shape
    int col; // Column index for printing multiple matrices side-by-side
    struct shape* shape = NULL;
    int** cache;
    int* result_matrix;

    // Initial sanity checks for global state before any operations
    if (!result_matrices || !result_matrices[0] || !start_matrix || !matrix || ntokens == 0 || nshapes == 0) {
        if (output_buffer) free(output_buffer); output_buffer = NULL; output_capacity = 0; output_length = 0;
        return;
    }

    result_matrix = result_matrices[0];
    if (print_matrix_flag) {
        memcpy(result_matrix, start_matrix, sizeof(int) * ntokens); // Copy initial matrix for base
    }
    else {
        memset(result_matrix, 0, sizeof(int) * ntokens); // Or clear it
    }

    // Loop through each shape (m represents the original shape ID)
    for (m = 0; m < (int)nshapes; ) {
        // Process up to max_print_cols shapes per row of output
        for (col = 0; col < max_print_cols && m < (int)nshapes; col++, m++) {
            // Find the 'shape' struct in the 'shapes' array that corresponds to original ID 'm'
            shape = NULL; // Reset shape pointer for each iteration
            for (i = (int)nshapes - 1; i >= 0; i--) { // Iterate backwards (as shapes array is reverse sorted)
                if (shapes[i].a.nr == (unsigned int)m) {
                    shape = shapes + i; // Found the shape
                    break;
                }
            }
            if (shape == NULL) {
                // This indicates an internal logic error (shape with original ID 'm' not found).
                if (output_buffer) free(output_buffer); output_buffer = NULL; output_capacity = 0; output_length = 0;
                return;
            }

            n = shape->seq; // Get the chosen placement sequence for this shape
            append_to_output("Column: %d, Row: %d", n % shape->a.x, n / shape->a.x);
            if (!output_buffer) return; // Check for append_to_output failure

            // Padding after sequence description to align columns
            // x_dim * 2 (for tokens and |/+) + 2 (for "  ") - length of text output - some constant
            int padding_len = (int)x_dim * 2 + 2 - (int)strlen("Column: %d, Row: %d");
            if (padding_len < 0) padding_len = 0; // Avoid negative padding
            for (j = 0; j < padding_len; j++) {
                append_to_output(" ");
            }
            if (!output_buffer) return;

            // Ensure shape_graphs[col] is valid before calling copy_shape_to_result
            if (col < 0 || col > max_print_cols || !shape_graphs || !shape_graphs[col]) {
                if (output_buffer) free(output_buffer); output_buffer = NULL; return;
            }
            copy_shape_to_result(shape_graphs[col], shape, 0); // Copy shape's pattern to its graph matrix
            if (!output_buffer) return; // Check copy_shape_to_result for failure

            // Prepare result_matrix for the next column (which is a copy of the current one before applying changes)
            if (col + 1 > max_print_cols || !result_matrices || !result_matrices[col + 1] || !result_matrices[col]) {
                if (output_buffer) free(output_buffer); output_buffer = NULL; return;
            }
            result_matrix = result_matrices[col + 1];
            memcpy(result_matrix, result_matrices[col], sizeof(int) * ntokens);

            if (print_matrix_flag) {
                // Apply the shape's effect (undo toggling) to the result matrix
                // Ensure shape->cache is valid and its access for sequence `shape->seq` is within bounds
                if (!shape->cache || (unsigned int)shape->seq * shape->a.npoints >= (shape->a.total * shape->a.npoints + 1)) {
                    if (output_buffer) free(output_buffer); output_buffer = NULL; return;
                }
                cache = shape->cache + (unsigned int)shape->seq * shape->a.npoints;
                for (int k_point = 0; k_point < (int)shape->a.npoints - 1; k_point++) { // Loop until sentinel (npoints-1 actual points)
                    if (*cache == 0) break; // Sentinel encountered prematurely
                    n = (int)(*cache - matrix); // Get relative index in matrix
                    // Ensure 'n' is a valid index for result_matrix
                    if (n < 0 || n >= (int)ntokens) {
                        if (output_buffer) free(output_buffer); output_buffer = NULL; return;
                    }
                    result_matrix[n] = ((result_matrix[n] + max_token - 1) % max_token); // Apply reverse toggle (decrement with wrap-around)
                    cache++;
                }
            }
        }
        append_to_output("\n"); // Newline after each row of shape descriptions and their applied matrices
        if (!output_buffer) return;

        print_result_matrix(0, col); // Print the row of result matrices (0 to 'col' which is count of matrices printed)

        // Copy the last column's result matrix back to the first for the next iteration's base.
        if (!result_matrices[0] || !result_matrices[max_print_cols]) {
            if (output_buffer) free(output_buffer); output_buffer = NULL; return;
        }
        memcpy(result_matrices[0], result_matrices[max_print_cols], sizeof(int) * ntokens);
        append_to_output("\n");
        if (!output_buffer) return;
    }
}

// --- DLL Exported Functions ---

// Cancel entry point
EXPORT void CancelSolve(void) {
    cancel_requested = 1; // Set the volatile flag to signal cancellation
#ifdef _WIN32
    // Optional: Add a memory barrier. For simple flag, volatile is usually sufficient,
    // but explicit barrier ensures writes are visible immediately across cores.
    // _ReadWriteBarrier(); // MSVC specific
#endif
// For GCC: __sync_synchronize();
}

// Main entry point for the DLL. Solves the shapeshifter puzzle.
// input: A null-terminated string containing the puzzle definition.
// Returns: A null-terminated string with the solution, or NULL on error/cancellation.
EXPORT char* SolveShapeshifter(const char* input) {
    reset_solver_state(); // Clean up global static state from previous run
    cancel_requested = 0;  // Explicitly RESET CANCEL FLAG for a new run's start

    input_buffer_ptr = input; // Set global pointer to the C# input string
    input_offset = 0;         // Reset input parsing offset

    // Initialize output buffer for appending results
    output_length = 0;
    output_capacity = 1024; // Initial capacity (can grow with realloc in append_to_output)
    output_buffer = (char*)malloc(output_capacity);
    if (!output_buffer) return NULL; // Failed to allocate initial output buffer
    output_buffer[0] = '\0'; // Ensure it's null-terminated immediately

    // Step 1: Initialize all puzzle data by parsing input and preparing internal structures
    initialise();
    // If output_buffer is NULL, an error occurred during initialization (e.g., malloc failed, bad input, or internal error path cleared it).
    if (!output_buffer) {
        return NULL;
    }

    // Step 2: Find the solution sequence (main computational part)
    find_sequence();
    // If output_buffer is NULL here, it means find_sequence either:
    //  a) Encountered an internal error (e.g., out of bounds access, logic issue that cleared output_buffer)
    //  b) Was cancelled (by setting cancel_requested and checking it)
    //  c) Found no solution (and correctly signaled this by clearing output_buffer)
    // In all these cases, we want to return NULL to C# to signal no valid result.
    if (!output_buffer) {
        return NULL;
    }

    // Step 3: Print the solution result to the output_buffer
    print_result(1); // Pass 1 to print the matrix changes (detailed output)
    // If output_buffer is NULL after print_result, it means print_result also had an issue or was cancelled.
    if (!output_buffer) {
        return NULL;
    }

    // Finalize: Attempt to reallocate output_buffer to its exact used size for efficiency.
    // The C# wrapper is responsible for calling FreeShapeshifterResult to free this memory later.
    char* final_result = (char*)realloc(output_buffer, output_length + 1);
    if (!final_result) {
        // If realloc fails, output_buffer is still valid at its original location.
        // Return this original buffer. It will still be freed by C# via FreeShapeshifterResult.
        return output_buffer;
    }
    output_buffer = NULL; // `final_result` now owns the memory (transfer ownership)
    return final_result;
}

// Frees the memory allocated by SolveShapeshifter for the result string.
// This function MUST be called from C# to prevent memory leaks.
EXPORT void FreeShapeshifterResult(char* result) {
    if (result) {
        free(result);
    }
}