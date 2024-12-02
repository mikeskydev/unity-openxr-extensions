// SPDX-FileCopyrightText: 2024 Michael Nisbet <me@mikesky.dev>
// SPDX-License-Identifier: MIT

#if XR_COMPOSITION_LAYERS
using System;
using UnityEngine;
using Unity.XR.CompositionLayers.Services;
using Unity.XR.CompositionLayers.Layers;
using UnityEngine.XR.OpenXR.NativeTypes;
using UnityEngine.XR.OpenXR.CompositionLayers;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace OpenXR.Extensions
{
    public class FBPassthroughLayerHandler : OpenXRLayerProvider.ILayerHandler, IDisposable
    {
        private Dictionary<int, XrCompositionLayerPassthroughFB> m_nativeLayers = new Dictionary<int, XrCompositionLayerPassthroughFB>();
        NativeArray<XrCompositionLayerPassthroughFB> m_ActiveNativeLayers;
        NativeArray<int> m_ActiveNativeLayerOrders;
        int m_ActiveNativeLayerCount;

        Dictionary<int, XrPassthroughLayerFB> m_layerHandles = new Dictionary<int, XrPassthroughLayerFB>();

        public void CreateLayer(CompositionLayerManager.LayerInfo layerInfo)
        {
            var layerData = layerInfo.Layer.LayerData as FBPassthroughLayerData;
            unsafe
            {
                if (!FBPassthrough.PassthroughRunning)
                {
                    FBPassthrough.StartPassthrough();
                }
                var passthroughFlags = XrPassthroughFlagsFB.XR_PASSTHROUGH_IS_RUNNING_AT_CREATION_BIT_FB | (layerData.SubmitDepth ? XrPassthroughFlagsFB.XR_PASSTHROUGH_LAYER_DEPTH_BIT_FB : 0);
                var createInfo = new XrPassthroughLayerCreateInfoFB(FBPassthrough.PassthroughHandle, passthroughFlags, XrPassthroughLayerPurposeFB.XR_PASSTHROUGH_LAYER_PURPOSE_RECONSTRUCTION_FB);
                var success = FBPassthrough.CreatePassthroughLayer(layerInfo.Id, createInfo, out var layerHandle);

                if (!success)
                {
                    return;
                }
                m_layerHandles[layerInfo.Id] = layerHandle;

                var layerFlags = layerData.BlendType == BlendType.Premultiply ? XrCompositionLayerFlags.SourceAlpha : XrCompositionLayerFlags.SourceAlpha | XrCompositionLayerFlags.UnPremultipliedAlpha;
                m_nativeLayers[layerInfo.Id] = new XrCompositionLayerPassthroughFB(layerFlags, layerHandle);
            }
        }

        public void OnUpdate()
        {
            if(m_ActiveNativeLayerCount > 0)
            {
                if(!FBPassthrough.PassthroughRunning)
                {
                    FBPassthrough.StartPassthrough();
                }
                unsafe
                {
                    OpenXRLayerUtility.AddActiveLayersToEndFrame(m_ActiveNativeLayers.GetUnsafePtr(), m_ActiveNativeLayerOrders.GetUnsafePtr(), m_ActiveNativeLayerCount, UnsafeUtility.SizeOf<XrCompositionLayerPassthroughFB>());
                }

                m_ActiveNativeLayerCount = 0;
            }
            else
            {
                // If there are no active layers, pause passthrough
                if(FBPassthrough.PassthroughRunning)
                {
                    FBPassthrough.PausePassthrough();
                }
            }
        }

        public void ModifyLayer(CompositionLayerManager.LayerInfo layerInfo)
        {
            // TODO
            return;
        }

        public void SetActiveLayer(CompositionLayerManager.LayerInfo layerInfo)
        {
            if (!m_nativeLayers.TryGetValue(layerInfo.Id, out var nativeLayer))
                return;

            m_nativeLayers[layerInfo.Id] = nativeLayer;
            ResizeNativeArrays();
            m_ActiveNativeLayers[m_ActiveNativeLayerCount] = m_nativeLayers[layerInfo.Id];
            m_ActiveNativeLayerOrders[m_ActiveNativeLayerCount] = layerInfo.Layer.Order;
            ++m_ActiveNativeLayerCount;
        }

        public void RemoveLayer(int id)
        {
            m_nativeLayers.Remove(id);

            if (m_layerHandles.TryGetValue(id, out var layerHandle))
            {
                var success = FBPassthrough.DestroyPassthroughLayer(id, layerHandle);
                if (!success)
                {
                    Debug.LogError($"Failed to destroy passthrough layer.");
                }
                m_layerHandles.Remove(id);
            }
        }

        public void Dispose()
        {
            m_nativeLayers.Clear();
            m_layerHandles.Clear();

            if (m_ActiveNativeLayers.IsCreated)
                m_ActiveNativeLayers.Dispose();
            if (m_ActiveNativeLayerOrders.IsCreated)
                m_ActiveNativeLayerOrders.Dispose();
        }
        
        protected virtual void ResizeNativeArrays()
        {
            if (!m_ActiveNativeLayers.IsCreated && !m_ActiveNativeLayerOrders.IsCreated)
            {
                m_ActiveNativeLayers = new NativeArray<XrCompositionLayerPassthroughFB>(m_nativeLayers.Count, Allocator.Persistent);
                m_ActiveNativeLayerOrders = new NativeArray<int>(m_nativeLayers.Count, Allocator.Persistent);
                return;
            }

            UnityEngine.Assertions.Assert.AreEqual(m_ActiveNativeLayers.Length, m_ActiveNativeLayerOrders.Length);

            if (m_ActiveNativeLayers.Length < m_nativeLayers.Count)
            {
                var newLayerArray = new NativeArray<XrCompositionLayerPassthroughFB>(m_nativeLayers.Count, Allocator.Persistent);
                NativeArray<XrCompositionLayerPassthroughFB>.Copy(m_ActiveNativeLayers, newLayerArray, m_ActiveNativeLayers.Length);
                m_ActiveNativeLayers.Dispose();
                m_ActiveNativeLayers = newLayerArray;

                var newOrderArray = new NativeArray<int>(m_nativeLayers.Count, Allocator.Persistent);
                NativeArray<int>.Copy(m_ActiveNativeLayerOrders, newOrderArray, m_ActiveNativeLayerOrders.Length);
                m_ActiveNativeLayerOrders.Dispose();
                m_ActiveNativeLayerOrders = newOrderArray;
            }
        }
    }

    [CompositionLayerData(
        Provider = "OpenXR Extensions",
        Name = "FB Passthrough Layer",
        IconPath = "",
        InspectorIcon = "",
        ListViewIcon = "",
        Description = "FB Passthrough layer for OpenXR"//,
        //SuggestedExtenstionTypes = new[] { typeof(ColorScaleBiasExtension) }
    )]
    public class FBPassthroughLayerData : LayerData
    {
        [Tooltip("Submit depth data to the runtime")]
        [SerializeField] bool m_SubmitDepth;

        public bool SubmitDepth
        {
            get => m_SubmitDepth;
            set => m_SubmitDepth = UpdateValue(m_SubmitDepth, value);
        }

        public override void CopyFrom(LayerData layerData)
        {
            if (layerData is FBPassthroughLayerData customQuadLayerData)
            {
                m_SubmitDepth = customQuadLayerData.SubmitDepth;
            }
        }
    }
}
#endif
