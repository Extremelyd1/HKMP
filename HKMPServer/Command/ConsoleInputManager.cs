using System;
using System.Threading;
using System.Threading.Tasks;

namespace HkmpServer.Command {
    /// <summary>
    /// Input manager for console command-line input.
    /// </summary>
    internal class ConsoleInputManager {
        /// <summary>
        /// Event that is called when input is given by the user.
        /// </summary>
        public event Action<string> ConsoleInputEvent;

        /// <summary>
        /// Object for locking asynchronous access.
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// The currently inputted text in the console.
        /// </summary>
        private string _currentInput;

        /// <summary>
        /// The cancellation token source for the task of reading input.
        /// </summary>
        private CancellationTokenSource _readingTaskTokenSource;

        /// <inheritdoc cref="_currentInput" />
        private string CurrentInput {
            get {
                lock (_lock) {
                    return _currentInput;
                }
            }
            set {
                lock (_lock) {
                    _currentInput = value;
                }
            }
        }

        /// <summary>
        /// Construct the console input manager by initializing values.
        /// </summary>
        public ConsoleInputManager() {
            CurrentInput = "";
        }

        /// <summary>
        /// Starts the console input manager.
        /// </summary>
        public void Start() {
            // Start a thread with cancellation token to read user input
            _readingTaskTokenSource = new CancellationTokenSource();
            new Thread(() => StartReading(_readingTaskTokenSource.Token)).Start();
        }

        /// <summary>
        /// Stops the console input manager.
        /// </summary>
        public void Stop() {
            _readingTaskTokenSource.Cancel();
        }

        /// <summary>
        /// Starts the read loop for command-line input.
        /// </summary>
        /// <param name="token">The cancellation token for checking whether this task is requested to cancel.</param>
        private void StartReading(CancellationToken token) {
            while (!token.IsCancellationRequested) {
                // This call will block until the user provides a key input
                var consoleKeyInfo = Console.ReadKey();

                if (consoleKeyInfo.Key == ConsoleKey.Escape) {
                    CurrentInput = "";
                    continue;
                }

                if (consoleKeyInfo.Key == ConsoleKey.Backspace) {
                    if (CurrentInput.Length > 0) {
                        for (var i = 0; i < CurrentInput.Length; i++) {
                            Console.Write(" ");
                        }

                        CurrentInput = CurrentInput.Substring(0, CurrentInput.Length - 1);
                    }

                    ResetCursor();
                    Console.Write(CurrentInput);

                    continue;
                }

                if (consoleKeyInfo.Key == ConsoleKey.Enter) {
                    Clear();

                    var input = CurrentInput;
                    CurrentInput = "";

                    ConsoleInputEvent?.Invoke(input);

                    continue;
                }

                CurrentInput += consoleKeyInfo.KeyChar;

                ResetCursor();
                Console.Write(CurrentInput);
            }
        }

        /// <summary>
        /// Writes a line to the console and restores the current input.
        /// </summary>
        /// <param name="line">The line to write.</param>
        public void WriteLine(string line) {
            if (CurrentInput != "") {
                Clear();
            }

            Console.WriteLine(line);

            Console.Write(CurrentInput);
        }

        /// <summary>
        /// Resets the cursor to the left position of the current line.
        /// </summary>
        private static void ResetCursor() {
            // Clamp the value of CursorTop to its possible values
            var cursorTop = Console.CursorTop;
            if (cursorTop < 0) {
                cursorTop = 0;
            }

            if (cursorTop >= short.MaxValue) {
                cursorTop = short.MaxValue - 1;
            }

            if (cursorTop >= Console.BufferHeight) {
                cursorTop = Console.BufferHeight - 1;
            }

            // Call SetCursorPosition directly instead of the CursorLeft property
            Console.SetCursorPosition(0, cursorTop);
        }

        /// <summary>
        /// Clears the current input.
        /// </summary>
        private static void Clear() {
            var length = Console.CursorLeft;
            ResetCursor();

            for (var i = 0; i < length; i++) {
                Console.Write(" ");
            }

            ResetCursor();
        }
    }
}
