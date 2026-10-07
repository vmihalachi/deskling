using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel;

namespace Deskling.Windows.Shell;

/// <summary>
/// "Open at login", through the package manifest's <c>StartupTask</c> extension. Needs package identity. The user
/// (Task Manager, Settings › Apps › Startup) or a policy can block the task, and then only they can turn it back on.
/// </summary>
public sealed class StartupService
{
    private readonly string taskId;

    /// <param name="taskId">The <c>TaskId</c> of the <c>uap5:StartupTask</c> in <c>Package.appxmanifest</c>.</param>
    public StartupService(string taskId)
    {
        this.taskId = taskId;
    }

    public async Task<bool> IsEnabledAsync()
    {
        var task = await StartupTask.GetAsync(taskId);
        return task.State is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
    }

    /// <summary>Returns whether it ended up enabled, which Windows may refuse without any prompt of its own.</summary>
    public async Task<bool> SetEnabledAsync(bool enabled)
    {
        var task = await StartupTask.GetAsync(taskId);
        if (!enabled)
        {
            if (task.State == StartupTaskState.Enabled)
                task.Disable();
            return false;
        }
        var state = await task.RequestEnableAsync();
        return state is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
    }

    /// <summary>Whether this launch came from the startup task ("was I opened at login?"), so the app can stay quiet.</summary>
    public static bool IsStartupActivation(AppActivationArguments activation) =>
        activation.Kind == ExtendedActivationKind.StartupTask;
}
