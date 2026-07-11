module Fake.DotNet.Testing.VSTestTests

open System.IO
open Fake.Core
open Fake.DotNet
open Fake.DotNet.Testing
open Fake.Testing
open Expecto

[<Tests>]
let tests =
    testList
        "Fake.DotNet.Testing.VSTest.Tests"
        [ testCase "Test that we write and delete arguments file"
          <| fun _ ->
              let cp =
                  VSTest.createProcess
                      Path.GetTempFileName
                      (fun param -> { param with ToolPath = "vstest.exe" })
                      [| "assembly.dll" |]

              let file, args =
                  match cp.Command with
                  | RawCommand (file, args) -> file, args
                  | _ -> failwithf "expected RawCommand"
                  |> ArgumentHelper.checkIfMono

              Expect.equal file "vstest.exe" "Expected vstest.exe"
              Expect.equal (args |> Arguments.toArray).Length 1 "expected a single argument"
              let arg = (args |> Arguments.toArray).[0]
              Expect.stringStarts arg "@" "Expected arg to start with @"
              let argFile = arg.Substring(1)

              (use _state = cp.Hook.PrepareState()
               let contents = File.ReadAllText argFile
               let args = Args.fromWindowsCommandLine contents
               Expect.sequenceEqual args [ "assembly.dll"; "/InIsolation" ] "Expected arg file to be correct")

              Expect.isFalse (File.Exists argFile) "File should be deleted"

          testCase "Test that we can set Parallel setting"
          <| fun _ ->
              let cp =
                  VSTest.createProcess
                      Path.GetTempFileName
                      (fun param ->
                          { param with
                              ToolPath = "vstest.console.exe"
                              Parallel = true })
                      [| "assembly1.dll"; "assembly2.dll" |]

              let file, args =
                  match cp.Command with
                  | RawCommand (file, args) -> file, args
                  | _ -> failwithf "expected RawCommand"
                  |> ArgumentHelper.checkIfMono

              Expect.equal file "vstest.console.exe" "Expected vstest.console.exe"
              Expect.equal (args |> Arguments.toArray).Length 1 "expected a single argument"
              let arg = (args |> Arguments.toArray).[0]
              Expect.stringStarts arg "@" "Expected arg to start with @"
              let argFile = arg.Substring(1)

              (use _state = cp.Hook.PrepareState()
               let contents = File.ReadAllText argFile
               let args = Args.fromWindowsCommandLine contents

               Expect.sequenceEqual
                   args
                   [ "assembly1.dll"; "assembly2.dll"; "/Parallel"; "/InIsolation" ]
                   "Expected arg file to be correct")

              Expect.isFalse (File.Exists argFile) "File should be deleted"

          // A negative exit code means the test host itself crashed (e.g. an unhandled CLR exception)
          // rather than reporting failing tests, so it must fail the build even under DontFailBuild -
          // otherwise a runner that dies mid-run reports zero failures and the build goes green.
          testCase "Test that a crashed test host (negative exit code) fails the build even under DontFailBuild"
          <| fun _ ->
              let cp =
                  VSTest.createProcess
                      Path.GetTempFileName
                      (fun param ->
                          { param with
                              ToolPath = "vstest.exe"
                              ErrorLevel = Fake.Testing.Common.DontFailBuild })
                      [| "assembly.dll" |]

              use state = cp.Hook.PrepareState()

              let runWithExitCode (exitCode: int) =
                  cp.Hook.RetrieveResult(state, System.Threading.Tasks.Task.FromResult { RawExitCode = exitCode })
                  |> Async.RunSynchronously
                  |> ignore

              // Negative exit code = crash -> must throw regardless of DontFailBuild.
              Expect.throws
                  (fun () -> runWithExitCode -532462766)
                  "a crashed test host must fail the build even under DontFailBuild"

              // Positive non-zero exit code = ordinary test failure -> honours DontFailBuild (no throw).
              runWithExitCode 1

          // Under the default ErrorLevel, an ordinary test failure (positive exit code) must fail the build.
          testCase "Test that a positive exit code fails the build under the default ErrorLevel"
          <| fun _ ->
              let cp =
                  VSTest.createProcess
                      Path.GetTempFileName
                      (fun param -> { param with ToolPath = "vstest.exe" })
                      [| "assembly.dll" |]

              use state = cp.Hook.PrepareState()

              Expect.throws
                  (fun () ->
                      cp.Hook.RetrieveResult(state, System.Threading.Tasks.Task.FromResult { RawExitCode = 1 })
                      |> Async.RunSynchronously
                      |> ignore)
                  "a non-zero exit code must fail the build under the default ErrorLevel" ]
