using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Microsoft.VisualBasic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Process = System.Diagnostics.Process;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio;

namespace MakefileBuild
{
    internal sealed class MakeCommand
    {
        private static IVsOutputWindow _outputWindow;
        public const int CommandId = 0x0100;
        public const int MyGenerateFileCommandId = 0x0200;
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

            var generateCmdID = new CommandID(CommandSet, MyGenerateFileCommandId);
            var generateMenuItem = new OleMenuCommand(GenerateFile, generateCmdID)
            {
                Supported = true,
                Visible = true,
            };
            commandService.AddCommand(generateMenuItem);
        }

        public static MakeCommand Instance { get; private set; }
        private IServiceProvider ServiceProvider => _package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new MakeCommand(package, commandService);
            _outputWindow = await package.GetServiceAsync(typeof(SVsOutputWindow)) as IVsOutputWindow;
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                var dte = (DTE2)ServiceProvider.GetService(typeof(DTE));
                if (dte == null)
                {
                    WriteToOutputWindow("Failed to get DTE service.");
                    return;
                }

                var activeDocument = dte.ActiveDocument;
                if (activeDocument == null)
                {
                    WriteToOutputWindow("No active document found.");
                    return;
                }

                string makefilePath = activeDocument.FullName;
                var workingDirectory = Path.GetDirectoryName(makefilePath);

                string makePath = Environment.GetEnvironmentVariable("PATH")?.Split(';')
                    .SelectMany(path => Directory.GetFiles(path, "make.exe", SearchOption.TopDirectoryOnly))
                    .FirstOrDefault();

                if (string.IsNullOrEmpty(makePath))
                {
                    WriteToOutputWindow("make.exe not found in PATH.");
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
                            outputBuilder.AppendLine(outputEvent.Data);
                        }
                    };

                    process.ErrorDataReceived += (errorSender, errorEvent) =>
                    {
                        if (!string.IsNullOrEmpty(errorEvent.Data))
                        {
                            errorBuilder.AppendLine(errorEvent.Data);
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();

                    WriteToOutputWindow($"Make command exited with code: {process.ExitCode}");

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
                WriteToOutputWindow($"Error executing make command: {ex.Message}");
            }
        }

        private static void LogToDesktop(string message)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string logFilePath = Path.Combine(desktopPath, "MakefileBuildLog.txt");

            try
            {
                File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error writing to log file: {ex.Message}");
                WriteToOutputWindow($"Error writing to log file: {ex.Message}");
            }
        }
        private static string GetRelativePath(string basePath, string fullPath)
        {
            Uri baseUri = new Uri(basePath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
            Uri fullUri = new Uri(fullPath);

            if (baseUri.Scheme != fullUri.Scheme)
            {
                return fullPath; // path is not relative to base path
            }

            Uri relativeUri = baseUri.MakeRelativeUri(fullUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }

        private void GenerateFile(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                Debug.WriteLine("GenerateFileForSingleMakefile command called.");
                var dte = (DTE2)ServiceProvider.GetService(typeof(DTE));
                if (dte?.ActiveDocument == null)
                {
                    ShowMessage("No active document found.");
                    return;
                }
                string activeDocumentPath = dte.ActiveDocument.FullName;
                string projectDirectory = Path.GetDirectoryName(activeDocumentPath);

                var makefilePath = Directory.GetFiles(projectDirectory, "Makefile", SearchOption.AllDirectories)
                                            .FirstOrDefault();

                if (makefilePath == null)
                {
                    ShowMessage("No Makefile found.");
                    return;
                }

                string makefileDirectory = Path.GetDirectoryName(makefilePath);

                // Prompt user for the project file name
                string projectName = Microsoft.VisualBasic.Interaction.InputBox($"Enter a name for the project file generated from {Path.GetFileName(makefilePath)}:", "Project File Name", "GeneratedProject");
                if (string.IsNullOrEmpty(projectName))
                {
                    return; // Skip if no name provided
                }

                string projectFilePath = Path.Combine(makefileDirectory, $"{projectName}.vcxproj");
                string filtersFilePath = Path.Combine(makefileDirectory, $"{projectName}.vcxproj.filters");

                var sourceFiles = Directory.GetFiles(makefileDirectory, "*.*", SearchOption.AllDirectories)
                                           .Where(f => f.EndsWith(".c") || f.EndsWith(".cpp") || f.EndsWith(".h") || f.EndsWith("Makefile"))
                                           .Select(f => (fullPath: f, relativePath: GetRelativePath(makefileDirectory, f)));

                var projectContentGenerator = new ProjectContentGenerator();
                string projectContent = projectContentGenerator.GenerateProjectContent(sourceFiles);
                string filtersContent = projectContentGenerator.GenerateFiltersContent(sourceFiles);

                File.WriteAllText(projectFilePath, projectContent);
                File.WriteAllText(filtersFilePath, filtersContent);

                ShowMessage($"Generated project file: {projectFilePath}");
                ShowMessage($"Generated filters file: {filtersFilePath}");
                Debug.WriteLine($"Generated project file: {projectFilePath}");
                Debug.WriteLine($"Generated filters file: {filtersFilePath}");

                // Prompt user to restart Visual Studio for the project
                var userPrompt = VsShellUtilities.ShowMessageBox(
                    this._package,
                    $"Do you want to restart Visual Studio to open the generated project file at {makefileDirectory}?",
                    "Restart Visual Studio",
                    OLEMSGICON.OLEMSGICON_QUERY,
                    OLEMSGBUTTON.OLEMSGBUTTON_YESNO,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

                if (userPrompt == (int)DialogResult.Yes)
                {
                    dte.ExecuteCommand("File.Exit");
                    System.Diagnostics.Process.Start("devenv.exe", projectFilePath);
                    ShowMessage($"Restarting Visual Studio and opening project file: {projectFilePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error generating project file: {ex.Message}");
                ShowMessage($"Error generating project file: {ex.Message}");
            }
        }

        private string PromptForProjectName(string makefilePath)
        {
            string message = $"Enter a name for the project file generated from {Path.GetFileName(makefilePath)}:";
            string title = "Project File Name";
            return Microsoft.VisualBasic.Interaction.InputBox(message, title, "GeneratedProject");
        }

        private void ShowMessage(string message)
        {
            VsShellUtilities.ShowMessageBox(
                this._package,
                message,
                "MakefileBuild",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
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

                // Log the message for debugging
                Debug.WriteLine(message);

                pane.OutputString(message + "\n");
                pane.Activate(); // Bring the Output pane to focus
            }
        }


    }
}