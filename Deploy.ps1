<#
This script performs the following actions:
1. Cleans the .bonsai directory by removing untracked files and directories.
2. Clean the python .venv by removing untracked files and directories.
3. Install uv if not installed.
4. Install python environment .venv with uv using the pyproject.toml specs
#>

git clean -fdx .bonsai

git clean -fdx .venv
irm https://astral.sh/uv/install.ps1 | iex
uv sync --all-extras
