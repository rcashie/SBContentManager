using System.Collections.Generic;
using System.Threading;

namespace AssetBasedContentManager
{
    /// <summary>
    /// A batch of requested assets.
    /// </summary>
    internal sealed class AssetRequestBatch
    {
        /// <summary>
        /// Indicates whether the assets were loaded or not.
        /// </summary>
        private int mLoaded;

        /// <summary>
        ///  List of request items.
        /// </summary>
        internal readonly List<IAssetLoader> mRequestedItems;

        /// <summary>
        /// The asset session associated with the batch.
        /// </summary>
        public readonly AssetSession mSession;

        /// <summary>
        /// Initializes a new instance of the AssetRequestBatch class.
        /// </summary>
        public AssetRequestBatch(AssetSession _session)
        {
            mSession = _session;
            mRequestedItems = new List<IAssetLoader>();
            mLoaded = 1;
        }

        /// <summary>
        /// Gets the batch count. This is not thread safe.
        /// </summary>
        public int ItemCount
        {
            get
            {
                return mRequestedItems.Count;
            }
        }

        /// <summary>
        /// Returns whether loading is done or not.
        /// </summary>
        public bool IsLoaded()
        {
            return mLoaded == 1;
        }

        /// <summary>
        /// Add an item to load.
        /// </summary>
        public void AddItem<T>(ReferenceObject<T> _assetPointer, string _url)
        {
            mRequestedItems.Add(new AssetRequest<T>(_assetPointer, _url));
            Interlocked.Exchange(ref mLoaded, 0);
        }

        /// <summary>
        /// Sets the batch to done.
        /// </summary>
        internal void SetLoaded()
        {
            Interlocked.Exchange(ref mLoaded, 1);
        }
    }
}
