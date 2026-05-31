using System;

namespace Odoro
{
    public enum StageAvatarOptionKind
    {
        ProceduralSkeleton,
        ResourcesPrefab,
    }

    [Serializable]
    public sealed class StageAvatarOption
    {
        public string id;
        public StageAvatarOptionKind kind;
        public string title;
        public string subtitle;
        public string resourcePath;
        public bool isInstalled;
        public bool canInstall;
        public bool canUninstall;

        public bool UsesAvatar => kind != StageAvatarOptionKind.ProceduralSkeleton;
    }
}
