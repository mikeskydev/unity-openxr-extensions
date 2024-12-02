// SPDX-FileCopyrightText: 2024 Michael Nisbet <me@mikesky.dev>
// SPDX-License-Identifier: MIT

using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.XR.CompositionLayers;
using UnityEngine.XR.OpenXR.NativeTypes;

namespace OpenXR.Extensions
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("XR/Composition Layers/Extensions/FB Depth Test")]
    public class FBDepthTestExtension : CompositionLayerExtension
    {
        public override ExtensionTarget Target => ExtensionTarget.Layer;

        public enum ComparisonOp
        {
            Never = 0,
            Less = 1,
            Equal = 2,
            LessOrEqual = 3,
            Greater = 4,
            NotEqual = 5,
            GreaterOrEqual = 6,
            Always = 7,
        }

        [SerializeField]
        bool m_WriteDepth = true;
        
        [SerializeField]
        ComparisonOp m_DepthComparison = ComparisonOp.Less;


        NativeArray<XrCompositionLayerDepthTestFB> m_NativeArray;

        public ComparisonOp DepthComparison
        {
            get => m_DepthComparison;
            set => m_DepthComparison = UpdateValue(m_DepthComparison, value);
        }

        public override unsafe void* GetNativeStructPtr()
        {
            var depthTestStruct = new XrCompositionLayerDepthTestFB()
            {
                type = XrStructureTypeExt.XR_TYPE_COMPOSITION_LAYER_DEPTH_TEST_FB,
                next = null,
                depthMask = new XrBool32(m_WriteDepth),
                compareOp = (XrCompareOpFB)m_DepthComparison
            };

            if (!m_NativeArray.IsCreated)
                m_NativeArray = new NativeArray<XrCompositionLayerDepthTestFB>(1, Allocator.Persistent);

            m_NativeArray[0] = depthTestStruct;
            return m_NativeArray.GetUnsafePtr();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            if (m_NativeArray.IsCreated)
                m_NativeArray.Dispose();
        }
    }
}
