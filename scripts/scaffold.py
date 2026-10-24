#!/usr/bin/env python3
"""Scaffold a new Godot 4 C# game project from this skill's templates.

Standard library only. Copies templates/project/ into an empty folder, replacing the __TOKEN__ placeholders in
paths and file contents, generates the non-English PO catalogs, then (unless --skip-dotnet) creates the solution,
restores the local tools, and runs CSharpier so the result passes `dotnet csharpier check .` straight away.

Skill uploads drop every path that starts with a dot, so the template stores dotfiles as _dot_<name> (restored to
.<name> on copy), and templates/MANIFEST.txt lists every template file, so a lost file fails loudly instead of
being silently missing from the project.

Example:
    python scaffold.py --name StarForge --title "Star Forge" --dest D:/projects/star-forge \
        --godot 4.5.1-stable --locales en,es,de --ci gitlab

Before uploading the skill, check the template; after adding or removing template files, update the manifest:
    python scaffold.py --check-template
    python scaffold.py --update-manifest
"""

from __future__ import annotations

import argparse
import datetime
import re
import shutil
import subprocess
import sys
from pathlib import Path

SKILL_DIR = Path(__file__).resolve().parent.parent
TEMPLATE_DIR = SKILL_DIR / "templates" / "project"
MANIFEST = SKILL_DIR / "templates" / "MANIFEST.txt"

# gettext Plural-Forms headers for common locales; anything else gets the two-form default with a warning.
PLURAL_FORMS = {
    "en": "nplurals=2; plural=(n != 1);",
    "de": "nplurals=2; plural=(n != 1);",
    "es": "nplurals=2; plural=(n != 1);",
    "it": "nplurals=2; plural=(n != 1);",
    "nl": "nplurals=2; plural=(n != 1);",
    "sv": "nplurals=2; plural=(n != 1);",
    "fi": "nplurals=2; plural=(n != 1);",
    "da": "nplurals=2; plural=(n != 1);",
    "nb": "nplurals=2; plural=(n != 1);",
    "pt": "nplurals=2; plural=(n != 1);",
    "pt_BR": "nplurals=2; plural=(n > 1);",
    "fr": "nplurals=2; plural=(n > 1);",
    "tr": "nplurals=2; plural=(n != 1);",
    "ru": "nplurals=3; plural=(n%10==1 && n%100!=11 ? 0 : n%10>=2 && n%10<=4 && (n%100<10 || n%100>=20) ? 1 : 2);",
    "uk": "nplurals=3; plural=(n%10==1 && n%100!=11 ? 0 : n%10>=2 && n%10<=4 && (n%100<10 || n%100>=20) ? 1 : 2);",
    "pl": "nplurals=3; plural=(n==1 ? 0 : n%10>=2 && n%10<=4 && (n%100<10 || n%100>=20) ? 1 : 2);",
    "cs": "nplurals=3; plural=(n==1) ? 0 : (n>=2 && n<=4) ? 1 : 2;",
    "zh_CN": "nplurals=1; plural=0;",
    "zh_TW": "nplurals=1; plural=0;",
    "ja": "nplurals=1; plural=0;",
    "ko": "nplurals=1; plural=0;",
}

RENDERERS = {
    # name: (config/features entry, rendering_method value or None for the default Forward+)
    "forward_plus": ("Forward Plus", None),
    "mobile": ("Mobile", "mobile"),
    "gl_compatibility": ("GL Compatibility", "gl_compatibility"),
}

# Template source paths (dotfiles are stored as _dot_<name>) that belong to one CI host only.
CI_PATHS = {
    "gitlab": ["_dot_gitlab-ci.yml", "_dot_gitlab"],
    "github": ["_dot_github"],
}

RESERVED_NAMES = {"Godot", "System", "Microsoft", "Game", "Core", "Tests", "Node", "Object", "Resource", "Engine"}


def fail(message: str) -> None:
    print(f"error: {message}", file=sys.stderr)
    sys.exit(2)


def template_files() -> list[str]:
    """Lists every file in templates/project/ as a POSIX path relative to it, sorted."""
    return sorted(path.relative_to(TEMPLATE_DIR).as_posix() for path in TEMPLATE_DIR.rglob("*") if path.is_file())


def reject_dot_paths(files: list[str]) -> None:
    """Fails on template paths with a segment starting with a dot: skill uploads drop them."""
    dotted = [path for path in files if any(part.startswith(".") for part in path.split("/"))]
    if dotted:
        fail(
            "Template paths must not start with a dot, because skill uploads drop them. "
            "Rename each dot segment to _dot_<name> (the copy restores the dot): " + ", ".join(dotted)
        )


def check_template() -> None:
    """Fails unless templates/project/ holds exactly the files listed in templates/MANIFEST.txt."""
    present = template_files()
    reject_dot_paths(present)
    if not MANIFEST.is_file():
        fail(f"{MANIFEST} is missing, so the template can't be checked. Restore it from a verified copy of the skill.")
    expected = [
        line.strip()
        for line in MANIFEST.read_text(encoding="utf-8").splitlines()
        if line.strip() and not line.startswith("#")
    ]
    missing = sorted(set(expected) - set(present))
    if missing:
        fail("The template is incomplete (files lost in upload?). Missing: " + ", ".join(missing))
    unlisted = sorted(set(present) - set(expected))
    if unlisted:
        fail(
            "Template files missing from templates/MANIFEST.txt (run with --update-manifest after adding files): "
            + ", ".join(unlisted)
        )


def write_manifest() -> None:
    """Rewrites templates/MANIFEST.txt from the files currently in templates/project/."""
    files = template_files()
    reject_dot_paths(files)
    header = (
        "# Every file in templates/project/. scaffold.py refuses to run if one is missing or unlisted.\n"
        "# Regenerate with: python scripts/scaffold.py --update-manifest\n"
    )
    MANIFEST.write_text(header + "\n".join(files) + "\n", encoding="utf-8", newline="\n")
    print(f"Wrote {len(files)} entries to {MANIFEST}.")


def parse_godot_release(release: str) -> tuple[str, str]:
    """Maps a godot-builds release tag to (Godot.NET.Sdk version, config/features version).

    4.5.1-stable -> 4.5.1, 4.6-stable -> 4.6.0, 4.8-dev7 -> 4.8.0-dev.7, 4.6-beta2 -> 4.6.0-beta.2.
    """
    match = re.fullmatch(r"(\d+)\.(\d+)(?:\.(\d+))?-(stable|dev|beta|rc)(\d*)", release)
    if not match:
        fail(f"--godot must be a godot-builds release tag such as 4.5.1-stable or 4.8-dev7, not '{release}'.")
    major, minor, patch, kind, number = match.groups()
    patch = patch or "0"
    if (int(major), int(minor)) < (4, 5):
        fail("Godot 4.5 or later is required (the runtime test runner uses OS.AddLogger, added in 4.5).")
    if kind == "stable":
        sdk = f"{major}.{minor}.{patch}"
    else:
        if not number:
            fail(f"A {kind} release tag needs its number, e.g. 4.8-{kind}1.")
        sdk = f"{major}.{minor}.{patch}-{kind}.{number}"
    return sdk, f"{major}.{minor}"


def detect_dotnet_sdk(tfm: str) -> str:
    """Picks the newest installed .NET SDK whose major version matches the target framework."""
    major = re.fullmatch(r"net(\d+)\.0", tfm)
    if not major:
        fail(f"--tfm must look like net10.0, not '{tfm}'.")
    try:
        output = subprocess.run(["dotnet", "--list-sdks"], capture_output=True, text=True, check=True).stdout
    except (OSError, subprocess.CalledProcessError):
        fail("Could not run 'dotnet --list-sdks'. Install the .NET SDK or pass --dotnet-sdk.")
    versions = []
    for line in output.splitlines():
        version = line.split(" ", 1)[0]
        if re.fullmatch(rf"{major.group(1)}\.0\.\d+", version):
            versions.append(version)
    if not versions:
        fail(f"No .NET {major.group(1)} SDK is installed. Install one or pass --dotnet-sdk.")
    return max(versions, key=lambda v: tuple(int(part) for part in v.split(".")))


def po_for_locale(english: str, locale: str, title: str) -> str:
    """Builds a translated catalog from en.po: same keys, empty msgstr, each entry flagged fuzzy."""
    if locale not in PLURAL_FORMS:
        print(f"warning: no Plural-Forms known for '{locale}'; using the two-form default. Check it.")
    plural = PLURAL_FORMS.get(locale, PLURAL_FORMS["en"])
    lines = english.split("\n")
    out: list[str] = [
        f"# {title}: {locale}.",
        "# Untranslated entries keep an empty msgstr and the fuzzy flag; the game falls back to English.",
    ]
    in_header = True
    index = 0
    # Skip en.po's leading comment lines; they describe the English catalog.
    while index < len(lines) and lines[index].startswith("#"):
        index += 1
    for line in lines[index:]:
        if in_header:
            if line.startswith('"Language:'):
                line = f'"Language: {locale}\\n"'
            elif line.startswith('"Plural-Forms:'):
                line = f'"Plural-Forms: {plural}\\n"'
            elif line == "":
                in_header = False
            out.append(line)
            continue
        if line.startswith("msgid "):
            out.append("#, fuzzy")
            out.append(line)
        elif line.startswith("msgstr"):
            out.append(re.sub(r'^(msgstr(\[\d+\])?) ".*"$', r'\1 ""', line))
        else:
            out.append(line)
    return "\n".join(out)


def run(command: list[str], cwd: Path) -> None:
    print("  $ " + " ".join(command))
    result = subprocess.run(command, cwd=cwd)
    if result.returncode != 0:
        fail(f"'{' '.join(command)}' failed with exit code {result.returncode}.")


def main() -> None:
    parser = argparse.ArgumentParser(description="Scaffold a Godot 4 C# game project.")
    parser.add_argument("--name", help="Required. PascalCase identifier: assembly and namespace root, e.g. StarForge")
    parser.add_argument("--title", help="Display title, e.g. 'Star Forge' (default: the name)")
    parser.add_argument("--dest", help="Required. Target folder; must be empty or hold only .git (see --overwrite)")
    parser.add_argument("--godot", help="Required. godot-builds release tag, e.g. 4.5.1-stable or 4.8-dev7")
    parser.add_argument("--tfm", default="net10.0", help="Target framework (default net10.0); must be >= GodotSharp's")
    parser.add_argument("--dotnet-sdk", help="Exact .NET SDK for global.json and CI (default: newest installed match)")
    parser.add_argument("--locales", default="en", help="Comma-separated Godot locale codes; en first (default: en)")
    parser.add_argument("--ci", choices=["gitlab", "github", "none"], default="gitlab")
    parser.add_argument("--renderer", choices=sorted(RENDERERS), default="forward_plus")
    parser.add_argument("--skip-dotnet", action="store_true", help="Skip solution creation, tool restore, and format")
    parser.add_argument(
        "--overwrite",
        action="store_true",
        help="Allow a non-empty --dest: template files replace same-named files, other files are kept",
    )
    parser.add_argument(
        "--check-template", action="store_true", help="Only check the template against its manifest (before uploading)"
    )
    parser.add_argument(
        "--update-manifest", action="store_true", help="Rewrite templates/MANIFEST.txt (after adding or removing files)"
    )
    args = parser.parse_args()

    if args.update_manifest:
        write_manifest()
        return
    check_template()
    if args.check_template:
        print(f"Template OK: {len(template_files())} files match {MANIFEST.name}, none dot-prefixed.")
        return
    missing_arguments = [f"--{option}" for option in ("name", "dest", "godot") if not getattr(args, option)]
    if missing_arguments:
        parser.error("the following arguments are required: " + ", ".join(missing_arguments))

    name = args.name
    if not re.fullmatch(r"[A-Z][A-Za-z0-9]*", name):
        fail("--name must be PascalCase letters and digits, e.g. StarForge (it becomes the assembly name).")
    if name in RESERVED_NAMES:
        fail(f"'{name}' would collide with a framework or project namespace; pick a distinctive name.")
    title = args.title or name
    if '"' in title or "\\" in title:
        fail("--title must not contain quotes or backslashes.")

    locales = [locale.strip() for locale in args.locales.split(",") if locale.strip()]
    if not locales or locales[0] != "en":
        fail("--locales must start with en, the source-of-truth catalog.")
    for locale in locales:
        if not re.fullmatch(r"[a-z]{2,3}(_[A-Z]{2})?", locale):
            fail(f"'{locale}' is not a Godot locale code such as es, pt_BR, or zh_CN.")
    translated = locales[1:]

    godot_sdk, godot_feature = parse_godot_release(args.godot)
    dotnet_sdk = args.dotnet_sdk or detect_dotnet_sdk(args.tfm)
    renderer_feature, rendering_method = RENDERERS[args.renderer]

    dest = Path(args.dest).resolve()
    if dest.exists() and any(child.name != ".git" for child in dest.iterdir()) and not args.overwrite:
        fail(
            f"{dest} is not empty. Scaffold into an empty folder (an initialized git repo is fine), "
            "or pass --overwrite to replace same-named files and keep the rest."
        )
    dest.mkdir(parents=True, exist_ok=True)
    in_git_repo = (dest / ".git").exists()
    if args.overwrite and not in_git_repo:
        print(f"warning: {dest} is not a git repository, so replaced files can't be recovered.")

    tokens = {
        "__GAME__": name,
        "__TITLE__": title,
        "__GODOT_RELEASE__": args.godot,
        "__GODOT_SDK__": godot_sdk,
        "__GODOT_FEATURE__": godot_feature,
        "__DOTNET_SDK__": dotnet_sdk,
        "__TFM__": args.tfm,
        "__RENDERER_FEATURE__": renderer_feature,
        "__RENDERING_SECTION__": (
            f'[rendering]\n\nrenderer/rendering_method="{rendering_method}"\n' if rendering_method else ""
        ),
        "__TRANSLATIONS__": ", ".join(f'"res://localization/{locale}.po"' for locale in locales),
        "__TRANSLATED_LOCALES__": ", ".join(f'"{locale}"' for locale in translated),
        "__LOCALES__": ", ".join(locales),
        "__DATE__": datetime.date.today().isoformat(),
    }

    skipped = [path for ci, paths in CI_PATHS.items() if ci != args.ci for path in paths]
    written: list[Path] = []
    replaced: list[str] = []
    for source in sorted(TEMPLATE_DIR.rglob("*")):
        if not source.is_file():
            continue
        relative = source.relative_to(TEMPLATE_DIR).as_posix()
        if any(relative == path or relative.startswith(path + "/") for path in skipped):
            continue
        target_relative = "/".join(
            "." + part[len("_dot_") :] if part.startswith("_dot_") else part for part in relative.split("/")
        )
        for token, value in tokens.items():
            target_relative = target_relative.replace(token, value)
        target = dest / target_relative
        if target.exists():
            replaced.append(target_relative)
        target.parent.mkdir(parents=True, exist_ok=True)
        text = source.read_text(encoding="utf-8")
        for token, value in tokens.items():
            text = text.replace(token, value)
        target.write_text(text, encoding="utf-8", newline="")
        if target.suffix == ".sh" or target.parent.name == ".githooks":
            target.chmod(0o755)
        written.append(target)
    copied = len(written)

    english = (dest / "localization" / "en.po").read_text(encoding="utf-8")
    for locale in translated:
        catalog = dest / "localization" / f"{locale}.po"
        if catalog.exists():
            replaced.append(f"localization/{locale}.po")
        catalog.write_text(po_for_locale(english, locale, title), encoding="utf-8", newline="")
        written.append(catalog)

    # Only what the scaffold wrote: files that were already in the folder may use __WORDS__ legitimately.
    leftovers = [
        path.relative_to(dest).as_posix()
        for path in written
        if re.search(r"__[A-Z][A-Z_]*__", path.read_text(encoding="utf-8", errors="ignore"))
    ]
    if leftovers:
        fail("Unreplaced tokens in: " + ", ".join(leftovers))

    print(f"Copied {copied} files and {len(translated)} translated catalogs to {dest}.")
    if replaced:
        print(f"Replaced {len(replaced)} existing files (other existing files were kept):")
        for path in replaced:
            print(f"  {path}")
        if in_git_repo:
            print("Review them with `git diff` and merge back any settings to keep (project.godot especially).")

    if not args.skip_dotnet:
        new_sln = ["dotnet", "new", "sln", "--format", "sln", "-n", name]
        run(new_sln + (["--force"] if args.overwrite else []), dest)
        run(
            [
                "dotnet",
                "sln",
                f"{name}.sln",
                "add",
                f"{name}.csproj",
                f"src/{name}.Core/{name}.Core.csproj",
                f"tests/{name}.Core.Tests/{name}.Core.Tests.csproj",
                f"tools/{name}.Wiki/{name}.Wiki.csproj",
            ],
            dest,
        )
        sln = dest / f"{name}.sln"
        sln.write_bytes(sln.read_bytes().replace(b"\r\n", b"\n").replace(b"\n", b"\r\n"))
        run(["dotnet", "tool", "restore"], dest)
        if args.overwrite:
            print("Note: CSharpier formats every C# and MSBuild file in the folder, including ones that were there.")
        run(["dotnet", "csharpier", "format", "."], dest)

    print()
    print("Next steps (see the skill's references/verification.md):")
    if args.skip_dotnet:
        # The skipped steps come first: without them there's no solution to build, and CSharpier's check fails, since
        # the token replacement changes line lengths and using order.
        projects = f"{name}.csproj src/{name}.Core/{name}.Core.csproj tests/{name}.Core.Tests/{name}.Core.Tests.csproj"
        print("  0. Run what --skip-dotnet skipped, in the project folder:")
        print(f"     dotnet new sln --format sln -n {name}")
        print(f"     dotnet sln {name}.sln add {projects} tools/{name}.Wiki/{name}.Wiki.csproj")
        print("     dotnet tool restore  &&  dotnet csharpier format .")
    print("  1. git init (if needed), then: git lfs install; sh tools/setup-hooks.sh (or .ps1)")
    print(f"  2. dotnet build {name}.sln  &&  dotnet test --project tests/{name}.Core.Tests")
    print('  3. "$GODOT_BIN" --headless --path . --import   (then the boot and runtime-test commands in CLAUDE.md)')
    print("  4. sh tools/wiki/build.sh --strict  (or build.ps1 -Strict)")
    print(
        "  5. Fill in docs/GDD.md, docs/ART_DIRECTION.md, docs/AUDIO.md, and docs/IMPLEMENTATION_PLAN.md with the game's"
        " specifics."
    )


if __name__ == "__main__":
    main()
