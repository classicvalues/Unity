using System.Linq;
using System.Threading;
using GitHub.Unity;
using NSubstitute;
using TestUtils;

namespace IntegrationTests
{
    class BaseGitEnvironmentTest : BaseGitRepoTest
    {
        protected void InitializeEnvironment(NPath repoPath, bool enableEnvironmentTrace = false)
        {
            Environment = new IntegrationTestEnvironment(repoPath, SolutionDirectory, enableTrace: enableEnvironmentTrace);

            var gitSetup = new GitSetup(Environment, FileSystem, CancellationToken.None);
            gitSetup.SetupIfNeeded().Wait();

            Environment.GitExecutablePath = gitSetup.GitExecutablePath;

            FileSystem.SetCurrentDirectory(repoPath);

            Platform = new Platform(Environment, FileSystem);
            GitEnvironment = Platform.GitEnvironment;
            ProcessManager = new ProcessManager(Environment, GitEnvironment);
            Platform.Initialize(ProcessManager, new TestUIDispatcher());

            Environment.GitExecutablePath = GitEnvironment.FindGitInstallationPath(ProcessManager).Result;

            var taskRunner = new TaskRunnerBase(new TestSynchronizationContext(), CancellationToken.None);
            taskRunner.Run();

            var usageTracker = new NullUsageTracker();
            var repositoryManagerFactory = new RepositoryManagerFactory();
            RepositoryManager = repositoryManagerFactory.CreateRepositoryManager(Platform, taskRunner, usageTracker, repoPath, CancellationToken.None);
            RepositoryManager.Initialize();
            RepositoryManager.Start();

            Environment.Repository = RepositoryManager.Repository;

            DotGitPath = repoPath.Combine(".git");

            if (DotGitPath.FileExists())
            {
                DotGitPath =
                    DotGitPath.ReadAllLines()
                              .Where(x => x.StartsWith("gitdir:"))
                              .Select(x => x.Substring(7).Trim())
                              .First();
            }

            BranchesPath = DotGitPath.Combine("refs", "heads");
            RemotesPath = DotGitPath.Combine("refs", "remotes");
            DotGitIndex = DotGitPath.Combine("index");
            DotGitHead = DotGitPath.Combine("HEAD");
            DotGitConfig = DotGitPath.Combine("config");
        }

        protected override void OnTearDown()
        {
            base.OnTearDown();
            RepositoryManager?.Stop();
        }

        public RepositoryManager RepositoryManager { get; private set; }

        protected Platform Platform { get; private set; }

        protected ProcessManager ProcessManager { get; private set; }

        protected IProcessEnvironment GitEnvironment { get; private set; }

        protected NPath DotGitConfig { get; private set; }

        protected NPath DotGitHead { get; private set; }

        protected NPath DotGitIndex { get; private set; }

        protected NPath RemotesPath { get; private set; }

        protected NPath BranchesPath { get; private set; }

        protected NPath DotGitPath { get; private set; }
    }
}
