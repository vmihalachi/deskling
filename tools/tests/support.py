"""Shared by the tools tests: the fixture app, copied into a temporary folder so a test may change it."""

from __future__ import annotations

import os
import shutil
import tempfile
from contextlib import contextmanager
from pathlib import Path

HERE = Path(__file__).resolve().parent
TOOLS = HERE.parent
REPO = TOOLS.parent
FIXTURE_APP = HERE / "fixture-app"


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
