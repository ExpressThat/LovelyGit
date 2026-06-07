# Bundled Git Notice

Some LovelyGit release packages include a portable Git distribution built by
the `desktop/dugite-native` project.

LovelyGit invokes the bundled Git executable as a separate command-line program.
The Git executable and its supporting files are not linked into the LovelyGit
executable. LovelyGit's own source code remains distributed under the MIT
license.

## Installed Location

When included, the bundled Git distribution is installed under:

```text
Git/
```

The Windows executable path is expected to be:

```text
Git/cmd/git.exe
```

## Upstream Package

```text
Project:  desktop/dugite-native
URL:      https://github.com/desktop/dugite-native
Release:  v2.53.0-3
Commit:   f49d009
License:  GNU General Public License v2.0
```

The upstream release identifies these component versions:

```text
Git:                    v2.53.0
Git for Windows:        v2.53.0.windows.3
Git LFS:                v3.7.1
Git Credential Manager: v2.7.3
```

The release artifacts used by LovelyGit are selected by runtime identifier:

```text
linux-x64    dugite-native-v2.53.0-f49d009-ubuntu-x64.tar.gz
linux-arm64  dugite-native-v2.53.0-f49d009-ubuntu-arm64.tar.gz
win-x64      dugite-native-v2.53.0-f49d009-windows-x64.tar.gz
win-arm64    dugite-native-v2.53.0-f49d009-windows-arm64.tar.gz
osx-x64      dugite-native-v2.53.0-f49d009-macOS-x64.tar.gz
osx-arm64    dugite-native-v2.53.0-f49d009-macOS-arm64.tar.gz
```

## Source Code

The corresponding source code and build tooling for the bundled Git
distribution are available from:

```text
https://github.com/desktop/dugite-native/tree/v2.53.0-3
```

The upstream dependency manifest for that release is available from:

```text
https://raw.githubusercontent.com/desktop/dugite-native/v2.53.0-3/dependencies.json
```

## License

The bundled Git distribution and dugite-native packaging are distributed under
the GNU General Public License v2.0. A copy of the GPLv2 license is included at:

```text
ThirdPartyNotices/Git/GPL-2.0.txt
```

See `SOURCE-OFFER.md` in this folder for source availability information.
