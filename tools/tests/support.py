"""Shared by the tools tests: the fixture app, copied into a temporary folder so a test may change it, and how to run a
shell script on each OS."""

from __future__ import annotations

import os
import shutil
import sys
import tempfile
from contextlib import contextmanager
from pathlib import Path

HERE = Path(__file__).resolve().parent
TOOLS = HERE.parent
REPO = TOOLS.parent
FIXTURE_APP = HERE / "fixture-app"
WINDOWS = os.name == "nt"

sys.path.insert(0, str(TOOLS))
import companion  # noqa: E402


BASH = companion.git_bash() if WINDOWS else "bash"
"""The bash that runs the shell scripts; None on a Windows without Git for Windows."""

NO_SHELL = "Windows starts no shell script itself and has no Git for Windows bash here"
"""The reason a test that runs a shell script skips when `BASH` is None."""

NO_SHEBANG = "Windows can't start a `#!/bin/sh` file as a program, as this test's fake tool is"
"""The reason a test whose fake tool is a shell script skips on Windows."""


def shell(script: Path) -> list[str]:
    """The command that runs `script`: the script itself on macOS and Linux, through `BASH` on Windows."""
    return companion.shell_command(script) or [str(script)]


def search_path(*folders: Path) -> str:
    """$PATH with `folders` first (a test's fake commands), joined as this OS joins it."""
    return os.pathsep.join([*map(str, folders), os.environ["PATH"]])


@contextmanager
def fixture_app():
    """A fresh copy of the fixture app, with $COMPANION_ROOT pointing at it while the block runs."""
    with tempfile.TemporaryDirectory() as temp:
        root = Path(temp).resolve() / "app"
        shutil.copytree(FIXTURE_APP, root)
        previous = os.environ.get("COMPANION_ROOT")
        os.environ["COMPANION_ROOT"] = str(root)
        try:
            yield root
        finally:
            if previous is None:
                os.environ.pop("COMPANION_ROOT", None)
            else:
                os.environ["COMPANION_ROOT"] = previous
