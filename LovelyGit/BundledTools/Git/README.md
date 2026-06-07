# Bundled Git Binaries

This folder is the intended local shape for Git binaries when testing a bundled
Git distribution.

Do not commit downloaded Git binary payloads to the repository. Release builds
should download or unpack the selected `desktop/dugite-native` artifact into the
publish output instead.

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
