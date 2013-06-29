
namespace AssetBasedContentManager
{
    /// <summary>
    /// The asset loader interface.
    /// </summary>
    internal interface IAssetLoader
    {
        void Load(AssetManager _assetManager, AssetSession _session);

        string AssetUrl { get; }
    }

    /// <summary>
    /// Used to pass and store a reference to an object.
    /// </summary>
    internal sealed class ReferenceObject<T>
    {
        public T mValue;

        public ReferenceObject(T _initialValue)
        {
            mValue = _initialValue;
        }
    }

    /// <summary>
    /// An item to load.
    /// </summary>
    internal sealed class AssetRequest<T> : IAssetLoader
    {
        /// <summary>
        /// Asset to load into.
        /// </summary>
        private readonly ReferenceObject<T> mAssetPointer;

        /// <summary>
        /// Url of the resource to load.
        /// </summary>
        private readonly string mUrl;

        /// <summary>
        /// Initializes a new instance of the LoadItem class.
        /// </summary>
        public AssetRequest(ReferenceObject<T> _assetPointer, string _url)
        {
            mAssetPointer = _assetPointer;
            mUrl = _url;
        }

        /// <summary>
        /// Gets the url of the asset.
        /// </summary>
        string IAssetLoader.AssetUrl
        {
            get
            {
                return mUrl;
            }
        }

        /// <summary>
        /// Load the asset
        /// </summary>
        void IAssetLoader.Load(AssetManager _assetManager, AssetSession _session)
        {
            mAssetPointer.mValue = _assetManager.Load<T>(mUrl, _session);
        }
    }
}
