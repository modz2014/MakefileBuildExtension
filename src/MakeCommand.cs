using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Process = System.Diagnostics.Process;
using Task = System.Threading.Tasks.Task;

namespace MakefileBuild
{

    internal sealed class MakeCommand
    {
        private static IVsOutputWindow _outputWindow;
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("2b40859b-27f8-4dc6-85b1-f253386aa5f6");

        private readonly AsyncPackage _package;

        private MakeCommand(AsyncPackage package, IMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var cmdID = new CommandID(CommandSet, CommandId);
            var command = new OleMenuCommand(Execute, cmdID)
            {
                Supported = true,
                Visible = true,
            };

            commandService.AddCommand(command);
        }

        public static MakeCommand Instance { get; private set; }

        private IServiceProvider ServiceProvider
        {
            get { return _package; }
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;

            // Initialize the command
            Instance = new MakeCommand(package, commandService);
            // Get the Output window service
            _outputWindow = await package.GetServiceAsync(typeof(SVsOutputWindow)) as IVsOutputWindow;
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                Debug.WriteLine("Attempting to get DTE service...");
                var dte = (DTE2)ServiceProvider.GetService(typeof(DTE));

                if (dte == null)
                {
                    WriteToOutputWindow("Failed to get DTE service.");
                    Debug.WriteLine("Failed to get DTE service.");
                    return;
                }

                var activeDocument = dte.ActiveDocument;
                if (activeDocument == null)
                {
                    WriteToOutputWindow("No active document found.");
                    Debug.WriteLine("No active document found.");
                    return;
                }

                string makefilePath = activeDocument.FullName;
                Debug.WriteLine($"Active document path: {makefilePath}");

                var workingDirectory = Path.GetDirectoryName(makefilePath);
                Debug.WriteLine($"Working directory: {workingDirectory}");

                string makePath = Environment.GetEnvironmentVariable("PATH")?.Split(';')
                    .SelectMany(path => Directory.GetFiles(path, "make.exe", SearchOption.TopDirectoryOnly))
                    .FirstOrDefault();

                if (string.IsNullOrEmpty(makePath))
                {
                    WriteToOutputWindow("nmake.exe not found in PATH.");
                    Debug.WriteLine("nmake.exe not found in PATH.");
                    return;
                }

                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();
                using (Process process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = makePath,
                        Arguments = $"-f \"{makefilePath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = workingDirectory
                    };

                    process.OutputDataReceived += (outputSender, outputEvent) =>
                    {
                        if (!string.IsNullOrEmpty(outputEvent.Data))
                        {
                            Debug.WriteLine($"Output: {outputEvent.Data}");
                            outputBuilder.AppendLine(outputEvent.Data);
                        }
                    };

                    process.ErrorDataReceived += (errorSender, errorEvent) =>
                    {
                        if (!string.IsNullOrEmpty(errorEvent.Data))
                        {
                            Debug.WriteLine($"Error: {errorEvent.Data}");
                            errorBuilder.AppendLine(errorEvent.Data);
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();

                    WriteToOutputWindow($"Make command exited with code: {process.ExitCode}");
                    Debug.WriteLine($"Make command exited with code: {process.ExitCode}");

                    if (process.ExitCode != 0)
                    {
                        if (errorBuilder.Length > 0)
                        {
                            WriteToOutputWindow("Errors:\n" + errorBuilder.ToString());
                        }
                        else
                        {
                            WriteToOutputWindow("Errors: No additional error details available.");
                        }
                    }
                    else
                    {
                        WriteToOutputWindow("Makefile build completed successfully.");
                        WriteToOutputWindow("Output:\n" + outputBuilder.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error executing make command: {ex.Message}");
                WriteToOutputWindow($"An error occurred: {ex.Message}");
            }
        }

        private static void WriteToOutputWindow(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_outputWindow != null)
            {
                // Find or create the output pane
                Guid outputPaneGuid = Guid.NewGuid();
                _outputWindow.CreatePane(ref outputPaneGuid, "Makefile Builder Output", 1, 1);
                _outputWindow.GetPane(ref outputPaneGuid, out IVsOutputWindowPane pane);
                pane.OutputString(message + "\n");
                pane.Activate();
            }
        }



    }
}