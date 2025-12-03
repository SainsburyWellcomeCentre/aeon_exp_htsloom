<#
This script performs the following actions:
1. Cleans the .bonsai directory by removing untracked files and directories.
2. Install bonsai by running setup.ps1 in bonsai folder.
7. Clean the python .venv by removing untracked files and directories.
8. Install uv if not installed.
9. Install python environment .venv with uv using the pyproject.toml specs
#>

git clean -fdx .bonsai

git clean -fdx .venv
irm https://astral.sh/uv/install.ps1 | iex
uv sync --all-extras