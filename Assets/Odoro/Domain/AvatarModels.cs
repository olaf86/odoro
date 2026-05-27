using System;

namespace Odoro
{
    public enum StageAvatarOptionKind
    {
        ProceduralSkeleton,
        ResourcesPrefab,
        LocalDevelopmentGlb,
        DownloadableGlb,
    }

    [Serializable]
    public sealed class StageAvatarOption
    {
        public string id;
        public StageAvatarOptionKind kind;
        public string title;
        public string subtitle;
        public string resourcePath;
        public string runtimeAssetPath;
        public string runtimeAssetRemoteUrl;
        public long runtimeAssetSizeBytes;
        public string sourceFilename;
        public bool isInstalled;

        public bool UsesAvatar => kind != StageAvatarOptionKind.ProceduralSkeleton;

        public bool RequiresDownload => kind == StageAvatarOptionKind.DownloadableGlb && !isInstalled;
    }
}
