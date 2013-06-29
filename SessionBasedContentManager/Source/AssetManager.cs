using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Content;

namespace AssetBasedContentManager
{
    /// <summary>
    /// Defines an asset session.
    /// </summary>
    internal enum AssetSession : byte
    {
        None = 0,
        Session1 = 1,
        Session2 = 2,
        Session3 = 3,
        Session4 = 4,
        Session5 = 5,
        Session6 = 6,
        Permanent = 7
    }

    /// <summary>
    /// The AssetManager used for most content loading.
    /// </summary>
    internal sealed class AssetManager : ContentManager
    {
        /// <summary>
        /// Reference to the available thread worker for loading.
        /// </summary>
        public readonly ThreadWorker mThreadWorker;

        /// <summary>
        /// Dictionary of assets we have loaded so far.
        /// </summary>
        private readonly Dictionary<string, Asset> mLoadedAssets;

        /// <summary>
        /// List of requested batches.
        /// </summary>
        private readonly List<AssetRequestBatch> mRequestBatches;

        /// <summary>
        /// Stack of free sessions.
        /// </summary>
        private readonly Stack<AssetSession> mFreeSessionPile;

        /// <summary>
        /// Initializes a new instance of the AssetManager class.
        /// </summary>
        public AssetManager(ThreadWorker _threadWorker, IServiceProvider _serviceProvider)
            : base(_serviceProvider)
        {
            // Save reference.
            mThreadWorker = _threadWorker;

            mLoadedAssets = new Dictionary<string, Asset>();
            mRequestBatches = new List<AssetRequestBatch>();

            // Initialize the free stack.
            // NOTE: The Permanent session is never added here.
            mFreeSessionPile = new Stack<AssetSession>(6);
            for (int i = (int)AssetSession.Permanent - 1; i > 0; --i)
            {
                mFreeSessionPile.Push((AssetSession)i);
            }
        }

        /// <summary>
        /// Returns whether loading is active or not.
        /// </summary>
        public bool IsLoadingActive()
        {
            return mThreadWorker.TaskCount > 0;
        }

        /// <summary>
        /// Get a new session.
        /// </summary>
        public AssetSession GetNewSession()
        {
            return mFreeSessionPile.Pop();
        }

        /// <summary>
        /// Free a session. Dereferences any assets loaded under the session.
        /// </summary>
        public void FreeSession(AssetSession _session)
        {
            Stack<AssetSession> _freePile = mFreeSessionPile;
#if DEBUG
            if (_session == AssetSession.None || _session == AssetSession.Permanent)
            {
                throw new ArgumentException("_session");
            }

            // Make sure that the session is valid.
            foreach (AssetSession _assetSession in _freePile)
            {
                if (_assetSession == _session)
                {
                    throw new InvalidOperationException("The session is already free.");
                }
            }
#endif // DEBUG

            // Push the session on the free stack.
            _freePile.Push(_session);

            // Dereference the session from all loaded assets.
            byte _bitMask = (byte)(~(1 << (int)_session));
            foreach (Asset _asset in mLoadedAssets.Values)
            {
                _asset.mSessionFlags &= _bitMask;
            }
        }

        /// <summary>
        /// Load an asset under the persistent session.
        /// </summary>
        public override T Load<T>(string _assetUrl)
        {
            return Load<T>(_assetUrl, AssetSession.Permanent);
        }

        /// <summary>
        /// Load an asset under a given session.
        /// </summary>
        public T Load<T>(string _assetUrl, AssetSession _session)
        {
#if DEBUG
            // Make sure that the session is valid.
            foreach (AssetSession _assetSession in mFreeSessionPile)
            {
                if (_assetSession == _session)
                {
                    throw new InvalidOperationException("The specified session is marked as free.");
                }
            }
#endif // DEBUG

            Dictionary<string, Asset> _loadedAssets = mLoadedAssets;

            Asset _asset;
            if (!_loadedAssets.TryGetValue(_assetUrl, out _asset))
            {
                T _content;

                try
                {
                    _content = base.ReadAsset<T>(_assetUrl, null);
                }
                catch (OutOfMemoryException)
                {
                    // This should rarely happen, but in case it does.
                    // unload any assets that are no longer used
                    UnloadUnusedAssets();
                    GC.GetTotalMemory(true);

                    // Then try again.
                    _content = base.ReadAsset<T>(_assetUrl, null);
                }

                // Create the new asset object and save reference.
                _asset = new Asset();
                _asset.mContent = _content;
                _asset.mSessionFlags = 0;

                // Save reference.
                _loadedAssets.Add(_assetUrl, _asset);

                System.Diagnostics.Debug.WriteLine(string.Concat("<Success> Asset \"", _assetUrl, "\" loaded."));
            }

            // Flag the asset under the specified session.
            _asset.mSessionFlags |= (byte)(1 << (int)_session);
            return (T)_asset.mContent;
        }

        /// <summary>
        /// Add a batch request for loading.
        /// </summary>
        public void AddRequestBatch(AssetRequestBatch _batchRequest)
        {
            mRequestBatches.Add(_batchRequest);
        }

        /// <summary>
        /// Flush all the request batches. Loading starts asynchronously from this point.
        /// </summary>
        public void FlushRequestBatches(AssetSession _session)
        {
            mThreadWorker.EnqueueTask(new FlushSessionRequestsTask(this, _session));
        }

        /// <summary>
        /// Unload all data.
        /// </summary>
        public override void Unload()
        {
            foreach (Asset _asset in mLoadedAssets.Values)
            {
                if (_asset.mContent is IDisposable)
                {
                    ((IDisposable)_asset.mContent).Dispose();
                }
            }

            mLoadedAssets.Clear();
            base.Unload();
        }

        /// <summary>
        /// Unload assets that are no longer used.
        /// </summary>
        public void UnloadUnusedAssets()
        {
            Dictionary<string, Asset> _loadedAssets = mLoadedAssets;
            List<string> _assetsUnloaded = new List<string>();
            foreach (KeyValuePair<string, Asset> _keyValuePair in _loadedAssets)
            {
                Asset _asset = _keyValuePair.Value;
                if (_asset.mSessionFlags == 0)
                {
                    if (_asset.mContent is IDisposable)
                    {
                        ((IDisposable)_asset.mContent).Dispose();
                    }

                    _assetsUnloaded.Add(_keyValuePair.Key);
                }
            }

            // Remove the assets from the dictionary.
            for (int i = 0; i < _assetsUnloaded.Count; ++i)
            {
                _loadedAssets.Remove(_assetsUnloaded[i]);
            }
        }

        /// <summary>
        /// Open a stream to an asset given it's url.
        /// </summary>
        public Stream OpenAsset(string _assetUrl)
        {
            if (!Path.HasExtension(_assetUrl))
            {
                _assetUrl = string.Concat(_assetUrl, ".xnb");
            }
#if WINDOWS
            if (!Path.IsPathRooted(_assetUrl))
            {
                _assetUrl = string.Concat(RootDirectory, @"\", _assetUrl);
            }

            return (new StreamReader(_assetUrl)).BaseStream;
#else
            return Microsoft.Xna.Framework.TitleContainer.OpenStream(_assetUrl);
#endif // WINDOWS
        }

        /// <summary>
        /// Open a stream to an asset given it's url.
        /// </summary>
        protected override Stream OpenStream(string _assetUrl)
        {
            return OpenAsset(_assetUrl);
        }

        /// <summary>
        /// Compares two asset loaders by their asset url.
        /// </summary>
        private static int CompareAssetLoaders(IAssetLoader _assetLoaderA, IAssetLoader _assetLoaderB)
        {
            return string.CompareOrdinal(_assetLoaderA.AssetUrl, _assetLoaderA.AssetUrl);
        }

        /// <summary>
        /// Loads all assets requested for the specified session.
        /// </summary>
        private void LoadRequestBatches(AssetSession _session)
        {
#if DEBUG
            if (_session == AssetSession.None)
            {
                throw new ArgumentException("_session");
            }

            // Make sure that the session is valid.
            foreach (AssetSession _assetSession in mFreeSessionPile)
            {
                if (_assetSession == _session)
                {
                    throw new InvalidOperationException("The specified session is marked as free.");
                }
            }
#endif // DEBUG

            List<AssetRequestBatch> _requestedBatches = mRequestBatches;
            Dictionary<string, Asset> _loadedAssets = mLoadedAssets;

            // Flag assets that have already been loaded for use with the 
            // current session so that we don't unload them if need to free up memory.
            byte _bitMask = (byte)(1 << (int)_session);
            for (int i = 0; i < _requestedBatches.Count; ++i)
            {
                // Make sure this batch corresponds to the session specified.
                AssetRequestBatch _requestedBatch = _requestedBatches[i];
                if (_requestedBatch.mSession == _session)
                {
                    // Loop over each requested item in this batch.
                    List<IAssetLoader> _requestedItems = _requestedBatch.mRequestedItems;
                    for (int j = 0; j < _requestedItems.Count; ++j)
                    {
                        Asset asset;
                        string _assetUrl = _requestedItems[j].AssetUrl;
                        if (!_loadedAssets.TryGetValue(_assetUrl, out asset))
                        {
                            continue;
                        }

                        // Set the current session so that we don't unload it.
                        asset.mSessionFlags |= _bitMask;
                    }
                }
            }

            // Load the new content.
            for (int i = 0; i < _requestedBatches.Count; ++i)
            {
                AssetRequestBatch _requestedBatch = _requestedBatches[i];

                // Make sure this batch corresponds to the session specified.
                if (_requestedBatch.mSession == _session)
                {
                    _requestedBatches.RemoveAt(i);
                    --i;

                    // Sort the assets to optimize seeking.
                    List<IAssetLoader> _requestedItems = _requestedBatch.mRequestedItems;
                    _requestedItems.Sort(CompareAssetLoaders);

                    // Load each item in this batch.
                    for (int j = 0; j < _requestedItems.Count; ++j)
                    {
                        _requestedItems[j].Load(this, _session);
                    }

                    _requestedBatch.SetLoaded();
                }
            }
        }

        /// <summary>
        /// An asset object.
        /// </summary>
        private sealed class Asset
        {
            public object mContent;
            public byte mSessionFlags;
        }

        /// <summary>
        /// Simple task for flushing session requests.
        /// </summary>
        private sealed class FlushSessionRequestsTask : ITask
        {
            private AssetSession mSession;
            private AssetManager mManager;

            public FlushSessionRequestsTask(AssetManager _manager, AssetSession _session)
            {
                mSession = _session;
                mManager = _manager;
            }

            void ITask.Execute()
            {
                mManager.LoadRequestBatches(mSession);
            }
        }
    }
}
