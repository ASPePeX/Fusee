﻿using Fusee.Base.Core;
using Fusee.Engine.Core;
using Fusee.Engine.Core.Scene;
using Fusee.PointCloud.Common;
using Fusee.PointCloud.Core.Accessors;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fusee.PointCloud.Core
{
    /// <summary>
    /// Delegate for a method that tries to get the mesh(es) of an octant. If they are not cached yet, they should be created an added to the _meshCache.
    /// </summary>
    /// <param name="guid"></param>
    /// <returns></returns>
    public delegate IEnumerable<InstanceData> GetInstanceData(string guid);

    /// <summary>
    /// Generic delegate to inject a method that nows how to actually create a GpuMesh for the given point type.
    /// The injected methods decide which point values are assigned to which mesh properties (primarily important for the various color values).
    /// </summary>
    /// <typeparam name="TPoint">Generic that describes the point type.</typeparam>
    /// <param name="ptAccessor">The <see cref="PointAccessor{TPoint}"/> that can be used to access the point data without casting the points.</param>
    /// <param name="points">The point cloud points as generic array.</param>
    /// <returns></returns>
    public delegate InstanceData CreateInstanceData<TPoint>(PointAccessor<TPoint> ptAccessor, TPoint[] points);

    /// <summary>
    /// Manages the caching and loading of point and mesh data.
    /// </summary>
    /// <typeparam name="TPoint"></typeparam>
    public class PointCloudDataHandlerInstanced<TPoint> : PointCloudDataHandlerBase<InstanceData> where TPoint : new()
    {
        /// <summary>
        /// Caches loaded points.
        /// </summary>
        private readonly MemoryCache<string, TPoint[]> _pointCache;

        /// <summary>
        /// Caches loaded points.
        /// </summary>
        private readonly MemoryCache<string, IEnumerable<InstanceData>> _instanceDataCache;

        private readonly PointAccessor<TPoint> _pointAccessor;
        private readonly CreateInstanceData<TPoint> _createInstanceDataHandler;
        private readonly LoadPointsHandler<TPoint> _loadPointsHandler;
        private const int _maxNumberOfDisposals = 1;
        private float _deltaTimeSinceLastDisposal;
        private bool _disposed;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="pointAccessor">The point accessor that allows to access the point data.</param>
        /// <param name="createMeshHandler">Method that knows how to create a mesh for the explicit point type (see <see cref="MeshMaker"/>).</param>
        /// <param name="loadPointsHandler">The method that is able to load the points from the hard drive/file.</param>
        public PointCloudDataHandlerInstanced(PointAccessor<TPoint> pointAccessor, CreateInstanceData<TPoint> createInstanceDataHandler, LoadPointsHandler<TPoint> loadPointsHandler)
        {
            _pointCache = new();
            _instanceDataCache = new();
            _instanceDataCache.SlidingExpiration = 30;
            _instanceDataCache.ExpirationScanFrequency = 31;

            _createInstanceDataHandler = createInstanceDataHandler;
            _loadPointsHandler = loadPointsHandler;
            _pointAccessor = pointAccessor;

            LoadingQueue = new((8 ^ 8) / 8);
            DisposeQueue = new Dictionary<string, IEnumerable<InstanceData>>((8 ^ 8) / 8);

            _instanceDataCache.HandleEvictedItem = OnItemEvictedFromCache;
        }

        /// <summary>
        /// First looks in the mesh cache, if there are meshes return, 
        /// else look in the DisposeQueue, if there are meshes return,
        /// else look in the point cache, if there are points create a mesh and add to the _meshCache.
        /// </summary>
        /// <param name="guid">The unique id of an octant.</param>
        /// <returns></returns>
        public override IEnumerable<InstanceData> GetGpuData(string guid)
        {
            if (_instanceDataCache.TryGetValue(guid, out var instanceData))
                return instanceData;
            else if (DisposeQueue.TryGetValue(guid, out instanceData))
            {
                lock (LockDisposeQueue)
                {
                    DisposeQueue.Remove(guid);
                    _instanceDataCache.Add(guid, instanceData);
                    return instanceData;
                }
            }
            else if (_pointCache.TryGetValue(guid, out var points))
            {
                //does not have to be a list/enumerable here - this is because of PointCloudDataHandlerBase and the 65k vertex constraint for meshes.
                instanceData = new List<InstanceData>() { _createInstanceDataHandler.Invoke(_pointAccessor, points) };  
                _instanceDataCache.Add(guid, instanceData);
            }
            //no points yet, probably in loading queue
            return null;
        }

        /// <summary>
        /// Disposes of unused meshes, if needed. Depends on the dispose rate and the expiration frequency of the _meshCache.
        /// </summary>
        public override void ProcessDisposeQueue()
        {
            if (_deltaTimeSinceLastDisposal < DisposeRate)
                _deltaTimeSinceLastDisposal += Time.DeltaTime;
            else
            {
                _deltaTimeSinceLastDisposal = 0;

                lock (LockDisposeQueue)
                {
                    if (DisposeQueue.Count > 0)
                    {
                        var nodesInQueue = DisposeQueue.Count;
                        var count = nodesInQueue < _maxNumberOfDisposals ? nodesInQueue : _maxNumberOfDisposals;

                        for (int i = 0; i < count; i++)
                        {
                            var instanceData = DisposeQueue.Last();
                            var removed = DisposeQueue.Remove(instanceData.Key);
                            foreach (var data in instanceData.Value)
                                data.Dispose();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Loads points from the hard drive if they are neither in the loading queue nor in the PointCahce.
        /// </summary>
        /// <param name="guid">The octant for which the points should be loaded.</param>
        public override void TriggerPointLoading(string guid)
        {
            if (!LoadingQueue.Contains(guid) && LoadingQueue.Count <= MaxNumberOfNodesToLoad)
            {
                lock (LockLoadingQueue)
                {
                    LoadingQueue.Add(guid);
                }

                _ = Task.Run(() =>
                {
                    if (!_pointCache.TryGetValue(guid, out var points))
                    {
                        points = _loadPointsHandler.Invoke(guid);
                        _pointCache.Add(guid, points);
                    }

                    lock (LockLoadingQueue)
                    {
                        LoadingQueue.Remove(guid);
                    }
                });
            }
        }

        private void OnItemEvictedFromCache(object guid, object instanceData, EvictionReason reason, object state)
        {
            lock (LockDisposeQueue)
            {
                DisposeQueue.Add((string)guid, (IEnumerable<InstanceData>)instanceData);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">If disposing equals true, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        /// If disposing equals false, the method has been called by the
        /// runtime from inside the finalizer and you should not reference
        /// other objects. Only unmanaged resources can be disposed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _pointCache.Dispose();
                    _instanceDataCache.Dispose();

                    LockDisposeQueue = null;
                    LockLoadingQueue = null;
                    foreach (var instanceData in DisposeQueue)
                    {
                        foreach (var data in instanceData.Value)
                        {
                            data.Dispose();
                        }
                    }
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizers (historically referred to as destructors) are used to perform any necessary final clean-up when a class instance is being collected by the garbage collector.
        /// </summary>
        ~PointCloudDataHandlerInstanced()
        {
            Dispose(disposing: false);
        }
    }
}