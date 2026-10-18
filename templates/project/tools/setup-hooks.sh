#!/bin/sh
# Points git at the repository's hooks (.githooks/), installs Git LFS, and restores the local .NET tools.
set -e
cd "$(git rev-parse --show-toplevel)"
git lfs install --skip-repo
git config core.hooksPath .githooks
chmod +x .githooks/*
dotnet tool restore
echo "Git hooks enabled (core.hooksPath = .githooks)."
