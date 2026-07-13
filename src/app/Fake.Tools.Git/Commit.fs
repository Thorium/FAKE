namespace Fake.Tools.Git

open Fake.Core

/// <summary>
/// Contains helper functions which allow to commit to git repositories.
/// </summary>
[<RequireQualifiedAccess>]
module Commit =

    /// <summary>
    /// Commits all files in the given repository with the given message
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="message">The commit message text.</param>
    let exec repositoryDir message =
        Args.toWindowsCommandLine [ "commit"; "-m"; message ]
        |> CommandHelper.runSimpleGitCommand repositoryDir
        |> Trace.trace

    /// <summary>
    /// Commits all files in the given repository with the given short message along with an extended message
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="shortMessage">The commit short (title) message text.</param>
    /// <param name="extendedMessage">The commit extended (description) message text.</param>
    let execExtended repositoryDir shortMessage extendedMessage =
        Args.toWindowsCommandLine [ "commit"; "-m"; shortMessage; "-m"; extendedMessage ]
        |> CommandHelper.runSimpleGitCommand repositoryDir
        |> Trace.trace
