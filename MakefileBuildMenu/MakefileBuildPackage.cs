using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace MakefileBuild
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("Make Command", "Build your Makefile in Visual Studio", "1.0.0-beta.1")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideUIContextRule("24551deb-f034-43e9-a279-0e541241687e", // contextGuid must be a valid string-based GUID
        name: "UI Context for supported files",
        expression: "Makefile | Makfile",
        termNames: new[] { "Makefile", "Makfile" },
        termValues: new[] { "ActiveDocumentName:Makefile", "ActiveDocumentName:*.mak" })]
    [Guid("fa24d542-0b4d-4f6b-ac03-24ff47c11b76")]
    public sealed class MyPackage : AsyncPackage
    {
        // This method is run automatically the first time the command is being executed
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // Switch to the UI thread
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            // Initialize your command
            await MakeCommand.InitializeAsync(this);
        }
    }
}
