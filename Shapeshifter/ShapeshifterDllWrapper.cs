using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ShapeshifterDllWrapper
{
    public static class ShapeshifterDllWrapper
    {
        // Delegates for the unmanaged functions
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate IntPtr SolveShapeshifterDelegate(string input);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void FreeShapeshifterResultDelegate(IntPtr resultPtr);

        // Name of the DLL
        private const string DllName = "ShapeshifterKvho.dll";

        // P/Invoke declarations for kernel32.dll functions
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr hModule);

        /// <summary>
        /// Solves the shapeshifter problem using the native DLL.
        /// Loads the DLL, calls the solve function, frees the result, and unloads the DLL.
        /// </summary>
        /// <param name="input">The input string for the solver.</param>
        /// <returns>The solved string from the DLL.</returns>
        /// <exception cref="ShapeshifterSolverException">Thrown if there's an error loading the DLL,
        /// finding functions, or if the solver returns no solution/encounters an internal error.</exception>
        public static string Solve(string input)
        {
            IntPtr dllHandle = IntPtr.Zero; // Handle to the loaded DLL
            IntPtr resultPtr = IntPtr.Zero; // Pointer to the result returned by the native function
            string result = null;

            try
            {
                // 1. Construct the full path to the DLL
                string dllFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DllName);

                // 2. Load the DLL
                dllHandle = LoadLibrary(dllFullPath);
                if (dllHandle == IntPtr.Zero)
                {
                    int lastWin32Error = Marshal.GetLastWin32Error();
                    throw new ShapeshifterSolverException(string.Format("Failed to load DLL '{0}'. Win32 Error: {1}", DllName, lastWin32Error));
                }

                // 3. Get function pointer for SolveShapeshifter
                IntPtr solveProcAddress = GetProcAddress(dllHandle, "SolveShapeshifter");
                if (solveProcAddress == IntPtr.Zero)
                {
                    int lastWin32Error2 = Marshal.GetLastWin32Error();
                    throw new ShapeshifterSolverException(string.Format("Failed to find function 'SolveShapeshifter' in '{0}'. Win32 Error: {1}", DllName, lastWin32Error2));
                }

                // 4. Get function pointer for FreeShapeshifterResult
                IntPtr freeProcAddress = GetProcAddress(dllHandle, "FreeShapeshifterResult");
                if (freeProcAddress == IntPtr.Zero)
                {
                    int lastWin32Error3 = Marshal.GetLastWin32Error();
                    throw new ShapeshifterSolverException(string.Format("Failed to find function 'FreeShapeshifterResult' in '{0}'. Win32 Error: {1}", DllName, lastWin32Error3));
                }

                // 5. Marshal function pointers to delegates
                SolveShapeshifterDelegate solveDelegate = Marshal.GetDelegateForFunctionPointer<SolveShapeshifterDelegate>(solveProcAddress);
                FreeShapeshifterResultDelegate freeDelegate = Marshal.GetDelegateForFunctionPointer<FreeShapeshifterResultDelegate>(freeProcAddress);

                // 6. Call the native solver function
                // This is where the crash occurs: zero = delegateForFunctionPointer(input);
                resultPtr = solveDelegate(input);

                // 7. Check if the native solver returned a valid pointer
                if (resultPtr == IntPtr.Zero)
                {
                    throw new ShapeshifterSolverException("Shapeshifter solver returned no solution or encountered an internal error.");
                }

                // 8. Marshal the result pointer to a .NET string
                result = Marshal.PtrToStringAnsi(resultPtr);
            }
            catch (Exception ex)
            {
                // Re-throw if it's already a ShapeshifterSolverException, otherwise wrap it.
                if (ex is ShapeshifterSolverException)
                {
                    throw;
                }
                throw new ShapeshifterSolverException("An error occurred during native solver operation: " + ex.Message, ex);
            }
            finally
            {
                // 9. IMPORTANT: Free the memory allocated by the native DLL for the result string
                // This must happen BEFORE FreeLibrary, as FreeShapeshifterResult is part of the DLL
                if (resultPtr != IntPtr.Zero)
                {
                    // Ensure the delegate is available, otherwise this could crash if FreeProcAddress failed
                    // (though the outer try/catch should handle it before reaching here)
                    IntPtr freeProcAddress = IntPtr.Zero; // Need to re-get it if not in scope or stored
                    if (dllHandle != IntPtr.Zero) // Ensure DLL handle is valid before trying to get proc address
                    {
                        freeProcAddress = GetProcAddress(dllHandle, "FreeShapeshifterResult");
                    }

                    if (freeProcAddress != IntPtr.Zero)
                    {
                        FreeShapeshifterResultDelegate freeDelegate = Marshal.GetDelegateForFunctionPointer<FreeShapeshifterResultDelegate>(freeProcAddress);
                        freeDelegate(resultPtr);
                        resultPtr = IntPtr.Zero; // Clear the pointer after freeing
                    }
                    else
                    {
                        // This scenario should be caught earlier, but as a safeguard:
                        Console.WriteLine("Warning: FreeShapeshifterResult delegate was not available to free native memory.");
                    }
                }

                // 10. Unload the DLL
                if (dllHandle != IntPtr.Zero)
                {
                    if (!FreeLibrary(dllHandle))
                    {
                        int lastWin32Error = Marshal.GetLastWin32Error();
                        // This is a critical error, likely a memory leak or resource leak if FreeLibrary fails
                        Console.Error.WriteLine(string.Format("Warning: Failed to free DLL '{0}'. Win32 Error: {1}", DllName, lastWin32Error));
                        // Depending on criticality, you might throw an exception here as well.
                    }
                    dllHandle = IntPtr.Zero; // Clear the handle
                }
            }
            return result;
        }
    }

    // Custom exception class (assuming you have one, or you can define it)
    public class ShapeshifterSolverException : Exception
    {
        public ShapeshifterSolverException() { }
        public ShapeshifterSolverException(string message) : base(message) { }
        public ShapeshifterSolverException(string message, Exception innerException) : base(message, innerException) { }
    }
}