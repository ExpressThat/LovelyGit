# Bundled Git Binaries

This folder is the intended local shape for Git binaries when testing a bundled
Git distribution.

Do not commit downloaded Git binary payloads to the repository. Release builds
should download or unpack the selected `desktop/dugite-native` artifact into the
publish output instead.

The selected upstream version, release artifacts, and SHA-256 hashes are stored
in:

```text
dugite-native.json
```

For local development, hydrate this ignored folder with the archive for your
current OS and CPU from `LovelyGit/Frontend`:

```text
pnpm git-binaries:download
```

To bump the upstream dugite-native release and recompute all release artifact
hashes:

```text
pnpm git-binaries:bump v2.53.0-3
```

Expected publish layout:

```text
Git/
  LICENSE.txt
  cmd/
  mingw64/
  usr/
  ...
```

LovelyGit should invoke the bundled executable as a separate process, for
example:

```text
Git/cmd/git.exe
```

Keep license and source information under:

```text
ThirdPartyNotices/Git/
```
