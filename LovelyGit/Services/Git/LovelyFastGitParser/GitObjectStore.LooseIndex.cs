namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed partial class GitObjectStore
{
    private bool MightHaveLooseObject(string objectId)
    {
        EnsureLooseObjectIndex();
        return _looseObjectIndexTooLarge || _looseObjectIds?.Contains(objectId) == true;
    }

    private void EnsureLooseObjectIndex()
    {
        if (Volatile.Read(ref _looseObjectIndexLoaded))
        {
            return;
        }

        lock (_looseObjectIndexGate)
        {
            if (_looseObjectIndexLoaded)
            {
                return;
            }

            const int maxIndexedLooseObjects = 10_000;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var objectsPath in _objectDirectories)
            {
                LoadLooseObjectIndex(objectsPath, ids, maxIndexedLooseObjects);
                if (_looseObjectIndexTooLarge)
                {
                    break;
                }
            }

            _looseObjectIds = ids;
            Volatile.Write(ref _looseObjectIndexLoaded, true);
        }
    }

    private void LoadLooseObjectIndex(
        string objectsPath,
        HashSet<string> ids,
        int maxIndexedLooseObjects)
    {
        foreach (var directory in Directory.EnumerateDirectories(objectsPath))
        {
            var prefix = Path.GetFileName(directory);
            if (prefix.Length != 2 || !GitObjectId.IsHexPrefix(prefix))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(directory))
            {
                ids.Add(prefix + Path.GetFileName(file));
                if (ids.Count <= maxIndexedLooseObjects)
                {
                    continue;
                }

                _looseObjectIndexTooLarge = true;
                ids.Clear();
                return;
            }
        }
    }
}
