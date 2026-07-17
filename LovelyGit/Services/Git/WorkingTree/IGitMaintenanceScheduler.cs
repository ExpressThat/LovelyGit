namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal interface IGitMaintenanceScheduler
{
    void Schedule(string repositoryPath);
}
