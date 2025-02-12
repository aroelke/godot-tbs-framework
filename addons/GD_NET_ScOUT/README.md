# Godot .NET Scene-Oriented Unit Test

Godot .NET Scene-Oriented Unit Test (ScOUT) is a framework for writing C# unit
tests that run within scenes.

## Features

- Running unit tests within scenes makes it easy to test game logic that depends on engine features (e.g. signals) with
  minimal stubbing.
- [Automation](#project-export-and-test-automation) with [JUnit-style](https://github.com/testmoapp/junitxml) test
  reports.
- Only Godot .NET (C#) is supported.

## Setup

### Installation

Install directly from the [Godot Asset Library](https://godotengine.org/asset-library/asset/2811), or download
the [latest release](https://gitlab.com/jfletcher94/gd-net-scout/-/releases/permalink/latest) directly.

:warning: Make sure that the plugin is enabled in the project settings.

### Test Scenes

ScOUT is a scene-oriented test framework, so all unit tests are run within _test scenes_. A _test scene_ is any scene
with a [`test_runner`](addons/GD_NET_ScOUT/test_runner.tscn) node in it. The `test_runner` node has
the [`TestRunner`](addons/GD_NET_ScOUT/TestRunner.cs) script. To create a _test scene_:

- Create a new scene.
- Add a [`test_runner.tscn`](addons/GD_NET_ScOUT/test_runner.tscn) node anywhere in the scene hierarchy.
- (Optional) Configure `test_runner` node.

The empty _test scene_ should run without any errors. There is a UI that allows for reloading and running tests. During
test execution, this UI is hidden so as not to interfere with the tests.

### Test Classes and Methods

When ScOUT runs a _test scene_, it finds all nodes in the scene with a _test class_ as the node's script. It then runs
all _test methods_ for each _test class_.

#### The [`[Test]` Attribute](addons/GD_NET_ScOUT/TestAttribute.cs#L6)

A _test class_ is any class that:

- Extends `Godot.Node`.
- Has the [`[Test]` Attribute](addons/GD_NET_ScOUT/TestAttribute.cs#L6).

A _test method_ is any method that:

- Is a non-static member of a test class.
- Has the [`[Test]` Attribute](addons/GD_NET_ScOUT/TestAttribute.cs#L6).
- Has `void` return type and takes no parameters.

#### Assertions

The [`Assert`](addons/GD_NET_ScOUT/Assert.cs) class is a (static) class with utility methods for checking tests' failure
conditions.

When an [`Assert`](addons/GD_NET_ScOUT/Assert.cs) method fails (i.e. the specified condition is not met) it throws an
exception that ScOUT recognizes as meaning a test _failure_. If a test throws an exception that ScOUT does not
recognize, ScOUT will interpret that as a test _error_—to handle expected exceptions,
use [`Assert#Throws`](addons/GD_NET_ScOUT/Assert.cs#L167)
and/or [`Assert#DoesNotThrow`](addons/GD_NET_ScOUT/Assert.cs#L190).

### Running Multiple Test Scenes

#### Test Scene Runners

Multiple test scenes can be run in series by using a _test scene runner_—this a scene that is configured to run a
specific set of _test scenes_. These can be created manually
using [`test_scene_runner.tscn`](addons/GD_NET_ScOUT/test_scene_runner.tscn), but the recommended way is by using
the `ScOUT` editor tab.

When running individual _test scenes_, the ScOUT UI is active; tests can be reloaded and run multiple times. When
running _test scene runners_, the ScOUT UI is inactive and the program automatically exits after the last _test scene_
has run.

#### The `ScOUT` Editor Tab

When the plugin is enabled, a new `ScOUT` tab is added to the editor, next to `Inspector`, `Node` and `History`. The
primary function of the `ScOUT` tab is to create preconfigured _test scene runners_.

| Option                               | Usage                                                                                                                                                                                                          |
|--------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Test Scene Files`                   | Add individual _test scene_ `*.tscn` files.                                                                                                                                                                    |
| `Test Scene Directories`             | Add individual directories containing _test scene_ `*.tscn` files (other file types are ignored).                                                                                                              |
| `Verbose Logging`                    | Whether to print individual test results to `stdout`.                                                                                                                                                          |
| `Print Test Reports to StdOut`       | Whether to print a JUnit-style test report to `stdout`.                                                                                                                                                        |
| `Export Test Reports`                | Whether (and where) to save a JUnit-style test report file.                                                                                                                                                    |
| `Set as main scene for test exports` | Whether to add the generated _test scene runner_ as the value for `Main Scene` when [exporting project](#project-export-and-test-automation) with the `gdnetscout` feature. Overwrites previous value, if any. |
| `Save As`                            | Save new _test scene runner_ with the `ScOUT` tab's current configuration.                                                                                                                                     |

## Additional Functionality

### Other Attributes

#### [`[BeforeEach]`](addons/GD_NET_ScOUT/TestAttribute.cs#L9), [`[AfterEach]`](addons/GD_NET_ScOUT/TestAttribute.cs#L12), [`[BeforeAll]`](addons/GD_NET_ScOUT/TestAttribute.cs#L15) and [`[AfterAll]`](addons/GD_NET_ScOUT/TestAttribute.cs#L18)

These attributes can be added to a method in a _test class_ to run before/after each/all _test method_. The method must
not be a _test method_, and must be non-static with `void` return type and no parameters. There can be at most one
method with each attribute per _test class_, and a single method cannot have more than one.

| Method         | Runs                                                      | On Failure           |
|----------------|-----------------------------------------------------------|----------------------|
| `[BeforeEach]` | Repeatedly, before each _test method_.                    | _Test method_ fails. |
| `[AfterEach]`  | Repeatedly, after each _test method_.                     | _Test method_ fails. |
| `[BeforeAll]`  | Once, before the first _test method_ of the _test class_. | _Test class_ fails.  |
| `[AfterAll]`   | Once, after the last _test method_ of the _test class_.   | _Test class_ fails.  |

#### [`[Skip]`](addons/GD_NET_ScOUT/TestAttribute.cs#L21)

A _test class_ or _test method_ with the `[Skip]` attribute will be _skipped_. _Skipped_ tests are not run, though they
are still included in test reports.

### Using `TestRunner`'s Utility Methods

The [`TestRunner`](addons/GD_NET_ScOUT/TestRunner.cs) class has a number of utility methods that can be called by _test
methods_.

#### Accessing the Active `TestRunner` Instance

Every test scene has one [`test_runner`](addons/GD_NET_ScOUT/test_runner.tscn) node, and therefore one instance
of [`TestRunner`](addons/GD_NET_ScOUT/TestRunner.cs). Any object that extends `Godot.Node` can access the
active `TestRunner` isntance with the extension method `Node#GetTestRunner`. If called from an inappropriate context,
the method will throw an exception.

#### Waiting and Delaying Invocation

[`TestRunner`](addons/GD_NET_ScOUT/TestRunner.cs) has methods for waiting a single frame, for a fixed amount of time, or
for a specified signal. Each method takes an `Action` as an argument, which is invoked after the waiting is finished.

:warning: Only the `TestRunner` waits, not the invoking _test method_. Any code that should not run until after waiting
is over must be part of the provided `Action`.

| Method                                                               | Details                                                                                                                                               | Example                                                                           |
|----------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------|
| [`TestRunner.WaitFrames`](addons/GD_NET_ScOUT/TestRunner.cs#L124)    | Wait the g iven numer of `frames`, then perform the given `Action`.                                                                                   | [`TestRunnerTest.WaitFrames`](addons/GD_NET_ScOUT/test/TestRunnerTest.cs#L73)     |
| [`TestRunner.WaitSeconds`](addons/GD_NET_ScOUT/TestRunner.cs#L145)   | Wait the given number of seconds, then invoke the given `Action`.                                                                                     | [`TestRunnerTest.WaitSeconds`](addons/GD_NET_ScOUT/test/TestRunnerTest.cs#L101)   |
| [`TestRunner.WaitForSignal`](addons/GD_NET_ScOUT/TestRunner.cs#L179) | Wait for the given `Node` to emit a signal of the given name, then invoke the given `Action`. Fail if signal is not emitted within the given timeout. | [`TestRunnerTest.WaitForSignal`](addons/GD_NET_ScOUT/test/TestRunnerTest.cs#L118) |

All of these methods can have their invocations chained together by nesting the method calls in `Action`.
See [`TestRunnerTest.WaitForSignal`](addons/GD_NET_ScOUT/test/TestRunnerTest.cs#L118).

#### Printing

[`TestRunner`](addons/GD_NET_ScOUT/TestRunner.cs) has methods for printing that respect the configured verbosity of
test. They delegate to Godot's print methods and handle formatting.

| Method                 | Delegates to   | Example                                                                                            |
|------------------------|----------------|----------------------------------------------------------------------------------------------------|
| `TestRunner.Print`     | `GD.Print`     | `this.GetTestRunner().Print("pi is {0:0.####}...", Math.PI); // "pi is 3.1416..."`                 |
| `TestRunner.PrintRich` | `GD.PrintRich` | `this.GetTestRunner().PrintRich("[center]e is {0:0.###}...[/center]", Math.E); // "e is 2.718..."` |
| `TestRunner.PrintErr`  | `GD.PrintErr`  | `this.GetTestRunner().Print("Exception: {0}", e); // "Exception: <stack trace>"`                   |

### Project Export and Test Automation

In addition to running _test scenes_ and _test scene runners_ directly in the editor, because they are regular Godot
scenes, they can be exported using project export. A _test scene runner_ is much better suited for test automation than
a _test scene_.

#### Creating a ScOUT Export Preset

Create a ScOUT-specific export preset. If exporting for CI, a `Linux` preset is recommended. Add `gdnetscout`
to `Custom` in the `Features` tab. No other ScOUT-specific configuration is required.

The `gdnetscout` custom feature allows the ScOUT export preset to work without affecting anything non-test-related.

:warning: This requires that the `Set as main scene for test exports` option was selected in the `ScOUT` tab when the
_test scene runner_ was created. If not, the _test scene runner_ can be recreated using the `ScOUT` tab.

#### Automated Testing with GitLab CI

This is a broad overview of how this project uses GitLab CI for automated testing, and is not comprehensive. It assumes
some existing knowledge of GitLab CI, Docker and running Godot from the command line.

Create a [`.gitlab-ci.yml`](.gitlab-ci.yml) file that does the following:

- (Optional) Use GitLab container registry to re-use images (see [`Dockerfile`](Dockerfile)).
- Use an image that has (or install them):
    - Dotnet.
    - Godot.
- Download Godot export templates, and move to proper location.
- Export project using ScOUT-specific export preset.
- Run exported project in headless mode.
- Keep the test report in `artifacts.reports.junit`.

#### For an example of a project using ScOUT for testing, see [this project](https://gitlab.com/jfletcher94/isometric-tactics).

It includes:

- Multiple test scenes.
- Utilization of `TestRunner` helper methods.
- Automated testing using GitLab CI.

By [using `Xvfb`](https://gitlab.com/jfletcher94/isometric-tactics/-/blob/master/Dockerfile?ref_type=heads), it's able
to [run tests in non-headless mode](https://gitlab.com/jfletcher94/isometric-tactics/-/blob/master/.gitlab-ci.yml#L79-91).
This
enables [automated testing of shaders](https://gitlab.com/jfletcher94/isometric-tactics/-/blob/master/test/TileMapHelperNodeTest.cs#L267-597)
using ScOUT.
