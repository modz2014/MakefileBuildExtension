using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace MakefileBuild
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("Make Command", "Build your Makefile in Visual Studio", "1.0.0-beta.1")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid("fa24d542-0b4d-4f6b-ac03-24ff47c11b76")] // must match GUID in the .vsct file
    public sealed class MakePackage : AsyncPackage
    {
        // This method is run automatically the first time the command is being executed
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await MakeCommand.InitializeAsync(this);
        }
    }
}
